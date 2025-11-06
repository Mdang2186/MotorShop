namespace MotorShop.Models
{
    /// <summary>
    /// Đại diện cho một sản phẩm trong giỏ hàng.
    /// Lớp này được tối ưu để lưu trữ trong Session.
    /// </summary>
    public class CartItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }

        // Thuộc tính tính toán tổng tiền cho món hàng này
        public decimal Subtotal => Quantity * Price;
    }
}