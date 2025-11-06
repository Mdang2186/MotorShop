using MotorShop.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
// OrderItem.cs
namespace MotorShop.Models
{
    public class OrderItem
    {
        public int Id { get; set; }

        [Range(1, 100000)]
        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        [Range(0, 100000000)]
        public decimal UnitPrice { get; set; }

        // FK
        public int OrderId { get; set; }
        public int ProductId { get; set; }

        public Order Order { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }
}