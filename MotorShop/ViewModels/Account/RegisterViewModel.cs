using System.ComponentModel.DataAnnotations;

namespace MotorShop.ViewModels.Account
{
    public class RegisterViewModel
    {
        private string _fullName = string.Empty;
        private string _email = string.Empty;
        private string _password = string.Empty;
        private string _confirmPassword = string.Empty;
        private string? _address;
        private string? _phoneNumber;

        [Display(Name = "Họ và Tên")]
        [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
        [StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự.")]
        public string FullName
        {
            get => _fullName;
            set => _fullName = (value ?? string.Empty).Trim();
        }

        [Display(Name = "Email")]
        [Required(ErrorMessage = "Vui lòng nhập Email.")]
        [EmailAddress(ErrorMessage = "Định dạng Email không hợp lệ.")]
        [StringLength(255, ErrorMessage = "Email tối đa 255 ký tự.")]
        public string Email
        {
            get => _email;
            set => _email = (value ?? string.Empty).Trim().ToLowerInvariant();
        }

        [Display(Name = "Mật khẩu")]
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu từ {2}–{1} ký tự.")]
        [DataType(DataType.Password)]
        public string Password
        {
            get => _password;
            set => _password = value ?? string.Empty; // không trim mật khẩu
        }

        [Display(Name = "Xác nhận mật khẩu")]
        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu.")]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Mật khẩu và xác nhận mật khẩu không khớp.")]
        public string ConfirmPassword
        {
            get => _confirmPassword;
            set => _confirmPassword = value ?? string.Empty; // không trim
        }

        [Display(Name = "Địa chỉ")]
        [StringLength(255, ErrorMessage = "Địa chỉ tối đa 255 ký tự.")]
        public string? Address
        {
            get => _address;
            set => _address = value?.Trim();
        }

        [Display(Name = "Số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        [StringLength(20, ErrorMessage = "Số điện thoại tối đa 20 ký tự.")]
        public string? PhoneNumber
        {
            get => _phoneNumber;
            set => _phoneNumber = value?.Trim();
        }
    }
}
