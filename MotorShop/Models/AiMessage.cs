using System;

namespace MotorShop.Models
{
    /// <summary>
    /// Một message trong phiên AI (của user hoặc “bot” AI).
    /// </summary>
    public class AiMessage
    {
        public int Id { get; set; }

        public int ConversationId { get; set; }

        public AiConversation Conversation { get; set; } = null!;

        /// <summary>true = user, false = AI.</summary>
        public bool IsUser { get; set; }

        /// <summary>Nội dung chat hiển thị cho người dùng.</summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Chuỗi mô tả insight mà parser hiểu được (ví dụ: “AI hiểu: chiều cao ~160cm, ngân sách 40–50 triệu …”).
        /// </summary>
        public string? ParsedInsight { get; set; }

        /// <summary>
        /// JSON lưu danh sách gợi ý (chỉ dùng cho message của AI, để sau này có thể load lại nhanh).
        /// </summary>
        public string? SuggestionsJson { get; set; }

        /// <summary>Thời điểm gửi message (UTC).</summary>
        public DateTime CreatedAtUtc { get; set; }
    }
}
