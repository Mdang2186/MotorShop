using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MotorShop.Models
{
    /// <summary>
    /// Chi nhánh để khách nhận xe tại cửa hàng.
    /// </summary>
    public class Branch
    {
        public int Id { get; set; }

        [Required, StringLength(120)]
        public string Name { get; set; } = null!;           // Ví dụ: "MotorShop Hà Nội - Hoàn Kiếm"

        [StringLength(80)]
        public string? Code { get; set; }                   // Ví dụ: "HN-HK" (slug/mã nội bộ)

        [Required, StringLength(255)]
        public string Address { get; set; } = null!;        // Địa chỉ hiển thị

        [StringLength(50)]
        public string? Phone { get; set; }                  // Hotline chi nhánh (nếu có)

        [StringLength(120)]
        public string? OpeningHours { get; set; }           // "08:00–20:00 (T2–CN)"

        [StringLength(255)]
        public string? MapUrl { get; set; }                 // Link Google Maps (tuỳ chọn)

        public bool IsActive { get; set; } = true;          // Đang hoạt động hay không

        // Tuỳ chọn: toạ độ để map/SEO (không bắt buộc)
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        // Điều hướng: các đơn hàng chọn nhận tại chi nhánh này
        public ICollection<Order> PickupOrders { get; set; } = new List<Order>();
    }
}
