using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MotorShop.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên danh mục là bắt buộc"), StringLength(100)]
        public required string Name { get; set; }

        [StringLength(120)]
        public string? Slug { get; set; }

        [StringLength(500)]
        public string? Description { get; set; } // <--- ĐÃ THÊM ĐỂ SỬA LỖI

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}