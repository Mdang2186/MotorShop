using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MotorShop.Data;
using MotorShop.Models;
using MotorShop.Services.Ai;
using MotorShop.ViewModels.Ai;
using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MotorShop.Controllers
{
    [Route("ai")]
    public class AiController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly AiQueryParser _parser;
        private readonly AiRecommendationService _ai;

        public AiController(
            ApplicationDbContext db,
            AiQueryParser parser,
            AiRecommendationService ai)
        {
            _db = db;
            _parser = parser;
            _ai = ai;
        }

        [HttpGet("")]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] AiChatRequest req, CancellationToken ct)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Message))
                return BadRequest(new { error = "Nội dung câu hỏi không được để trống." });

            // 1. Phân tích câu hỏi
            var parsed = _parser.Parse(req.Message);
            var insight = parsed.BuildInsightSentence();

            // 2. Gợi ý sản phẩm
            var suggestions = await _ai.GetSuggestionsAsync(parsed, ct);

            // 3. Lưu lịch sử chat (nếu có user hoặc anh vẫn muốn lưu cho khách)
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            AiConversation? convo = null;

            if (req.ConversationId is int existingId && existingId > 0)
            {
                convo = await _db.AiConversations
                    .Include(c => c.Messages)
                    .FirstOrDefaultAsync(c => c.Id == existingId, ct);
            }

            if (convo == null)
            {
                convo = new AiConversation
                {
                    UserId = userId,
                    CreatedAtUtc = DateTime.UtcNow,
                    LastUpdatedUtc = DateTime.UtcNow,
                    Title = req.Message.Length > 60 ? req.Message[..60] + "…" : req.Message,
                    LastUserMessage = req.Message
                };
                _db.AiConversations.Add(convo);
            }
            else
            {
                convo.LastUpdatedUtc = DateTime.UtcNow;
                convo.LastUserMessage = req.Message;
            }

            // message của user
            var userMsg = new AiMessage
            {
                Conversation = convo,
                IsUser = true,
                Content = req.Message,
                ParsedInsight = insight,
                CreatedAtUtc = DateTime.UtcNow
            };
            _db.AiMessages.Add(userMsg);

            // message “bot” – tóm tắt lại + lưu JSON gợi ý
            var botSummary = suggestions.Any()
                ? $"Mình đã chọn ra {suggestions.Count} mẫu xe phù hợp nhất với tiêu chí của bạn."
                : "Hiện mình chưa tìm được mẫu xe nào thật sự khớp 100%. Bạn có thể điều chỉnh lại tiêu chí và thử lần nữa nhé.";

            var botMsg = new AiMessage
            {
                Conversation = convo,
                IsUser = false,
                Content = botSummary,
                ParsedInsight = insight,
                SuggestionsJson = JsonSerializer.Serialize(suggestions),
                CreatedAtUtc = DateTime.UtcNow
            };
            _db.AiMessages.Add(botMsg);

            await _db.SaveChangesAsync(ct);

            var response = new AiChatResponse
            {
                ConversationId = convo.Id,
                Insight = insight,
                Items = suggestions
            };

            return Json(response);
        }

        /// <summary>Lịch sử những phiên AI gần đây của user (để sau dùng build UI “Lịch sử trò chuyện”).</summary>
        [HttpGet("history")]
        public async Task<IActionResult> History(CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Json(Array.Empty<object>());

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
