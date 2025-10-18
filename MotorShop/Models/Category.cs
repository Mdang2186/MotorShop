using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MotorShop.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên loại xe là bắt buộc.")]
        [StringLength(100)]
        public required string Name { get; set; }

        

      
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}