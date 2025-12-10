using System;
using System.ComponentModel.DataAnnotations;

namespace MotorShop.Models
{
    public class ProductReview
    {
        public int Id { get; set; }

        // FK
        public int ProductId { get; set; }
        [Required]
        public string UserId { get; set; } = null!;
        public int OrderId { get; set; }

        // Nội dung đánh giá
        [Range(1, 5)]
        public int Rating { get; set; }      // 1..5 sao

        [StringLength(1000)]
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public Product Product { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;
        public Order Order { get; set; } = null!;
    }
}
