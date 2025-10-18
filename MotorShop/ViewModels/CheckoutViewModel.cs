using MotorShop.Models; // Namespace chứa CartItem
using System.ComponentModel.DataAnnotations;

namespace MotorShop.ViewModels
{
    public class CheckoutViewModel
    {
        // Sử dụng CartItem thay vì OrderItem
        public List<CartItem> CartItems { get; set; } = [];

        [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
        [Display(Name = "Họ và Tên")]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ.")]
        [Display(Name = "Địa chỉ giao hàng")]
        public string ShippingAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        [Display(Name = "Số điện thoại")]
        public string ShippingPhone { get; set; } = string.Empty;
    }
}