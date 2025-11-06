// Models/Bank.cs
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore; // để dùng [Index]

namespace MotorShop.Models
{
    [Index(nameof(Code), IsUnique = true)]
    [Index(nameof(Bin), IsUnique = true)]
    public class Bank
    {
        public int Id { get; set; }

        [Required, StringLength(120)]
        public string Name { get; set; } = "";

        [StringLength(30)]
        public string? ShortName { get; set; } // VCB, ACB, TCB...

        // Mã nội bộ ổn định để binding (khuyên: viết thường, không dấu, duy nhất)
        [Required, StringLength(20)]
        public string Code { get; set; } = "";

        // BIN 6 chữ số cho thẻ nội địa VN
        [StringLength(10)]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "BIN phải gồm 6 chữ số.")]
        public string? Bin { get; set; }

        [StringLength(500)]
        public string? LogoUrl { get; set; }   // /images/banks/vcb.svg

        public int SortOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;
    }
}
