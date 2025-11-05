using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http; // IFormFile

namespace MotorShop.ViewModels
{
    public class UpdateProfileViewModel
    {
        // Chỉ để hiển thị (readonly ở View)
        [Display(Name = "Tên đăng nhập")]
        public string Username { get; set; } = string.Empty;

        [Display(Name = "Họ và tên")]
        [StringLength(100)]
        public string? FullName { get; set; }

        [Display(Name = "Số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Địa chỉ")]
        [StringLength(255)]
        public string? Address { get; set; }

        // Avatar hiện tại để render ra <img>
        public string? CurrentAvatarUrl { get; set; }

        // Ảnh đại diện mới (tùy chọn)
        [Display(Name = "Ảnh đại diện mới")]
        public IFormFile? AvatarFile { get; set; }
    }
}
