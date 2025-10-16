using MotorShop.Models;

namespace MotorShop.ViewModels
{
    public class CheckoutViewModel
    {
        public List<OrderItem> CartItems { get; set; } = new List<OrderItem>();

        // Thông tin giao hàng
        public required string CustomerName { get; set; }
        public required string ShippingAddress { get; set; }
        public required string ShippingPhone { get; set; }
    }
}