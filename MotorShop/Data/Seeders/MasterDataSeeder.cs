using Microsoft.EntityFrameworkCore;
using MotorShop.Models;
using System.Threading.Tasks;

namespace MotorShop.Data.Seeders
{
    public static class MasterDataSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            // --- 3.1. Chi nhánh (5) ---
            if (!await context.Branches.AnyAsync())
            {
                context.Branches.AddRange(
        new Branch
        {
            Name = "MotorShop Hà Nội 1",
            Code = "HN-HBT",
            Address = "88 Phố Huế, Hai Bà Trưng, Hà Nội",
            Phone = "024.3971.8888",
            OpeningHours = "08:00–20:30 (Thứ 2 – Chủ nhật)",
            MapUrl = "https://www.google.com/maps/search/?api=1&query=88+Pho+Hue,+Hai+Ba+Trung,+Ha+Noi",
            IsActive = true,
            Latitude = 21.0135,   // có thể chỉnh lại cho đúng
            Longitude = 105.8541
        },
        new Branch
        {
            Name = "MotorShop Hà Nội 2",
            Code = "HN-CG",
            Address = "368 Cầu Giấy, Q. Cầu Giấy, Hà Nội",
            Phone = "024.3767.9999",
            OpeningHours = "08:00–20:30 (Thứ 2 – Chủ nhật)",
            MapUrl = "https://www.google.com/maps/search/?api=1&query=368+Cau+Giay,+Cau+Giay,+Ha+Noi",
            IsActive = true,
            Latitude = 21.0369,
            Longitude = 105.7906
        },
        new Branch
        {
            Name = "MotorShop Đà Nẵng",
            Code = "DN-HC",
            Address = "255 Hùng Vương, Q. Hải Châu, Đà Nẵng",
            Phone = "0236.382.7777",
            OpeningHours = "08:00–20:00 (Thứ 2 – Chủ nhật)",
            MapUrl = "https://www.google.com/maps/search/?api=1&query=255+Hung+Vuong,+Hai+Chau,+Da+Nang",
            IsActive = true,
            Latitude = 16.0678,
            Longitude = 108.2208
        },
        new Branch
        {
            Name = "MotorShop Sài Gòn 1",
            Code = "HCM-Q1",
            Address = "15 Nguyễn Huệ, Quận 1, TP.HCM",
            Phone = "028.3822.6666",
            OpeningHours = "09:00–21:00 (Thứ 2 – Chủ nhật)",
            MapUrl = "https://www.google.com/maps/search/?api=1&query=15+Nguyen+Hue,+Quan+1,+TP+HCM",
            IsActive = true,
            Latitude = 10.7731,
            Longitude = 106.7042
        },
        new Branch
        {
            Name = "MotorShop Sài Gòn 2",
            Code = "HCM-TB",
            Address = "456 Cộng Hòa, Q. Tân Bình, TP.HCM",
            Phone = "028.3811.5555",
            OpeningHours = "09:00–21:00 (Thứ 2 – Chủ nhật)",
            MapUrl = "https://www.google.com/maps/search/?api=1&query=456+Cong+Hoa,+Tan+Binh,+TP+HCM",
            IsActive = true,
            Latitude = 10.8009,
            Longitude = 106.6477
        }
    );

            }

            // --- 3.2. Thương hiệu (10) ---
            if (!await context.Brands.AnyAsync())
            {
                context.Brands.AddRange(
                    new Brand { Name = "Honda" }, new Brand { Name = "Yamaha" }, new Brand { Name = "Suzuki" },
                    new Brand { Name = "Piaggio" }, new Brand { Name = "Vespa" }, new Brand { Name = "SYM" },
                    new Brand { Name = "Ducati" }, new Brand { Name = "BMW Motorrad" }, new Brand { Name = "Kawasaki" },
                    new Brand { Name = "VinFast" }
                );
            }

            // --- 3.3. Danh mục (CÓ MÔ TẢ) (9) ---
            if (!await context.Categories.AnyAsync())
            {
                context.Categories.AddRange(
                    new Category { Name = "Xe Tay Ga", Description = "Xe máy vận hành tự động, thiết kế thời trang, tiện lợi cho đô thị." },
                    new Category { Name = "Xe Số", Description = "Xe máy sử dụng hộp số cơ khí, bền bỉ và tiết kiệm nhiên liệu." },
                    new Category { Name = "Xe Côn Tay", Description = "Xe thể thao với hộp số tay côn, mang lại cảm giác lái phấn khích." },
                    new Category { Name = "Sportbike", Description = "Dòng xe mô tô thể thao với thiết kế khí động học, tốc độ cao." },
                    new Category { Name = "Naked Bike", Description = "Xe mô tô phân khối lớn với thiết kế để lộ động cơ cơ bắp." },
                    new Category { Name = "Adventure", Description = "Dòng xe đa dụng, phù hợp cho những chuyến đi đường dài." },
                    new Category { Name = "Cruiser", Description = "Thiết kế cổ điển, tư thế ngồi thoải mái cho đường trường." },
                    new Category { Name = "Classic", Description = "Thiết kế hoài cổ, vượt thời gian." },
                    new Category { Name = "Xe Điện", Description = "Phương tiện di chuyển xanh, sử dụng động cơ điện thân thiện môi trường." },
                    new Category
                    {
                        Name = "Phụ tùng & Linh kiện",
                        Description = "Phụ tùng, linh kiện, dầu nhớt và vật tư bảo dưỡng chính hãng cho các dòng xe máy, xe tay ga, xe điện."
                    }

                );
            }

            await context.SaveChangesAsync();
        }
    }
}