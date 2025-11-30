using System;
using System.Collections.Generic;

namespace MotorShop.Models
{
    /// <summary>
    /// Một phiên trò chuyện AI (1 cuộc hỏi – đáp).
    /// </summary>
    public class AiConversation
    {
        public int Id { get; set; }

        /// <summary>User sở hữu cuộc trò chuyện (có thể null nếu khách chưa đăng nhập).</summary>
        public string? UserId { get; set; }

        public ApplicationUser? User { get; set; }

        /// <summary>Tiêu đề ngắn gọn để hiển thị trong lịch sử (ví dụ: “Xe cho nữ cao 1m6, 40–50tr”).</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>Thời điểm bắt đầu phiên (UTC).</summary>
        public DateTime CreatedAtUtc { get; set; }

        /// <summary>Thời điểm cập nhật cuối (UTC), dùng để sort lịch sử.</summary>
        public DateTime LastUpdatedUtc { get; set; }

        /// <summary>Nội dung câu hỏi gần nhất của user (để xem nhanh trong list).</summary>
        public string? LastUserMessage { get; set; }

        /// <summary>Danh sách message trong phiên AI này.</summary>
        public ICollection<AiMessage> Messages { get; set; } = new List<AiMessage>();
    }
}
