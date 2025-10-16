using System.ComponentModel.DataAnnotations.Schema;

namespace MotorShop.Models
{
    // Sản phẩm xe máy
    public class Product
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal Price { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? OriginalPrice { get; set; }

        public int StockQuantity { get; set; }
        public string? ImageUrl { get; set; }
        public int Year { get; set; }

        // Khóa ngoại
        public int BrandId { get; set; }
        public int CategoryId { get; set; }

        // Thuộc tính điều hướng
        public Brand Brand { get; set; } = null!;
        public Category Category { get; set; } = null!;
    }
}   