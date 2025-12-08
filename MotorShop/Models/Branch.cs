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
        public string Name { get; set; } = null!;

        [StringLength(80)]
        public string? Code { get; set; }

        [Required, StringLength(255)]
        public string Address { get; set; } = null!;

        [StringLength(50)]
        public string? Phone { get; set; }

        [StringLength(120)]
        public string? OpeningHours { get; set; }

        [StringLength(255)]
        public string? MapUrl { get; set; }

        public bool IsActive { get; set; } = true;

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        // Đơn hàng nhận xe tại chi nhánh này
        public ICollection<Order> PickupOrders { get; set; } = new List<Order>();

        /// <summary>
        /// Tồn kho các sản phẩm tại chi nhánh này.
        /// </summary>
        public ICollection<BranchInventory> Inventories { get; set; } = new List<BranchInventory>();
    }

}
