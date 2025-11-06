using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MotorShop.Models
{
    public class Shipper
    {
        public int Id { get; set; }

        [Required, StringLength(120)]
        public string Name { get; set; } = "";

        [StringLength(40)]
        public string? Code { get; set; }   // unique gọn gàng: GHN, GHTK...

        [StringLength(20)]
        public string? Phone { get; set; }
        // Shipper.cs
        [StringLength(500)]
        public string? Note { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
