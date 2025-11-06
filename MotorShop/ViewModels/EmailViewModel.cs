using System.ComponentModel.DataAnnotations;

namespace MotorShop.ViewModels
{
    public class EmailViewModel
    {
        [Display(Name = "Email hiện tại")]
        [EmailAddress(ErrorMessage = "Định dạng Email không hợp lệ.")]
        public string Email { get; set; } = string.Empty;

        public bool IsEmailConfirmed { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Email mới.")]
        [EmailAddress(ErrorMessage = "Định dạng Email không hợp lệ.")]
        [Display(Name = "Email mới")]
        public string NewEmail { get; set; } = string.Empty;
    }
}
