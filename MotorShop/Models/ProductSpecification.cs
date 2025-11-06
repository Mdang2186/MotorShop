using System.ComponentModel.DataAnnotations;

namespace MotorShop.Models
{
    public class ProductSpecification
    {
        public int Id { get; set; }
        public int ProductId { get; set; }

        [Required, StringLength(100)]
        public required string Name { get; set; }

        [StringLength(1000)]
        public string? Value { get; set; }

        public int SortOrder { get; set; } = 0;

        public Product Product { get; set; } = null!;
    }
}
