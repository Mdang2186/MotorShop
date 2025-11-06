using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MotorShop.Models
{
    public class ApplicationUser : IdentityUser
    {
        [StringLength(100)]
        public string? FullName { get; set; }

        [StringLength(255)]
        public string? Address { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Order> Orders { get; set; } = new List<Order>();

        // Ảnh đại diện (đường dẫn tĩnh trong wwwroot), ví dụ: /images/avatars/xxx.webp
        [StringLength(255)]
        public string? AvatarUrl { get; set; }

        // === OTP xác minh email (6 số) ===
        [StringLength(6)]
        public string? EmailOtpCode { get; set; }          // Mã 6 số

        public DateTime? EmailOtpExpiryUtc { get; set; }   // Hạn dùng OTP (UTC)

        // Tiện dụng: tên hiển thị
        public string DisplayName => string.IsNullOrWhiteSpace(FullName) ? (Email ?? UserName ?? "User") : FullName!;
    }
}
