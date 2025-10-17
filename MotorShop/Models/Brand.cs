using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MotorShop.Models
{
    public class Brand
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên thương hiệu là bắt buộc.")]
        [StringLength(100)]
        public required string Name { get; set; }

        // Dòng 'Slug' đã được xóa

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}