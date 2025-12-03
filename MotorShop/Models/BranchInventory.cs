using System.ComponentModel.DataAnnotations;

namespace MotorShop.Models
{
    public class BranchInventory
    {
        public int Id { get; set; }

        [Required]
        public int BranchId { get; set; }

        [Required]
        public int ProductId { get; set; }

        /// <summary>
        /// Số lượng còn lại của sản phẩm tại chi nhánh này.
        /// </summary>
        public int Quantity { get; set; }

        // Navigation
        public Branch Branch { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }
}
