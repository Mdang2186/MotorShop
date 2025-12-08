using MotorShop.Models;
using System.Collections.Generic;

namespace MotorShop.ViewModels.Home
{
    public class HomeViewModel
    {
        // 1. Xe tiêu điểm / Bán chạy (Hiển thị ô to nổi bật)
        public List<Product> BestSellers { get; set; } = new();

        // 2. Xe máy mới về (Hiển thị Grid chuẩn)
        public List<Product> FeaturedProducts { get; set; } = new();

        // 3. Phụ tùng & Linh kiện mới nhất
        public List<Product> LatestParts { get; set; } = new();

        // 4. Dữ liệu bổ trợ
        public List<Category> Categories { get; set; } = new();
        public List<Brand> Brands { get; set; } = new();

        // 5. Danh sách Showroom (Để hiển thị Map)
        public List<Branch> Branches { get; set; } = new();
    }
}