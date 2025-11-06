using System.ComponentModel.DataAnnotations;

namespace MotorShop.ViewModels.Account
{
    public class LoginViewModel
    {
        private string _email = string.Empty;
        private string _password = string.Empty;
        private string? _returnUrl;

        [Display(Name = "Email")]
        [Required(ErrorMessage = "Vui lòng nhập Email.")]
        [EmailAddress(ErrorMessage = "Định dạng Email không hợp lệ.")]
        [StringLength(255, ErrorMessage = "Email tối đa 255 ký tự.")]
        public string Email
        {
            get => _email;
            set => _email = (value ?? string.Empty).Trim();
        }

        [Display(Name = "Mật khẩu")]
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu từ 6–100 ký tự.")]
        public string Password
        {
            get => _password;
            set => _password = value ?? string.Empty; // không trim mật khẩu
        }

        [Display(Name = "Ghi nhớ đăng nhập")]
        public bool RememberMe { get; set; }

        // Giữ đường dẫn gốc để redirect sau khi đăng nhập
        public string? ReturnUrl
        {
            get => _returnUrl;
            set => _returnUrl = value?.Trim();
        }
    }
}
