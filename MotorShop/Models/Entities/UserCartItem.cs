using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MotorShop.Models; // Product

namespace MotorShop.Models.Entities
{
    /// <summary>
    /// Mỗi dòng tương ứng 1 sản phẩm trong giỏ của 1 user (lưu trong DB).
    /// </summary>
    public class UserCartItem
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Khóa ngoại tới AspNetUsers.Id (ApplicationUser.Id)
        /// </summary>
        [Required]
        public string UserId { get; set; } = null!;

        [Required]
        public int ProductId { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        /// <summary>
        /// Đơn giá tại thời điểm hiện tại (đã sync từ Product.Price).
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        // Navigation
        public Product? Product { get; set; }
    }
}
