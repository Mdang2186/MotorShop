// Models/Brand.cs
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MotorShop.Models
{
    public class Brand
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = "";

        [StringLength(120)]
        public string? Slug { get; set; }

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
