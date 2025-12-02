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
                return BadRequest("Nội dung câu hỏi không được để trống.");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 2. Xử lý hội thoại (Conversation)
            AiConversation conversation;
            if (request.ConversationId.HasValue)
            {
                conversation = await _db.AiConversations
                    .Include(c => c.Messages)
                    .FirstOrDefaultAsync(c => c.Id == request.ConversationId.Value, ct)
                    ?? new AiConversation
                    {
                        UserId = userId,
                        CreatedAtUtc = DateTime.UtcNow,
                        LastUpdatedUtc = DateTime.UtcNow
                    };

                if (conversation.Id == 0) _db.AiConversations.Add(conversation);
            }
            else
            {
                conversation = new AiConversation
                {
                    UserId = userId,
                    CreatedAtUtc = DateTime.UtcNow,
                    LastUpdatedUtc = DateTime.UtcNow
                };
                _db.AiConversations.Add(conversation);
            }

            // 3. Lưu tin nhắn người dùng
            var userMsg = new AiMessage
            {
                Conversation = conversation,
                IsUser = true,
                Content = request.Message,
                CreatedAtUtc = DateTime.UtcNow
            };
            conversation.Messages.Add(userMsg);

            conversation.LastUserMessage = request.Message;
            conversation.LastUpdatedUtc = DateTime.UtcNow;

            if (string.IsNullOrWhiteSpace(conversation.Title))
            {
                var title = request.Message.Trim();
                if (title.Length > 60) title = title.Substring(0, 60) + "...";
                conversation.Title = title;
            }

            await _db.SaveChangesAsync(ct);

            // 4. Xử lý AI Logic
            var parsed = _parser.Parse(request.Message);

            // Lấy danh sách gợi ý cơ bản từ Service (thường chỉ có ID, Name, Price, Reason)
            var suggestions = await _recommendation.GetSuggestionsAsync(parsed, userId, ct);

            // --- [QUAN TRỌNG] ENRICH DATA: Lấy thêm thông tin chi tiết từ DB ---
            // Bước này đảm bảo Sidebar ở Frontend có đủ dữ liệu (SKU, Brand, Description, Stock...)
            // mà không cần sửa đổi Service hay ViewModel cũ.

            var productIds = suggestions.Select(s => s.ProductId).ToList();

            var fullProducts = await _db.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, ct);

            // Map lại dữ liệu đầy đủ cho JSON response
            var enrichedItems = suggestions.Select(s => {
                if (fullProducts.TryGetValue(s.ProductId, out var p))
                {
                    return new
                    {
                        productId = p.Id,
                        name = p.Name,
                        price = p.Price,
                        imageUrl = p.ImageUrl,

                        // Các trường bổ sung cho Sidebar
                        sku = p.SKU,
                        year = p.Year,
                        stockQuantity = p.StockQuantity,
                        brandName = p.Brand?.Name ?? "Khác",
                        categoryName = p.Category?.Name ?? "Xe máy",
                        description = p.Description, // HTML Description từ Seeder

                        reason = s.Reason
                    };
                }
                return null;
            }).Where(x => x != null).ToList();
            // ------------------------------------------------------------------

            var insight = parsed.BuildInsightSentence()
                          ?? "AI đã phân tích nhu cầu của bạn và gợi ý một số mẫu xe phù hợp bên dưới.";

            // 5. Lưu tin nhắn Bot
            var botMsg = new AiMessage
            {
                ConversationId = conversation.Id,
                IsUser = false,
                Content = insight,
                ParsedInsight = insight,
                // Lưu object gốc vào DB để nhẹ database
                SuggestionsJson = JsonSerializer.Serialize(suggestions),
                CreatedAtUtc = DateTime.UtcNow
            };

            _db.AiMessages.Add(botMsg);
            await _db.SaveChangesAsync(ct);

            // 6. Trả về JSON (Sử dụng anonymous object để linh hoạt cấu trúc)
            return Json(new
            {
                conversationId = conversation.Id,
                insight = insight,
                items = enrichedItems // Trả về danh sách đã có đầy đủ thông tin
            });
        }

        [HttpGet("history")]
        public async Task<IActionResult> History(CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Json(Array.Empty<object>());
            }

            var list = await _db.AiConversations
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.LastUpdatedUtc)
                .Take(10)
                .Select(c => new
                {
                    c.Id,
                    c.Title,
                    c.LastUserMessage,
                    lastUpdated = c.LastUpdatedUtc
                })
                .ToListAsync(ct);

            return Json(list);
        }
    }
}