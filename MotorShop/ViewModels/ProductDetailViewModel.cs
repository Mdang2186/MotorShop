using MotorShop.Models;
using System.Collections.Generic;

namespace MotorShop.ViewModels
{
    public class ProductDetailViewModel
    {
        public Product Product { get; set; } = null!;
        public List<Product> RelatedProducts { get; set; } = new();
        public List<Branch> Branches { get; set; } = new(); // Thêm dòng này

        public bool InStock => Product?.StockQuantity > 0;
    }
}