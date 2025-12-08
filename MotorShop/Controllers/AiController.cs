using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MotorShop.Data;
using MotorShop.Models;
using MotorShop.Services.Ai;
using MotorShop.ViewModels.Ai;

namespace MotorShop.Controllers
{
    [Route("ai")]
    public class AiController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly AiQueryParser _parser;
        private readonly AiRecommendationService _recommendation;
        private readonly ILogger<AiController> _logger;

        public AiController(
            ApplicationDbContext db,
            AiQueryParser parser,
            AiRecommendationService recommendation,
            ILogger<AiController> logger)
        {
            _db = db;
            _parser = parser;
            _recommendation = recommendation;
            _logger = logger;
        }

        [HttpGet("")]
        [HttpGet("index")]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] AiChatRequest request, CancellationToken ct)
        {
            // 1. Kiểm tra đầu vào
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { error = "Nội dung câu hỏi không được để trống." });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 2. Quản lý Hội thoại (Conversation)
            AiConversation conversation;
            if (request.ConversationId.HasValue && request.ConversationId > 0)
            {
                conversation = await _db.AiConversations
                    .Include(c => c.Messages)
                    .FirstOrDefaultAsync(c => c.Id == request.ConversationId.Value, ct);

                if (conversation == null)
                {
                    // Nếu ID không tồn tại, tạo mới
                    conversation = new AiConversation
                    {
                        UserId = userId,
                        CreatedAtUtc = DateTime.UtcNow,
                        LastUpdatedUtc = DateTime.UtcNow,
                        Title = request.Message.Length > 50 ? request.Message.Substring(0, 50) + "..." : request.Message
                    };
                    _db.AiConversations.Add(conversation);
                }
            }
            else
            {
                conversation = new AiConversation
                {
                    UserId = userId,
                    CreatedAtUtc = DateTime.UtcNow,
                    LastUpdatedUtc = DateTime.UtcNow,
                    Title = request.Message.Length > 50 ? request.Message.Substring(0, 50) + "..." : request.Message
                };
                _db.AiConversations.Add(conversation);
            }

            // 3. Lưu tin nhắn User
            var userMsg = new AiMessage
            {
                Conversation = conversation,
                IsUser = true,
                Content = request.Message,
                CreatedAtUtc = DateTime.UtcNow
            };
            // Lưu ý: Nếu EF Core tự track conversation thì chỉ cần Add vào Messages hoặc DbContext
            if (conversation.Id > 0) _db.AiMessages.Add(userMsg);
            else conversation.Messages.Add(userMsg);

            // Cập nhật thời gian hội thoại
            conversation.LastUserMessage = request.Message;
            conversation.LastUpdatedUtc = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);

            // 4. XỬ LÝ AI (Parser + Service)
            // ---------------------------------------------------------
            // B1: Học ngữ cảnh từ các tin nhắn cũ (Context Learning)
            var finalQuery = new AiParsedQuery();

            // Lấy 3 tin nhắn gần nhất của user để gộp thông tin (ví dụ: câu trước hỏi Honda, câu sau hỏi giá)
            var recentMsgs = conversation.Messages
                .Where(m => m.IsUser)
                .OrderByDescending(m => m.CreatedAtUtc)
                .Take(3)
                .OrderBy(m => m.CreatedAtUtc)
                .ToList();

            foreach (var msg in recentMsgs)
            {
                var pastQ = _parser.Parse(msg.Content);
                finalQuery.Merge(pastQ);
            }

            // B2: Phân tích tin nhắn hiện tại và gộp vào
            var currentQ = _parser.Parse(request.Message);
            finalQuery.Merge(currentQ);

            // B3: Tìm kiếm sản phẩm phù hợp
            var suggestions = await _recommendation.GetSuggestionsAsync(finalQuery, userId, ct);
            var insight = finalQuery.BuildInsightSentence();

            // ---------------------------------------------------------
            // 5. ENRICH DATA (Lấy thêm thông tin chi tiết cho Sidebar)
            // Service chỉ trả về thông tin cơ bản, ta cần query thêm DB để lấy SKU, Description, Stock...
            // ---------------------------------------------------------
            var productIds = suggestions.Select(s => s.ProductId).ToList();

            var fullProducts = await _db.Products
                .AsNoTracking()
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, ct);

            var enrichedItems = suggestions.Select(s =>
            {
                if (fullProducts.TryGetValue(s.ProductId, out var p))
                {
                    return new
                    {
                        productId = p.Id,
                        name = p.Name,
                        price = p.Price,
                        imageUrl = p.ImageUrl,

                        // Dữ liệu bổ sung cho Sidebar
                        sku = p.SKU,
                        year = p.Year,
                        stockQuantity = p.StockQuantity,
                        brandName = p.Brand?.Name ?? "Khác",
                        categoryName = p.Category?.Name ?? "Sản phẩm",
                        description = p.Description, // HTML Description (Blog)

                        reason = s.Reason
                    };
                }
                return null;
            }).Where(x => x != null).ToList();

            // 6. Lưu tin nhắn Bot
            string botContent = enrichedItems.Any()
                ? $"Mình tìm thấy {enrichedItems.Count} sản phẩm phù hợp."
                : "Rất tiếc, mình chưa tìm thấy sản phẩm nào khớp 100% tiêu chí. Bạn thử nới lỏng yêu cầu xem sao nhé.";

            var botMsg = new AiMessage
            {
                ConversationId = conversation.Id,
                IsUser = false,
                Content = botContent,
                ParsedInsight = insight,
                SuggestionsJson = JsonSerializer.Serialize(suggestions), // Lưu log gọn
                CreatedAtUtc = DateTime.UtcNow
            };
            _db.AiMessages.Add(botMsg);
            await _db.SaveChangesAsync(ct);

            // 7. Trả về kết quả JSON
            return Json(new
            {
                conversationId = conversation.Id,
                insight = insight,
                items = enrichedItems // Trả về danh sách đầy đủ (Enriched)
            });
        }

        // API lấy lịch sử chat (Optional)
        [HttpGet("history")]
        public async Task<IActionResult> History(CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var list = await _db.AiConversations
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.LastUpdatedUtc)
                .Take(10)
                .Select(c => new { c.Id, c.Title, c.LastUpdatedUtc })
                .ToListAsync(ct);

            return Json(list);
        }
    }
}