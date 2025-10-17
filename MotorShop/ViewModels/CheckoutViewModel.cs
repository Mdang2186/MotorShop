using MotorShop.Models; // Đảm bảo bạn có using này
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MotorShop.ViewModels
{
    public class CheckoutViewModel
    {
        // THAY ĐỔI Ở ĐÂY:
        public List<CartItem> CartItems { get; set; } = new List<CartItem>();

        // Các thông tin giao hàng
        [Required(ErrorMessage = "Vui lòng nhập họ tên người nhận.")]
        [Display(Name = "Họ và Tên")]
        public string CustomerName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng.")]
        [Display(Name = "Địa chỉ giao hàng")]
        public string ShippingAddress { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [Display(Name = "Số điện thoại")]
        public string ShippingPhone { get; set; }
    }
}