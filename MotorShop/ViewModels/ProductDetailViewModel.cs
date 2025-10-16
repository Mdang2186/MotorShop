using MotorShop.Models;

namespace MotorShop.ViewModels
{
    public class ProductDetailViewModel
    {
        public Product Product { get; set; } = null!;
        public List<Product> RelatedProducts { get; set; } = new List<Product>();
    }
}