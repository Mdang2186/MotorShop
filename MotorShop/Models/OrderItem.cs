using System.ComponentModel.DataAnnotations.Schema;

namespace MotorShop.Models
{
    // Chi tiết một sản phẩm trong đơn hàng
    public class OrderItem
    {
        public int Id { get; set; }
        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal UnitPrice { get; set; }

        // Khóa ngoại
        public int OrderId { get; set; }
        public int ProductId { get; set; }

        // Thuộc tính điều hướng
        public Order Order { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }
}