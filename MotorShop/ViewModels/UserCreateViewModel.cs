using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MotorShop.ViewModels
{
    // ViewModel cho trang Tạo User
    public class UserCreateViewModel
    {
        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Mật khẩu không khớp.")]
        public string ConfirmPassword { get; set; } = "";

        // Danh sách tất cả các quyền để hiển thị checkbox
        public List<RoleCheckboxVM> Roles { get; set; } = new();
    }
}