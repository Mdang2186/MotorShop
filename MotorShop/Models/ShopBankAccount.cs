using System.ComponentModel.DataAnnotations;

namespace MotorShop.Models
{
    /// <summary>
    /// Tài khoản ngân hàng của MotorShop dùng để nhận tiền chuyển khoản.
    /// </summary>
    public class ShopBankAccount
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Ngân hàng")]
        public int BankId { get; set; }

        public Bank Bank { get; set; } = null!; // navigation

        [Required, StringLength(30)]
        [Display(Name = "Số tài khoản")]
        public string AccountNumber { get; set; } = string.Empty;

        [Required, StringLength(120)]
        [Display(Name = "Chủ tài khoản")]
        public string AccountName { get; set; } = string.Empty;

        [StringLength(120)]
        [Display(Name = "Chi nhánh")]
        public string? Branch { get; set; }

        [StringLength(300)]
        [Display(Name = "Ghi chú")]
        public string? Note { get; set; }

        [Display(Name = "Mặc định")]
        public bool IsDefault { get; set; }

        [Display(Name = "Đang sử dụng")]
        public bool IsActive { get; set; } = true;
    }
}
