using System;

namespace MotorShop.Models
{
    /// <summary>Một message trong cuộc hội thoại AI.</summary>
    public class AiMessage
    {
        public int Id { get; set; }

        public int ConversationId { get; set; }
        public AiConversation Conversation { get; set; } = null!;

        /// <summary>true = tin của user, false = tin “bot” AI.</summary>
        public bool IsUser { get; set; }

        /// <summary>Nội dung hiển thị cho user.</summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>Insight đã phân tích (chiều cao, ngân sách…)</summary>
        public string? ParsedInsight { get; set; }

        /// <summary>JSON danh sách gợi ý cho message AI (nếu là bot message).</summary>
        public string? SuggestionsJson { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
