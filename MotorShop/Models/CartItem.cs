using System.ComponentModel.DataAnnotations;

namespace MotorShop.Models
{
    /// <summary>
    /// Đại diện cho một sản phẩm trong giỏ hàng của 1 user.
    /// Bây giờ dùng làm entity để lưu trong CSDL.
    /// </summary>
    public class CartItem
    {
        // Khóa chính
        [Key]
        public int Id { get; set; }

        // Id của ApplicationUser (AspNetUsers.Id)
        [Required]
        public string UserId { get; set; } = null!;

        [Required]
        public int ProductId { get; set; }

        [Required]
        public string ProductName { get; set; } = null!;

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        public string? ImageUrl { get; set; }

        // Tổng tiền cho dòng này
        public decimal Subtotal => Quantity * Price;
    }
}
