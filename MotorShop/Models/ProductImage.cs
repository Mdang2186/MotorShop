using System.ComponentModel.DataAnnotations;

namespace MotorShop.Models
{
    public class ProductImage
    {
        public int Id { get; set; }
        public int ProductId { get; set; }

        // ProductImage.cs
        [Required, StringLength(500)]
        public required string ImageUrl { get; set; }

        [Required, StringLength(255)]
        public string Caption { get; set; } = ""; // mặc định rỗng, tránh NULL khi seed


        public int SortOrder { get; set; } = 0;
        public bool IsPrimary { get; set; } = false;

        public Product Product { get; set; } = null!;
    }
}
