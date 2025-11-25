using System;
using MotorShop.Models;

namespace MotorShop.Models.Entities
{
    public class ChatMessage
    {
        public int Id { get; set; }

        public int ThreadId { get; set; }
        public ChatThread Thread { get; set; } = null!;

        public string SenderId { get; set; } = null!;
        public ApplicationUser Sender { get; set; } = null!;

        public string Content { get; set; } = null!;
        public bool IsFromStaff { get; set; }
        public DateTime SentAt { get; set; }
    }
}
