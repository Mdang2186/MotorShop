using System;
using System.Collections.Generic;

namespace MotorShop.Models
{
    /// <summary>1 phiên trò chuyện AI của một user.</summary>
    public class AiConversation
    {
        public int Id { get; set; }

        /// <summary>User Identity Id, có thể null nếu khách chưa đăng nhập.</summary>
        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>Tiêu đề gợi nhớ (lấy từ câu hỏi đầu tiên).</summary>
        public string? Title { get; set; }

        /// <summary>Câu hỏi cuối cùng của người dùng.</summary>
        public string? LastUserMessage { get; set; }

        public List<AiMessage> Messages { get; set; } = new();
    }
}
