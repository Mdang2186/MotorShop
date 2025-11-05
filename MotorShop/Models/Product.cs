using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MotorShop.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required, StringLength(150)]
        public required string Name { get; set; }

        [StringLength(4000)]
        public string? Description { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        [Range(0, 100000000)]
        public decimal Price { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        [Range(0, 100000000)]
        public decimal? OriginalPrice { get; set; }

        [Range(0, int.MaxValue)]
        public int StockQuantity { get; set; }

        [StringLength(500)]
        public string? ImageUrl { get; set; }   // ảnh “legacy” hoặc ảnh đại diện

        [Range(1900, 2100)]
        public int Year { get; set; }

        [StringLength(180)]
        public string? Slug { get; set; }

        public bool IsPublished { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        [Timestamp]
        public byte[]? RowVersion { get; set; }

        // FK
        public int BrandId { get; set; }
        public int CategoryId { get; set; }

        // Nav
        public Brand Brand { get; set; } = null!;
        public Category Category { get; set; } = null!;

        // NEW: Gallery + Specs + Tags
        public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
        public ICollection<ProductSpecification> Specifications { get; set; } = new List<ProductSpecification>();
        public ICollection<ProductTag> ProductTags { get; set; } = new List<ProductTag>();

        // NEW: Ảnh ưu tiên cho UI (ảnh primary -> ảnh đầu -> ImageUrl)
        [NotMapped]
        public string? PrimaryImageUrl =>
            (Images?.Count > 0
                ? (Images!.OrderBy(i => i.SortOrder).FirstOrDefault(i => i.IsPrimary)?.ImageUrl
                   ?? Images!.OrderBy(i => i.SortOrder).First().ImageUrl)
                : null)
            ?? ImageUrl;
    }
}
