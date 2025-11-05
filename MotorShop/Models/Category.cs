using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MotorShop.Models
{

    public class Category
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public required string Name { get; set; }

        // NEW: SEO-friendly URL segment (tùy chọn)
        [StringLength(120)]
        public string? Slug { get; set; }

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}