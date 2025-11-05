using System.ComponentModel.DataAnnotations;

namespace MotorShop.ViewModels.Account
{
    public class VerifyEmailCodeViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập Email.")]
        [EmailAddress(ErrorMessage = "Định dạng Email không hợp lệ.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mã xác nhận.")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Mã gồm đúng 6 chữ số.")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Mã phải gồm 6 chữ số.")]
        [Display(Name = "Mã xác nhận 6 số")]
        public string Code { get; set; } = string.Empty;

    }
}
