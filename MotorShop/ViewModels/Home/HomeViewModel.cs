using MotorShop.Models;
using System.Collections.Generic;

namespace MotorShop.ViewModels.Home
{
    public class HomeViewModel
    {
        // Khởi tạo sẵn để View không bao giờ null
        public List<Product> FeaturedProducts { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public List<Brand> Brands { get; set; } = new();
    }
}
