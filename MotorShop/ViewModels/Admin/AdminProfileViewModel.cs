using System.ComponentModel.DataAnnotations;

namespace MotorShop.ViewModels.Admin
{
    public class AdminProfileViewModel
    {
        [Display(Name = "Email đăng nhập")]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(100)]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;

        [Phone]
        [Display(Name = "Số điện thoại")]
        public string? PhoneNumber { get; set; }

        [StringLength(255)]
        [Display(Name = "Địa chỉ")]
        public string? Address { get; set; }
    }
}
