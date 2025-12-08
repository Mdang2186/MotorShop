using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MotorShop.Models
{
    public class Brand
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = "";

        // Dùng cho SEO + đường dẫn ảnh belt (có thể trùng với name đã slug hoá)
        [StringLength(120)]
        public string? Slug { get; set; }

        // Mô tả ngắn (tuỳ chọn)
        [StringLength(255)]
        public string? Description { get; set; }

        // Đường dẫn logo nhỏ: /images/brands/honda.png
        [StringLength(255)]
        public string? LogoUrl { get; set; }

        // Bật/tắt thương hiệu
        public bool IsActive { get; set; } = true;

        // Dùng cho thống kê / sắp xếp
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
