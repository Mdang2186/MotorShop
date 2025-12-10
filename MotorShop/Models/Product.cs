using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace MotorShop.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên sản phẩm là bắt buộc"), StringLength(150)]
        public required string Name { get; set; }

        [StringLength(50, ErrorMessage = "Mã SKU tối đa 50 ký tự")]
        public string? SKU { get; set; } // <--- ĐÃ THÊM ĐỂ SỬA LỖI

        public string? Description { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        [Range(0, 100000000)]
        public decimal Price { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? OriginalPrice { get; set; }

        [Range(0, int.MaxValue)]
        public int StockQuantity { get; set; }

        [StringLength(500)]
        public string? ImageUrl { get; set; }

        [Range(1900, 2100)]
        public int Year { get; set; }

        [StringLength(180)]
        public string? Slug { get; set; }

        public bool IsActive { get; set; } = true; // <--- ĐÃ THÊM ĐỂ SỬA LỖI (Dùng cho quản lý nội bộ)
        public bool IsPublished { get; set; } = true; // (Dùng để hiển thị ra web khách hàng)

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        [Timestamp]
        public byte[]? RowVersion { get; set; }

        // Foreign Keys
        public int BrandId { get; set; }
        public Brand? Brand { get; set; }
        public int CategoryId { get; set; }
        public Category? Category { get; set; }
        public ICollection<BranchInventory> BranchInventories { get; set; } = new List<BranchInventory>();

        // Navigations
        public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
        public ICollection<ProductSpecification> Specifications { get; set; } = new List<ProductSpecification>();
        public ICollection<ProductTag> ProductTags { get; set; } = new List<ProductTag>();

        // Helper lấy ảnh đại diện
        [NotMapped]
        public string? PrimaryImageUrl =>
            (Images?.Count > 0
                ? (Images!.OrderBy(i => i.SortOrder).FirstOrDefault(i => i.IsPrimary)?.ImageUrl
                   ?? Images!.OrderBy(i => i.SortOrder).First().ImageUrl)
                : null)
            ?? ImageUrl;
        // ...

        public ICollection<ProductReview> Reviews { get; set; } = new List<ProductReview>();

        // ===== Helper rating =====
        // Đổi thành property thường để lưu vào Database.
        public double AverageRating { get; set; } = 0;

        public int ReviewCount { get; set; } = 0;
    }
}