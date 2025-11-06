using MotorShop.Models;
using System.Collections.Generic;

namespace MotorShop.ViewModels
{
    public class ProductDetailViewModel
    {
        public Product Product { get; set; } = null!;
        public List<Product> RelatedProducts { get; set; } = new();

        // Tuỳ chọn
        public bool InStock => Product?.StockQuantity > 0;
    }
}
