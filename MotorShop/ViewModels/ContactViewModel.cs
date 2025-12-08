using System.ComponentModel.DataAnnotations;

namespace MotorShop.ViewModels
{
    public class ContactRequestViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string Phone { get; set; } = string.Empty;

        // Thêm trường Email theo yêu cầu
        [Required(ErrorMessage = "Vui lòng nhập Email để chúng tôi liên hệ")]
        [EmailAddress(ErrorMessage = "Định dạng Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập nhu cầu")]
        public string RequestContent { get; set; } = string.Empty;
    }
}