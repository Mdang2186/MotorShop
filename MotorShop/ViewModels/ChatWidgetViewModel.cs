using System;
using System.Collections.Generic;

namespace MotorShop.Models.ViewModels
{
    public class ChatWidgetMessageViewModel
    {
        public string SenderDisplayName { get; set; } = null!;
        // Thuộc tính này được dùng để xác định ai là người gửi
        public bool IsFromStaff { get; set; }
        public string Content { get; set; } = null!;
        public DateTime SentAt { get; set; }
        public string SentAtDisplay { get; set; } = null!;
    }

    public class ChatWidgetViewModel
    {
        public int ThreadId { get; set; }
        public List<ChatWidgetMessageViewModel> Messages { get; set; } = new();
    }
}