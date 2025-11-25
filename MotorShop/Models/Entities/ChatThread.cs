using System;
using System.Collections.Generic;
using MotorShop.Models;

namespace MotorShop.Models.Entities
{
    public class ChatThread
    {
        public int Id { get; set; }

        public string CustomerId { get; set; } = null!;
        public ApplicationUser Customer { get; set; } = null!;

        public string? StaffId { get; set; }
        public ApplicationUser? Staff { get; set; }

        public DateTime CreatedAt { get; set; }
        public bool IsClosed { get; set; }

        public DateTime? LastMessageAt { get; set; }
        public string? LastMessagePreview { get; set; }

        public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }
}
