using Microsoft.EntityFrameworkCore;
using MotorShop.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;

namespace MotorShop.Data.Seeders
{
    public static class ProductSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            if (await context.Products.AnyAsync()) return;

            var honda = await context.Brands.FirstAsync(b => b.Name == "Honda");
            var yamaha = await context.Brands.FirstAsync(b => b.Name == "Yamaha");
            var suzuki = await context.Brands.FirstAsync(b => b.Name == "Suzuki");
            var vespa = await context.Brands.FirstAsync(b => b.Name == "Vespa");
            var vinfast = await context.Brands.FirstAsync(b => b.Name == "VinFast");
            var ducati = await context.Brands.FirstAsync(b => b.Name == "Ducati");
            var bmw = await context.Brands.FirstAsync(b => b.Name == "BMW Motorrad");

            var tayGa = await context.Categories.FirstAsync(c => c.Name == "Xe Tay Ga");
            var xeSo = await context.Categories.FirstAsync(c => c.Name == "Xe Số");
            var conTay = await context.Categories.FirstAsync(c => c.Name == "Xe Côn Tay");
            var sport = await context.Categories.FirstAsync(c => c.Name == "Sportbike");
            var naked = await context.Categories.FirstAsync(c => c.Name == "Naked Bike");
            var xeDien = await context.Categories.FirstAsync(c => c.Name == "Xe Điện");

            var products = new List<Product>
            {// --- HONDA (10 SP) ---
new() { Name = "Honda Vision 2025 Thể thao", SKU = "HD-VIS25-SPT", Price = 37000000, StockQuantity = 50, Year = 2025, BrandId = honda.Id, CategoryId = tayGa.Id, IsActive = true, ImageUrl = "images/products/honda/Vision_The_thao_Xam_den.png" },
new() { Name = "Honda SH 160i ABS Đặc biệt", SKU = "HD-SH160-SPE", Price = 104990000, StockQuantity = 15, Year = 2024, BrandId = honda.Id, CategoryId = tayGa.Id, IsActive = true, ImageUrl = "images/products/honda/SH160i_Dac_biet_Den.png" },
new() { Name = "Honda SH Mode 125cc Cao cấp", SKU = "HD-SHMODE-PRE", Price = 63290000, StockQuantity = 30, Year = 2024, BrandId = honda.Id, CategoryId = tayGa.Id, IsActive = true, ImageUrl = "images/products/honda/SH_Mode_Cao_cap_Do_den.png" },
new() { Name = "Honda Air Blade 160 ABS", SKU = "HD-AB160-STD", Price = 56690000, StockQuantity = 40, Year = 2025, BrandId = honda.Id, CategoryId = tayGa.Id, IsActive = true, ImageUrl = "images/products/honda/AB160_Dac_biet_Xanh_xam_den.png" },
new() { Name = "Honda Lead 125cc Đặc biệt", SKU = "HD-LEAD-SPE", Price = 45500000, StockQuantity = 45, Year = 2025, BrandId = honda.Id, CategoryId = tayGa.Id, IsActive = true, ImageUrl = "images/products/honda/LEAD_Dac_biet_Den.png" },
new() { Name = "Honda Vario 160 Thể thao", SKU = "HD-VAR160-SPT", Price = 55490000, StockQuantity = 25, Year = 2024, BrandId = honda.Id, CategoryId = tayGa.Id, IsActive = true, ImageUrl = "images/products/honda/Vario160_The_thao_Xam_den.png" },
new() { Name = "Honda Wave Alpha 110cc", SKU = "HD-WA110", Price = 18900000, StockQuantity = 100, Year = 2024, BrandId = honda.Id, CategoryId = xeSo.Id, IsActive = true, ImageUrl = "images/products/honda/Wave_Alpha_Tieu_chuan_Trang.png" },
new() { Name = "Honda Future 125 FI", SKU = "HD-FUT125", Price = 32500000, StockQuantity = 60, Year = 2024, BrandId = honda.Id, CategoryId = xeSo.Id, IsActive = true, ImageUrl = "images/products/honda/Future_Cao_cap_Xanh_den.png" },
new() { Name = "Honda Winner X Thể thao ABS", SKU = "HD-WINX-SPT", Price = 50560000, StockQuantity = 50, Year = 2024, BrandId = honda.Id, CategoryId = conTay.Id, IsActive = true, ImageUrl = "images/products/honda/WinnerX_The_thao_Do_den_trang.png" },
new() { Name = "Honda CBR150R", SKU = "HD-CBR150R", Price = 72290000, StockQuantity = 10, Year = 2023, BrandId = honda.Id, CategoryId = sport.Id, IsActive = true, ImageUrl = "images/products/honda/CBR150R_The_thao_Do.png" },
                // --- YAMAHA (8 SP) --- 
new() { Name = "Yamaha Grande Hybrid Giới hạn", SKU = "YM-GRD-LTD", Price = 51900000, StockQuantity = 30, Year = 2024, BrandId = yamaha.Id, CategoryId = tayGa.Id, IsActive = true, ImageUrl = "images/products/yamaha/Grande-Hybrid-Gioi-han-Hong-Bac.png" },
new() { Name = "Yamaha NVX 155 VVA Monster", SKU = "YM-NVX-MON", Price = 56000000, StockQuantity = 20, Year = 2024, BrandId = yamaha.Id, CategoryId = tayGa.Id, IsActive = true, ImageUrl = "images/products/yamaha/NVX-155-VVA-Monster-1.png" },
new() { Name = "Yamaha Janus Cao cấp", SKU = "YM-JAN-PRE", Price = 29000000, StockQuantity = 60, Year = 2024, BrandId = yamaha.Id, CategoryId = tayGa.Id, IsActive = true, ImageUrl = "images/products/yamaha/Janus-Phien-ban-Gioi-han-Moi-Den-Hong.png" },
new() { Name = "Yamaha Exciter 155 VVA-ABS GP", SKU = "YM-EX155-GP", Price = 55000000, StockQuantity = 40, Year = 2024, BrandId = yamaha.Id, CategoryId = conTay.Id, IsActive = true, ImageUrl = "images/products/yamaha/Exciter-155-VVA-ABS-GP-2024.png" },
new() { Name = "Yamaha Sirius FI RC", SKU = "YM-SIR-RC", Price = 24300000, StockQuantity = 80, Year = 2024, BrandId = yamaha.Id, CategoryId = xeSo.Id, IsActive = true, ImageUrl = "images/products/yamaha/Sirius-FI-RC-Den-1.png" },
new() { Name = "Yamaha YZF-R15M", SKU = "YM-R15M", Price = 87000000, StockQuantity = 8, Year = 2023, BrandId = yamaha.Id, CategoryId = sport.Id, IsActive = true, ImageUrl = "images/products/yamaha/R15M-Monster-Energy-Yamaha-MotoGP-Black-1.png" },
new() { Name = "Yamaha MT-15", SKU = "YM-MT15", Price = 69000000, StockQuantity = 12, Year = 2023, BrandId = yamaha.Id, CategoryId = naked.Id, IsActive = true, ImageUrl = "images/products/yamaha/MT-15-Xanh-Xam-1.png" },
new() { Name = "Yamaha PG-1", SKU = "YM-PG1", Price = 30437000, StockQuantity = 35, Year = 2024, BrandId = yamaha.Id, CategoryId = xeSo.Id, IsActive = true, ImageUrl = "images/products/yamaha/PG-1-Cam-Bac.png" },
              // --- SUZUKI (3 SP) ---
new() { Name = "Suzuki Raider R150", SKU = "SZ-RAI150", Price = 51190000, StockQuantity = 15, Year = 2023, BrandId = suzuki.Id, CategoryId = conTay.Id, IsActive = true, ImageUrl = "images/products/suzuki/Raider-R150-Xanh-Den.png" },
new() { Name = "Suzuki Satria F150", SKU = "SZ-SAT150", Price = 53490000, StockQuantity = 15, Year = 2023, BrandId = suzuki.Id, CategoryId = conTay.Id, IsActive = true, ImageUrl = "images/products/suzuki/Satria-F150-Xanh-Bac-Den.png" },
new() { Name = "Suzuki Burgman Street", SKU = "SZ-BURG125", Price = 48600000, StockQuantity = 10, Year = 2023, BrandId = suzuki.Id, CategoryId = tayGa.Id, IsActive = true, ImageUrl = "images/products/suzuki/Burgman-Street-Trang-Vang-Dong.png" },
               // --- VESPA/PIAGGIO (3 SP) ---
new() { Name = "Vespa Sprint S 150 TFT", SKU = "PG-VESPA-SPR", Price = 110000000, StockQuantity = 8, Year = 2024, BrandId = vespa.Id, CategoryId = tayGa.Id, IsActive = true, ImageUrl = "images/products/vespa/vespa-sprint-s-150-tft-bronze-antico.png" },
new() { Name = "Vespa Primavera Color Vibe", SKU = "PG-VESPA-PRI", Price = 86600000, StockQuantity = 12, Year = 2023, BrandId = vespa.Id, CategoryId = tayGa.Id, IsActive = true, ImageUrl = "images/products/vespa/primavera-color-vibe-125-arancio-impulsivo.png" },
new() { Name = "Piaggio Liberty S 125 ABS", SKU = "PG-LIB-S", Price = 57700000, StockQuantity = 20, Year = 2023, BrandId = vespa.Id, CategoryId = tayGa.Id, IsActive = true, ImageUrl = "images/products/vespa/liberty-s-125-nero-meteora.png" },
               // --- VINFAST (3 SP) ---
new() { Name = "VinFast Klara S (2022)", SKU = "VF-KLARA-S", Price = 35000000, StockQuantity = 30, Year = 2024, BrandId = vinfast.Id, CategoryId = xeDien.Id, IsActive = true, ImageUrl = "images/products/vinfast/klara-s-red.png" },
new() { Name = "VinFast Vento S", SKU = "VF-VENTO-S", Price = 50000000, StockQuantity = 20, Year = 2024, BrandId = vinfast.Id, CategoryId = xeDien.Id, IsActive = true, ImageUrl = "images/products/vinfast/vento-s-yellow.png" },
new() { Name = "VinFast Evo 200 Lite", SKU = "VF-EVO200", Price = 18000000, StockQuantity = 60, Year = 2024, BrandId = vinfast.Id, CategoryId = xeDien.Id, IsActive = true, ImageUrl = "images/products/vinfast/evo200-yellow.png" },
               // --- PKL (4 SP) ---
new() { Name = "Ducati Panigale V4 S", SKU = "DC-PANV4S", Price = 945000000, StockQuantity = 2, Year = 2024, BrandId = ducati.Id, CategoryId = sport.Id, IsActive = true, ImageUrl = "images/products/ducati/panigale-v4-s-red.png" },
new() { Name = "Ducati Monster 937", SKU = "DC-MNS937", Price = 439000000, StockQuantity = 4, Year = 2023, BrandId = ducati.Id, CategoryId = naked.Id, IsActive = true, ImageUrl = "images/products/ducati/monster-937-red.png" },
new() { Name = "BMW S 1000 RR", SKU = "BMW-S1000RR", Price = 949000000, StockQuantity = 3, Year = 2023, BrandId = bmw.Id, CategoryId = sport.Id, IsActive = true, ImageUrl = "images/products/bmw/s-1000-rr-m-package.png" },
new() { Name = "BMW R 1250 GS Adventure", SKU = "BMW-R1250GSA", Price = 739000000, StockQuantity = 2, Year = 2023, BrandId = bmw.Id, CategoryId = sport.Id, IsActive = true, ImageUrl = "images/products/bmw/r-1250-gs-adventure-triple-black.png" },     };

            foreach (var p in products)
            {
                p.Description = GenerateDescription(p);

                // Xác định folder theo BrandId
                var brandFolder =
                    p.BrandId == honda.Id ? "honda" :
                    p.BrandId == yamaha.Id ? "yamaha" :
                    p.BrandId == suzuki.Id ? "suzuki" :
                    p.BrandId == vespa.Id ? "vespa" :
                    p.BrandId == vinfast.Id ? "vinfast" :
                    p.BrandId == ducati.Id ? "ducati" :
                    p.BrandId == bmw.Id ? "bmw" :
                    "others";

                // Lấy tên file từ URL gốc (link CDN)
                var fileName = p.ImageUrl?.Substring(p.ImageUrl.LastIndexOf('/') + 1)
                               ?? "no-image.png";

                // Base folder local theo thương hiệu
                var baseFolder = $"/images/products/{brandFolder}";
                var fileNameNoExt = Path.GetFileNameWithoutExtension(fileName);
                var ext = Path.GetExtension(fileName);

                // Cập nhật lại ảnh chính về đường dẫn local
                p.ImageUrl = $"{baseFolder}/{fileNameNoExt}{ext}";

                // Tạo 3 ảnh góc 1–3: ...-goc1, ...-goc2, ...-goc3
                for (int i = 1; i <= 3; i++)
                {
                    var imgUrl = $"{baseFolder}/{fileNameNoExt}-goc{i}{ext}";

                    p.Images.Add(new ProductImage
                    {
                        ImageUrl = imgUrl,
                        Caption = $"{p.Name} - Góc {i}",
                        SortOrder = i,
                        IsPrimary = (i == 1)   // Góc 1 là ảnh chính
                    });
                }
            }

            context.Products.AddRange(products);
            await context.SaveChangesAsync();
        }


        private static string GenerateDescription(Product p) =>
            $"<p><strong>{p.Name}</strong> là sự lựa chọn tuyệt vời trong tầm giá {p.Price:N0} VNĐ. " +
            $"Sản phẩm chính hãng từ {p.Brand?.Name ?? "hãng"}, bảo hành dài hạn, thiết kế hiện đại và động cơ bền bỉ.</p>" +
            "<ul><li>Động cơ mạnh mẽ, tiết kiệm nhiên liệu.</li><li>Trang bị an toàn tiên tiến.</li><li>Phù hợp cho cả di chuyển hàng ngày và đường trường.</li></ul>";
    }
}