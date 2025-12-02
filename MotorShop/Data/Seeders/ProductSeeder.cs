// File: Data/Seeders/ProductSeeder.cs
using Microsoft.EntityFrameworkCore;
using MotorShop.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotorShop.Data.Seeders
{
    public static class ProductSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            // Nếu đã có Products thì không seed lại
            if (await context.Products.AnyAsync()) return;

            // 1. Lấy đầy đủ Brand & Category từ DB (đã seed ở MasterDataSeeder)
            var brands = await context.Brands.ToListAsync();
            var categories = await context.Categories.ToListAsync();

            int GetBrandId(string name) => brands.First(b => b.Name == name).Id;
            int GetCatId(string name) => categories.First(c => c.Name == name).Id;

            // 2. Danh sách sản phẩm (giữ nguyên các sản phẩm đang có trong CSDL)
            var seedItems = new List<ProductSeedItem>
            {
                // ==================== HONDA ====================
                // ==================== HONDA (1-10) ====================

new()
{
    Name = "Honda Vision 2025 Thể thao",
    SKU = "HD-VIS25-SPT",
    Price = 37000000,
    Stock = 50,
    Year = 2025,
    BrandId = GetBrandId("Honda"),
    CategoryId = GetCatId("Xe Tay Ga"),
    ImageName = "Vision_The_thao_Xam_den.png",
    Specs = newDictionary(
        // --- KÍCH THƯỚC & TRỌNG LƯỢNG ---
        "Khối lượng bản thân", "98 kg",
        "Kích thước (D x R x C)", "1.925 x 686 x 1.126 mm",
        "Khoảng cách trục bánh xe", "1.277 mm",
        "Chiều cao yên", "785 mm",
        "Khoảng sáng gầm xe", "130 mm",

        // --- DUNG TÍCH & LỐP / PHUỘC ---
        "Dung tích bình xăng", "4,9 lít",
        "Dung tích nhớt máy", "0,65 lít sau khi xả · 0,8 lít sau khi rã máy",
        "Kích cỡ lốp trước", "80/90-16M/C 43P (không săm)",
        "Kích cỡ lốp sau", "90/90-14M/C 46P (không săm)",
        "Phuộc trước", "Ống lồng, giảm chấn thủy lực",
        "Phuộc sau", "Lò xo trụ đơn, giảm chấn thủy lực",

        // --- ĐỘNG CƠ & VẬN HÀNH ---
        "Loại động cơ", "4 kỳ, 1 xy-lanh, làm mát bằng không khí, eSP",
        "Dung tích xy-lanh", "109,5 cm³",
        "Đường kính x hành trình piston", "47,0 x 63,1 mm (xấp xỉ)",
        "Tỷ số nén", "9,5 : 1 (xấp xỉ)",
        "Công suất tối đa", "6,59 kW / 7.500 vòng/phút",
        "Mô-men xoắn cực đại", "9,29 N.m / 6.000 vòng/phút",
        "Hệ thống nhiên liệu", "Phun xăng điện tử PGM-FI",
        "Mức tiêu thụ nhiên liệu", "≈ 1,8 – 1,9 lít/100 km (điều kiện tiêu chuẩn)",

        // --- TRUYỀN ĐỘNG & TIỆN ÍCH ---
        "Hộp số", "Truyền động vô cấp CVT",
        "Loại truyền động", "Dây đai, biến thiên vô cấp",
        "Hệ thống khởi động", "Điện",
        "Tiện ích nổi bật", "Cốp rộng, móc treo đồ, sàn để chân phẳng, mặt đồng hồ analog + LCD"
    )
},

new()
{
    Name = "Honda SH 160i ABS Đặc biệt",
    SKU = "HD-SH160-SPE",
    Price = 104990000,
    Stock = 15,
    Year = 2024,
    BrandId = GetBrandId("Honda"),
    CategoryId = GetCatId("Xe Tay Ga"),
    ImageName = "SH160i_Dac_biet_Den.png",
    Specs = newDictionary(
        // --- KÍCH THƯỚC & TRỌNG LƯỢNG ---
        "Khối lượng bản thân", "133 kg",
        "Kích thước (D x R x C)", "2.090 x 739 x 1.129 mm",
        "Khoảng cách trục bánh xe", "1.353 mm",
        "Chiều cao yên", "799 mm",
        "Khoảng sáng gầm xe", "146 mm",

        // --- DUNG TÍCH & LỐP / PHUỘC ---
        "Dung tích bình xăng", "7,0 lít",
        "Dung tích cốp xe", "≈ 28 lít",
        "Dung tích nhớt máy", "0,9 lít khi rã máy (xấp xỉ)",
        "Kích cỡ lốp trước", "100/80-16M/C",
        "Kích cỡ lốp sau", "120/80-16M/C 60P",
        "Phuộc trước", "Ống lồng, giảm chấn thủy lực",
        "Phuộc sau", "Lò xo trụ đôi, giảm chấn thủy lực",

        // --- ĐỘNG CƠ & VẬN HÀNH ---
        "Loại động cơ", "PGM-FI, xăng, 4 kỳ, 1 xy-lanh, làm mát bằng dung dịch, eSP+ 4 van",
        "Dung tích xy-lanh", "156,9 cm³",
        "Đường kính x hành trình piston", "60,0 x 55,5 mm",
        "Tỷ số nén", "12,0 : 1",
        "Công suất tối đa", "12,4 kW / 8.500 vòng/phút",
        "Mô-men xoắn cực đại", "14,8 N.m / 6.500 vòng/phút",
        "Mức tiêu thụ nhiên liệu", "≈ 2,3 – 2,4 lít/100 km",
        "Hệ thống nhiên liệu", "Phun xăng điện tử PGM-FI",

        // --- TRUYỀN ĐỘNG & HỆ THỐNG PHANH ---
        "Loại truyền động", "Vô cấp, điều khiển tự động (CVT)",
        "Hộp số", "Tự động vô cấp",
        "Hệ thống phanh trước", "Đĩa thủy lực, ABS 1 kênh (bánh trước)",
        "Hệ thống phanh sau", "Phanh đĩa / tang trống (tùy phiên bản)",
        "Tiện ích nổi bật", "Smart Key, HSTC (kiểm soát lực kéo), đèn LED, hộc sạc, cổng sạc USB"
    )
},

new()
{
    Name = "Honda SH Mode 125cc Cao cấp",
    SKU = "HD-SHMODE-PRE",
    Price = 63290000,
    Stock = 30,
    Year = 2024,
    BrandId = GetBrandId("Honda"),
    CategoryId = GetCatId("Xe Tay Ga"),
    ImageName = "SH_Mode_Cao_cap_Do_den.png",
    Specs = newDictionary(
        // --- KÍCH THƯỚC & TRỌNG LƯỢNG ---
        "Khối lượng bản thân", "116 kg",
        "Kích thước (D x R x C)", "1.950 x 669 x 1.100 mm",
        "Khoảng cách trục bánh xe", "1.304 mm",
        "Chiều cao yên", "765 mm",
        "Khoảng sáng gầm xe", "≈ 130 – 151 mm (tuỳ nguồn công bố)",

        // --- DUNG TÍCH & LỐP / PHUỘC ---
        "Dung tích bình xăng", "5,6 lít",
        "Dung tích nhớt máy", "0,9/0,8 lít (rã máy / thay nhớt)",
        "Kích cỡ lốp trước", "80/90-16M/C 43P",
        "Kích cỡ lốp sau", "100/90-14M/C 57P",
        "Phuộc trước", "Ống lồng, giảm chấn thủy lực",
        "Phuộc sau", "Phuộc đơn, giảm chấn thủy lực",

        // --- ĐỘNG CƠ & VẬN HÀNH ---
        "Loại động cơ", "Xăng, 4 kỳ, 1 xy-lanh, làm mát bằng chất lỏng, eSP+ 4 van",
        "Dung tích xy-lanh", "124,8 cm³",
        "Tỷ số nén", "11,5 : 1",
        "Công suất tối đa", "8,2 kW / 8.500 vòng/phút",
        "Mô-men xoắn cực đại", "11,7 N.m / 5.000 vòng/phút",
        "Hệ thống nhiên liệu", "Phun xăng điện tử PGM-FI",
        "Mức tiêu thụ nhiên liệu", "≈ 2,1 – 2,2 lít/100 km",

        // --- TRUYỀN ĐỘNG & TIỆN ÍCH ---
        "Loại truyền động", "Dây đai, biến thiên vô cấp (CVT)",
        "Hệ thống khởi động", "Điện",
        "Tiện ích nổi bật", "Smart Key, cốp rộng, hộc trước có nắp + cổng sạc, sàn để chân phẳng"
    )
},

new()
{
    Name = "Honda Air Blade 160 ABS",
    SKU = "HD-AB160-STD",
    Price = 56690000,
    Stock = 40,
    Year = 2025,
    BrandId = GetBrandId("Honda"),
    CategoryId = GetCatId("Xe Tay Ga"),
    ImageName = "AB160_Dac_biet_Xanh_xam_den.png",
    Specs = newDictionary(
        // --- KÍCH THƯỚC & TRỌNG LƯỢNG ---
        "Khối lượng bản thân", "114 kg",
        "Kích thước (D x R x C)", "1.890 x 686 x 1.116 mm",
        "Khoảng cách trục bánh xe", "1.286 mm",
        "Chiều cao yên", "775 mm",
        "Khoảng sáng gầm xe", "142 mm",

        // --- DUNG TÍCH & LỐP / PHUỘC ---
        "Dung tích bình xăng", "4,4 lít",
        "Dung tích cốp xe", "23,2 lít (đựng được 2 nón nửa đầu + đồ cá nhân)",
        "Dung tích nhớt máy", "0,8 lít khi thay nhớt · 0,9 lít khi rã máy",
        "Kích cỡ lốp trước", "90/80-14M/C (không săm)",
        "Kích cỡ lốp sau", "100/80-14M/C (không săm)",
        "Phuộc trước", "Ống lồng, giảm chấn thủy lực",
        "Phuộc sau", "Lò xo trụ, giảm chấn thủy lực",

        // --- ĐỘNG CƠ & VẬN HÀNH ---
        "Loại động cơ", "Xăng, 4 kỳ, 1 xy-lanh, làm mát bằng dung dịch, eSP+",
        "Dung tích xy-lanh", "156,9 cm³",
        "Đường kính x hành trình piston", "60,0 x 55,5 mm",
        "Tỷ số nén", "12,0 : 1",
        "Công suất tối đa", "11,2 kW / 8.000 vòng/phút",
        "Mô-men xoắn cực đại", "14,6 N.m / 6.500 vòng/phút",
        "Hệ thống nhiên liệu", "Phun xăng điện tử PGM-FI",
        "Mức tiêu thụ nhiên liệu", "≈ 2,2 – 2,3 lít/100 km",

        // --- TRUYỀN ĐỘNG & TIỆN ÍCH ---
        "Hộp số", "Truyền động vô cấp CVT",
        "Loại truyền động", "Dây đai, biến thiên vô cấp",
        "Hệ thống phanh trước", "Đĩa thủy lực, ABS (tùy phiên bản)",
        "Hệ thống phanh sau", "Phanh tang trống",
        "Tiện ích nổi bật", "Smart Key, đèn LED, sạc USB, cốp rộng có đèn, ổ khóa đa năng"
    )
},

new()
{
    Name = "Honda Lead 125cc Đặc biệt",
    SKU = "HD-LEAD-SPE",
    Price = 45500000,
    Stock = 45,
    Year = 2025,
    BrandId = GetBrandId("Honda"),
    CategoryId = GetCatId("Xe Tay Ga"),
    ImageName = "LEAD_Dac_biet_Den.png",
    Specs = newDictionary(
        // --- KÍCH THƯỚC & TRỌNG LƯỢNG ---
        "Khối lượng bản thân", "≈ 113 – 114 kg",
        "Kích thước (D x R x C)", "1.844 x 680 x 1.130 mm (xấp xỉ)",
        "Khoảng cách trục bánh xe", "≈ 1.273 mm",
        "Chiều cao yên", "760 mm",
        "Khoảng sáng gầm xe", "≈ 120 – 140 mm",

        // --- DUNG TÍCH & LỐP / PHUỘC ---
        "Dung tích bình xăng", "6,0 lít",
        "Dung tích cốp xe", "≈ 37 lít (rất rộng)",
        "Kích cỡ lốp trước", "90/90-12 44J",
        "Kích cỡ lốp sau", "100/90-10 56J",
        "Phuộc trước", "Ống lồng, giảm chấn thủy lực",
        "Phuộc sau", "Lò xo trụ đơn, giảm chấn thủy lực",

        // --- ĐỘNG CƠ & VẬN HÀNH ---
        "Loại động cơ", "Xăng, 4 kỳ, 1 xy-lanh, làm mát bằng chất lỏng, eSP+",
        "Dung tích xy-lanh", "124,8 cm³",
        "Tỷ số nén", "11,5 : 1",
        "Công suất tối đa", "8,22 kW / 8.500 vòng/phút",
        "Mô-men xoắn cực đại", "11,7 N.m / 5.250 vòng/phút",
        "Hệ thống nhiên liệu", "Phun xăng điện tử PGM-FI",
        "Mức tiêu thụ nhiên liệu", "≈ 2,1 lít/100 km",

        // --- TRUYỀN ĐỘNG & TIỆN ÍCH ---
        "Loại truyền động", "Vô cấp (CVT)",
        "Hệ thống phanh trước", "Đĩa thủy lực (có ABS tuỳ phiên bản)",
        "Hệ thống phanh sau", "Phanh cơ (tang trống)",
        "Tiện ích nổi bật", "Cốp siêu rộng, sàn để chân phẳng, móc treo tiện lợi, hộc trước có nắp & sạc"
    )
},

new()
{
    Name = "Honda Vario 160 Thể thao",
    SKU = "HD-VAR160-SPT",
    Price = 55490000,
    Stock = 25,
    Year = 2024,
    BrandId = GetBrandId("Honda"),
    CategoryId = GetCatId("Xe Tay Ga"),
    ImageName = "Vario160_The_thao_Xam_den.png",
    Specs = newDictionary(
        // --- KÍCH THƯỚC & TRỌNG LƯỢNG ---
        "Khối lượng bản thân", "≈ 118 kg (bản Thể thao)",
        "Kích thước (D x R x C)", "1.929 x 695 x 1.088 mm (bản Thể thao)",
        "Khoảng cách trục bánh xe", "1.278 mm",
        "Chiều cao yên", "778 mm",
        "Khoảng sáng gầm xe", "138 mm",

        // --- DUNG TÍCH & LỐP / PHUỘC ---
        "Dung tích bình xăng", "5,5 lít",
        "Dung tích cốp xe", "≈ 18 lít",
        "Kích cỡ lốp trước", "100/80-14M/C 48P",
        "Kích cỡ lốp sau", "120/70-14M/C 61P",
        "Phuộc trước", "Ống lồng, giảm chấn thủy lực",
        "Phuộc sau", "Lò xo trụ đôi, giảm chấn thủy lực",

        // --- ĐỘNG CƠ & VẬN HÀNH ---
        "Loại động cơ", "eSP+, 4 kỳ, 4 van, 1 xy-lanh, làm mát bằng dung dịch",
        "Dung tích xy-lanh", "156,9 cm³",
        "Tỷ số nén", "12,0 : 1",
        "Công suất tối đa", "≈ 11,3 kW / 8.500 vòng/phút",
        "Mô-men xoắn cực đại", "≈ 13,8 N.m / 7.000 vòng/phút",
        "Hệ thống nhiên liệu", "Phun xăng điện tử PGM-FI",
        "Mức tiêu thụ nhiên liệu", "≈ 2,2 – 2,3 lít/100 km",

        // --- TRUYỀN ĐỘNG & TIỆN ÍCH ---
        "Loại truyền động", "Vô cấp (CVT)",
        "Hệ thống phanh trước", "Đĩa thủy lực, có ABS trên một số phiên bản",
        "Hệ thống phanh sau", "Phanh tang trống",
        "Tiện ích nổi bật", "Smart Key, cổng sạc USB A, hộc trước có nắp, đèn LED toàn bộ, khung eSAF"
    )
},

new()
{
    Name = "Honda Wave Alpha 110cc",
    SKU = "HD-WA110",
    Price = 18900000,
    Stock = 100,
    Year = 2024,
    BrandId = GetBrandId("Honda"),
    CategoryId = GetCatId("Xe Số"),
    ImageName = "Wave_Alpha_Tieu_chuan_Trang.png",
    Specs = newDictionary(
        // --- KÍCH THƯỚC & TRỌNG LƯỢNG ---
        "Khối lượng bản thân", "96 kg",
        "Kích thước (D x R x C)", "1.913 x 689 x 1.076 mm",
        "Khoảng cách trục bánh xe", "1.224 mm",
        "Chiều cao yên", "770 mm",
        "Khoảng sáng gầm xe", "134 mm",

        // --- DUNG TÍCH & LỐP / PHUỘC ---
        "Dung tích bình xăng", "3,7 lít",
        "Dung tích nhớt máy", "≈ 0,9 – 1,0 lít",
        "Kích cỡ lốp trước", "70/90-17M/C 38P",
        "Kích cỡ lốp sau", "80/90-17M/C 50P",
        "Phuộc trước", "Ống lồng, giảm chấn thủy lực",
        "Phuộc sau", "Lò xo trụ đôi, giảm chấn thủy lực",

        // --- ĐỘNG CƠ & VẬN HÀNH ---
        "Loại động cơ", "4 kỳ, 1 xy-lanh, làm mát bằng không khí",
        "Dung tích xy-lanh", "109,2 cm³",
        "Đường kính x hành trình piston", "50,0 x 55,6 mm",
        "Tỷ số nén", "9,0 : 1",
        "Công suất tối đa", "6,12 kW / 7.500 vòng/phút",
        "Mô-men xoắn cực đại", "8,44 N.m / 5.500 vòng/phút",
        "Mức tiêu thụ nhiên liệu", "≈ 1,72 lít/100 km",
        "Hệ thống nhiên liệu", "Phun xăng điện tử (tuỳ phiên bản / thị trường)",

        // --- TRUYỀN ĐỘNG ---
        "Loại truyền động", "Cơ khí, 4 số tròn",
        "Hệ thống khởi động", "Điện & cần đạp",
        "Tiện ích nổi bật", "Thiết kế nhỏ gọn, bền bỉ, chi phí vận hành thấp"
    )
},

new()
{
    Name = "Honda Future 125 FI",
    SKU = "HD-FUT125",
    Price = 32500000,
    Stock = 60,
    Year = 2024,
    BrandId = GetBrandId("Honda"),
    CategoryId = GetCatId("Xe Số"),
    ImageName = "Future_Cao_cap_Xanh_den.png",
    Specs = newDictionary(
        // --- KÍCH THƯỚC & TRỌNG LƯỢNG ---
        "Khối lượng bản thân", "104 kg",
        "Kích thước (D x R x C)", "1.931 x 711 x 1.083 mm",
        "Khoảng cách trục bánh xe", "1.258 mm",
        "Chiều cao yên", "756 mm",
        "Khoảng sáng gầm xe", "133 mm",

        // --- DUNG TÍCH & LỐP / PHUỘC ---
        "Dung tích bình xăng", "4,6 lít",
        "Dung tích cốp xe", "≈ 12 lít",
        "Kích cỡ lốp trước", "70/90-17M/C 38P",
        "Kích cỡ lốp sau", "80/90-17M/C 50P",
        "Phuộc trước", "Ống lồng, giảm chấn thủy lực",
        "Phuộc sau", "Lò xo trụ đôi, giảm chấn thủy lực",

        // --- ĐỘNG CƠ & VẬN HÀNH ---
        "Loại động cơ", "Xăng, 4 kỳ, 1 xy-lanh, làm mát bằng không khí",
        "Dung tích xy-lanh", "124,9 cm³",
        "Tỷ số nén", "9,3 : 1",
        "Công suất tối đa", "6,83 kW / 7.500 vòng/phút",
        "Mô-men xoắn cực đại", "10,2 N.m / 5.500 vòng/phút",
        "Mức tiêu thụ nhiên liệu", "≈ 1,5 lít/100 km",
        "Hệ thống nhiên liệu", "Phun xăng điện tử PGM-FI",

        // --- TRUYỀN ĐỘNG & TIỆN ÍCH ---
        "Loại truyền động", "Cơ khí, 4 số",
        "Hệ thống khởi động", "Điện & cần đạp",
        "Tiện ích nổi bật", "Đèn pha LED, cốp rộng, ổ khóa đa năng 4 trong 1"
    )
},

new()
{
    Name = "Honda Winner X Thể thao ABS",
    SKU = "HD-WINX-SPT",
    Price = 50560000,
    Stock = 50,
    Year = 2024,
    BrandId = GetBrandId("Honda"),
    CategoryId = GetCatId("Xe Côn Tay"),
    ImageName = "WinnerX_The_thao_Do_den_trang.png",
    Specs = newDictionary(
        // --- KÍCH THƯỚC & TRỌNG LƯỢNG ---
        "Khối lượng bản thân", "122 kg",
        "Kích thước (D x R x C)", "2.019 x 727 x 1.104 mm",
        "Khoảng cách trục bánh xe", "1.278 mm",
        "Chiều cao yên", "795 mm",
        "Khoảng sáng gầm xe", "151 mm",

        // --- DUNG TÍCH & LỐP / PHUỘC ---
        "Dung tích bình xăng", "4,5 lít",
        "Dung tích nhớt máy", "1,1 lít khi thay nhớt · 1,3 lít khi rã máy",
        "Kích cỡ lốp trước", "90/80-17M/C 46P",
        "Kích cỡ lốp sau", "120/70-17M/C 58P",
        "Phuộc trước", "Ống lồng, giảm chấn thủy lực",
        "Phuộc sau", "Lò xo trụ đơn, Pro-Link (tuỳ nguồn)",

        // --- ĐỘNG CƠ & VẬN HÀNH ---
        "Loại động cơ", "PGM-FI, DOHC, 4 kỳ, xy-lanh đơn, làm mát bằng dung dịch",
        "Dung tích xy-lanh", "149,2 cm³",
        "Đường kính x hành trình piston", "57,3 x 57,8 mm (xấp xỉ)",
        "Tỷ số nén", "≈ 11,3 : 1",
        "Công suất tối đa", "≈ 11,5 kW / 9.000 vòng/phút",
        "Mô-men xoắn cực đại", "13,5 N.m / 7.000 vòng/phút",
        "Mức tiêu thụ nhiên liệu", "≈ 1,9 – 2,0 lít/100 km",

        // --- TRUYỀN ĐỘNG & AN TOÀN ---
        "Loại truyền động", "Côn tay 6 cấp số",
        "Hệ thống phanh", "Đĩa trước/sau, có ABS trên một số phiên bản",
        "Tiện ích nổi bật", "Thiết kế sport underbone, phanh ABS, ly hợp hỗ trợ & chống trượt (A&S), đèn LED"
    )
},

new()
{
    Name = "Honda CBR150R",
    SKU = "HD-CBR150R",
    Price = 72290000,
    Stock = 10,
    Year = 2023,
    BrandId = GetBrandId("Honda"),
    CategoryId = GetCatId("Sportbike"),
    ImageName = "CBR150R_The_thao_Do.png",
    Specs = newDictionary(
        // --- KÍCH THƯỚC & TRỌNG LƯỢNG ---
        "Khối lượng bản thân", "139 kg",
        "Kích thước (D x R x C)", "1.983 x 700 x 1.090 mm",
        "Khoảng cách trục bánh xe", "1.312 mm",
        "Chiều cao yên", "788 mm",
        "Khoảng sáng gầm xe", "151 mm",

        // --- DUNG TÍCH & LỐP / PHUỘC ---
        "Dung tích bình xăng", "12 lít",
        "Dung tích nhớt máy", "1,1 lít khi thay nhớt · 1,3 lít khi rã máy",
        "Kích cỡ lốp trước", "100/80-17M/C",
        "Kích cỡ lốp sau", "130/70-17M/C",
        "Phuộc trước", "Upside Down (USD), giảm chấn thủy lực",
        "Phuộc sau", "Lò xo trụ đơn, liên kết Pro-Link",

        // --- ĐỘNG CƠ & VẬN HÀNH ---
        "Loại động cơ", "PGM-FI, DOHC, 4 kỳ, 1 xy-lanh, làm mát bằng dung dịch",
        "Dung tích xy-lanh", "149,2 cm³",
        "Đường kính x hành trình piston", "≈ 57,3 x 57,8 mm",
        "Tỷ số nén", "≈ 11,3 : 1",
        "Công suất tối đa", "12,6 kW / 9.000 vòng/phút",
        "Mô-men xoắn cực đại", "14,4 N.m / 7.000 vòng/phút",
        "Mức tiêu thụ nhiên liệu", "≈ 2,9 lít/100 km",

        // --- TRUYỀN ĐỘNG & AN TOÀN ---
        "Loại truyền động", "Côn tay 6 cấp số",
        "Hệ thống phanh", "Đĩa trước/sau, ABS 2 kênh (tuỳ thị trường)",
        "Tiện ích nổi bật", "Tư thế lái sport, phuộc USD, phanh ABS, thiết kế khí động học"
    )
}
,

                // ==================== YAMAHA ====================
                // ==================== YAMAHA ====================
                new()
                {
                    Name = "Yamaha Grande Hybrid Giới hạn",
                    SKU = "YM-GRD-LTD",
                    Price = 51900000,
                    Stock = 30,
                    Year = 2024,
                    BrandId = GetBrandId("Yamaha"),
                    CategoryId = GetCatId("Xe Tay Ga"),
                    ImageName = "Grande-Hybrid-Gioi-han-Hong-Bac.png",
                    Specs = NewDictionary(
                        // --- KÍCH THƯỚC & TRỌNG LƯỢNG ---
                        "Khối lượng bản thân", "101 kg",
                        "Kích thước (D x R x C)", "1.820 x 685 x 1.150 mm",
                        "Khoảng cách trục bánh xe", "1.280 mm",
                        "Chiều cao yên", "790 mm",
                        "Khoảng sáng gầm xe", "125 mm",

                        // --- KHUNG SƯỜN & GIẢM XÓC ---
                        "Loại khung", "Underbone",
                        "Giảm xóc trước", "Ống lồng, hành trình 90 mm, giảm chấn thủy lực",
                        "Giảm xóc sau", "Lò xo trụ đôi, giảm chấn thủy lực",

                        // --- DUNG TÍCH & LỐP ---
                        "Dung tích cốp xe", "27 lít (có đèn LED)",
                        "Dung tích bình xăng", "4,0 lít",
                        "Dung tích dầu máy", "0,84 lít",
                        "Kích cỡ lốp trước", "110/70-12 (không săm)",
                        "Kích cỡ lốp sau", "110/70-12 (không săm)",

                        // --- ĐỘNG CƠ & VẬN HÀNH ---
                        "Loại động cơ", "Blue Core Hybrid, SOHC, 4 kỳ, 2 van, làm mát bằng không khí cưỡng bức",
                        "Bố trí xy-lanh", "Xy-lanh đơn",
                        "Dung tích xy-lanh", "124,9 cm³",
                        "Đường kính x hành trình piston", "52,4 x 57,9 mm",
                        "Tỷ số nén", "11,0 : 1",
                        "Công suất tối đa", "≈ 6,05 kW / 6.500 vòng/phút",
                        "Mô-men xoắn cực đại", "10,4 N.m / 5.000 vòng/phút",
                        "Hệ thống nhiên liệu", "Phun xăng điện tử",
                        "Mức tiêu thụ nhiên liệu", "≈ 1,66 lít/100 km",

                        // --- TRUYỀN ĐỘNG & TIỆN ÍCH ---
                        "Hộp số", "Tự động, vô cấp CVT",
                        "Hệ thống khởi động", "Điện",
                        "Hệ thống phanh", "Đĩa trước ABS, phanh sau tang trống",
                        "Tiện ích nổi bật", "Smart Key, Y-Connect, nắp bình xăng phía trước, cổng sạc, cốp 27L"
                    )
                },

                new()
                {
                    Name = "Yamaha NVX 155 VVA Monster",
                    SKU = "YM-NVX-MON",
                    Price = 56000000,
                    Stock = 20,
                    Year = 2024,
                    BrandId = GetBrandId("Yamaha"),
                    CategoryId = GetCatId("Xe Tay Ga"),
                    ImageName = "NVX-155-VVA-Monster-1.png",
                    Specs = NewDictionary(
                        // --- KÍCH THƯỚC & TRỌNG LƯỢNG ---
                        "Khối lượng bản thân", "≈ 127 – 130 kg (tùy phiên bản)",
                        "Kích thước (D x R x C)", "1.980 x 700 x 1.150 mm (xấp xỉ)",
                        "Khoảng cách trục bánh xe", "1.350 mm",
                        "Chiều cao yên", "790 mm",
                        "Khoảng sáng gầm xe", "140 – 145 mm",

                        // --- DUNG TÍCH & LỐP / PHUỘC ---
                        "Dung tích bình xăng", "5,5 lít",
                        "Dung tích cốp xe", "≈ 25 lít",
                        "Kích cỡ lốp trước", "110/80-14M/C",
                        "Kích cỡ lốp sau", "140/70-14M/C",
                        "Giảm xóc trước", "Phuộc ống lồng, giảm chấn thủy lực",
                        "Giảm xóc sau", "Giảm xóc đôi, lò xo trụ",

                        // --- ĐỘNG CƠ & VẬN HÀNH ---
                        "Loại động cơ", "Blue Core 155cc, SOHC, 4 van, làm mát bằng dung dịch, tích hợp VVA",
                        "Dung tích xy-lanh", "155 cm³",
                        "Tỷ số nén", "≈ 11,6 : 1",
                        "Công suất tối đa", "11,3 kW / 8.000 vòng/phút",
                        "Mô-men xoắn cực đại", "13,9 N.m / 6.500 vòng/phút",
                        "Hệ thống nhiên liệu", "Phun xăng điện tử",
                        "Mức tiêu thụ nhiên liệu", "≈ 2,7 – 2,8 lít/100 km (thực tế phụ thuộc điều kiện)",

                        // --- TRUYỀN ĐỘNG & TIỆN ÍCH ---
                        "Hộp số", "Tự động, vô cấp CVT",
                        "Hệ thống phanh", "Đĩa trước ABS, phanh sau tang trống",
                        "Công nghệ hỗ trợ", "VVA, Stop & Start System, Smart Key, Y-Connect",
                        "Tiện ích nổi bật", "Cốp rộng, cổng sạc, hộc đồ trước, yên xe êm"
                    )
                },

                new()
                {
                    Name = "Yamaha Janus Cao cấp",
                    SKU = "YM-JAN-PRE",
                    Price = 29000000,
                    Stock = 60,
                    Year = 2024,
                    BrandId = GetBrandId("Yamaha"),
                    CategoryId = GetCatId("Xe Tay Ga"),
                    ImageName = "Janus-Phien-ban-Gioi-han-Moi-Den-Hong.png",
                    Specs = NewDictionary(
                        // --- KÍCH THƯỚC & TRỌNG LƯỢNG ---
                        "Khối lượng bản thân", "97 kg",
                        "Kích thước (D x R x C)", "1.850 x 705 x 1.120 mm",
                        "Chiều cao yên", "770 mm",
                        "Khoảng sáng gầm xe", "135 mm (xấp xỉ)",

                        // --- DUNG TÍCH & LỐP / PHUỘC ---
                        "Dung tích cốp xe", "≈ 14 lít",
                        "Dung tích bình xăng", "4,2 lít",
                        "Kích cỡ lốp trước", "80/80-14M/C (không săm)",
                        "Kích cỡ lốp sau", "110/70-14M/C (không săm)",
                        "Giảm xóc trước", "Ống lồng, giảm chấn thủy lực",
                        "Giảm xóc sau", "Lò xo trụ đơn, giảm chấn thủy lực",

                        // --- ĐỘNG CƠ & VẬN HÀNH ---
                        "Loại động cơ", "Xăng 4 kỳ, SOHC, làm mát bằng không khí",
                        "Bố trí xy-lanh", "Xy-lanh đơn",
                        "Dung tích xy-lanh", "124,9 cm³",
                        "Đường kính x hành trình piston", "52,4 x 57,9 mm",
                        "Tỷ số nén", "9,5 : 1",
                        "Công suất tối đa", "7,0 kW / 8.000 vòng/phút",
                        "Mô-men xoắn cực đại", "9,6 N.m / 5.500 vòng/phút",
                        "Mức tiêu thụ nhiên liệu", "≈ 1,88 lít/100 km",
                        "Hệ thống nhiên liệu", "Phun xăng điện tử",

                        // --- TRUYỀN ĐỘNG & TIỆN ÍCH ---
                        "Hộp số", "Tự động, vô cấp CVT",
                        "Hệ thống khởi động", "Điện",
                        "Hệ thống phanh", "Đĩa thủy lực phía trước, phanh tang trống phía sau",
                        "Tiện ích nổi bật", "Stop & Start System, Smart Key (bản Cao cấp/Giới hạn), cốp rộng, sàn để chân phẳng"
                    )
                },

                new()
                {
                    Name = "Yamaha Exciter 155 VVA-ABS GP",
                    SKU = "YM-EX155-GP",
                    Price = 55000000,
                    Stock = 40,
                    Year = 2024,
                    BrandId = GetBrandId("Yamaha"),
                    CategoryId = GetCatId("Xe Côn Tay"),
                    ImageName = "Exciter-155-VVA-ABS-GP-2024.png",
                    Specs = NewDictionary(
                        // --- KÍCH THƯỚC & TRỌNG LƯỢNG ---
                        "Khối lượng bản thân", "≈ 121 kg (bản ABS)",
                        "Kích thước (D x R x C)", "1.975 x 665 x 1.105 mm",
                        "Khoảng cách trục bánh xe", "1.290 mm",
                        "Chiều cao yên", "795 mm",
                        "Khoảng sáng gầm xe", "≈ 150 mm",

                        // --- DUNG TÍCH & LỐP / KHUNG ---
                        "Dung tích bình xăng", "5,4 lít",
                        "Loại khung", "Underbone",
                        "Kích cỡ lốp trước", "90/80-17M/C 46P (không săm)",
                        "Kích cỡ lốp sau", "120/70-17M/C 58P (không săm)",
                        "Giảm xóc trước", "Ống lồng, giảm chấn thủy lực",
                        "Giảm xóc sau", "Lò xo trụ đơn, liên kết",

                        // --- ĐỘNG CƠ & VẬN HÀNH ---
                        "Loại động cơ", "155cc, 4 van, SOHC, VVA, làm mát bằng dung dịch",
                        "Dung tích xy-lanh", "155 cm³",
                        "Tỷ số nén", "≈ 10,5 : 1",
                        "Công suất tối đa", "13,2 kW / 9.500 vòng/phút",
                        "Mô-men xoắn cực đại", "14,4 N.m / 8.000 vòng/phút",
                        "Hệ thống nhiên liệu", "Phun xăng điện tử",
                        "Mức tiêu thụ nhiên liệu", "≈ 2,1 – 2,3 lít/100 km (thực tế)",

                        // --- TRUYỀN ĐỘNG & AN TOÀN ---
                        "Hộp số", "Côn tay 6 cấp",
                        "Hệ thống phanh", "Đĩa trước 2 piston + ABS, đĩa sau 1 piston",
                        "Công nghệ hỗ trợ", "Hệ thống ngắt động cơ, khóa Smart Key (trên một số phiên bản), ổ cắm sạc 12V"
                    )
                },

                new()
                {
                    Name = "Yamaha Sirius FI RC",
                    SKU = "YM-SIR-RC",
                    Price = 24300000,
                    Stock = 80,
                    Year = 2024,
                    BrandId = GetBrandId("Yamaha"),
                    CategoryId = GetCatId("Xe Số"),
                    ImageName = "Sirius-FI-RC-Den-1.png",
                    Specs = NewDictionary(
                        // --- KÍCH THƯỚC & TRỌNG LƯỢNG ---
                        "Khối lượng bản thân", "98 – 99 kg (tùy phiên bản)",
                        "Kích thước (D x R x C)", "1.940 x 715 x 1.075 mm (xấp xỉ)",
                        "Khoảng cách trục bánh xe", "1.250 mm",
                        "Chiều cao yên", "770 mm",
                        "Khoảng sáng gầm xe", "155 mm",

                        // --- DUNG TÍCH & LỐP / PHUỘC ---
                        "Dung tích bình xăng", "4,0 lít",
                        "Dung tích dầu máy", "≈ 1,0 lít",
                        "Kích cỡ lốp trước", "70/90-17M/C",
                        "Kích cỡ lốp sau", "80/90-17M/C",
                        "Giảm xóc trước", "Ống lồng, giảm chấn thủy lực",
                        "Giảm xóc sau", "Lò xo trụ đôi, giảm chấn thủy lực",

                        // --- ĐỘNG CƠ & VẬN HÀNH ---
                        "Loại động cơ", "4 thì, 2 van, SOHC, làm mát bằng không khí",
                        "Dung tích xy-lanh", "113,7 cm³",
                        "Đường kính x hành trình piston", "50,0 x 57,9 mm",
                        "Tỷ số nén", "9,3 : 1",
                        "Công suất tối đa", "6,4 kW / 7.000 vòng/phút",
                        "Mô-men xoắn cực đại", "9,5 N.m / 5.500 vòng/phút",
                        "Mức tiêu thụ nhiên liệu", "≈ 1,65 lít/100 km",
                        "Hệ thống nhiên liệu", "Phun xăng điện tử Fi",

                        // --- TRUYỀN ĐỘNG ---
                        "Hộp số", "Cơ khí, 4 số tròn",
                        "Hệ thống khởi động", "Điện & cần đạp",
                        "Tiện ích nổi bật", "Động cơ bền bỉ, tiết kiệm xăng, chi phí bảo dưỡng thấp"
                    )
                },

                new()
                {
                    Name = "Yamaha YZF-R15M",
                    SKU = "YM-R15M",
                    Price = 87000000,
                    Stock = 8,
                    Year = 2023,
                    BrandId = GetBrandId("Yamaha"),
                    CategoryId = GetCatId("Sportbike"),
                    ImageName = "R15M-Monster-Energy-Yamaha-MotoGP-Black-1.png",
                    Specs = NewDictionary(
                        // --- KÍCH THƯỚC & TRỌNG LƯỢNG ---
                        "Khối lượng bản thân", "≈ 141 kg (ướt)",
                        "Kích thước (D x R x C)", "1.990 x 725 x 1.135 mm",
                        "Chiều cao yên", "815 mm",
                        "Khoảng cách trục bánh xe", "1.325 mm",
                        "Khoảng sáng gầm xe", "170 mm",

                        // --- DUNG TÍCH & LỐP / KHUNG ---
                        "Dung tích bình xăng", "11 lít",
                        "Kích cỡ lốp trước", "100/80-17M/C (không săm)",
                        "Kích cỡ lốp sau", "140/70-17M/C (không săm)",
                        "Loại khung", "Deltabox thể thao",
                        "Giảm xóc trước", "Phuộc hành trình ngược (Upside Down)",
                        "Giảm xóc sau", "Monoshock, liên kết",

                        // --- ĐỘNG CƠ & VẬN HÀNH ---
                        "Loại động cơ", "155cc, LC4V, SOHC, VVA, làm mát bằng dung dịch",
                        "Dung tích xy-lanh", "155 cm³",
                        "Công suất tối đa", "≈ 13,5 kW (18,4 PS)",
                        "Mô-men xoắn cực đại", "≈ 14,2 N.m",
                        "Hệ thống nhiên liệu", "Phun xăng điện tử",
                        "Hộp số", "6 cấp, côn tay",
                        "Tính năng vận hành", "Assist & Slipper Clutch, Quickshifter, Traction Control",

                        // --- AN TOÀN & CÔNG NGHỆ ---
                        "Hệ thống phanh", "Đĩa trước/sau, ABS 2 kênh",
                        "Trang bị điện tử", "Bảng đồng hồ LCD, kết nối điện thoại (tùy thị trường)"
                    )
                },

                new()
                {
                    Name = "Yamaha MT-15",
                    SKU = "YM-MT15",
                    Price = 69000000,
                    Stock = 12,
                    Year = 2023,
                    BrandId = GetBrandId("Yamaha"),
                    CategoryId = GetCatId("Naked Bike"),
                    ImageName = "MT-15-Xanh-Xam-1.png",
                    Specs = NewDictionary(
                        // --- KÍCH THƯỚC & TRỌNG LƯỢNG ---
                        "Khối lượng bản thân", "≈ 133 – 138 kg (tùy thị trường)",
                        "Kích thước (D x R x C)", "1.965 x 800 x 1.065 mm",
                        "Chiều cao yên", "810 mm",
                        "Khoảng cách trục bánh xe", "1.335 mm",
                        "Khoảng sáng gầm xe", "≈ 155 – 170 mm",

                        // --- DUNG TÍCH & LỐP / KHUNG ---
                        "Dung tích bình xăng", "10 lít",
                        "Kích cỡ lốp trước", "110/70-17M/C (không săm)",
                        "Kích cỡ lốp sau", "140/70-17M/C (không săm)",
                        "Loại khung", "Deltabox",
                        "Giảm xóc trước", "Phuộc hành trình ngược (USD)",
                        "Giảm xóc sau", "Monoshock, gắn trên gắp nhôm",

                        // --- ĐỘNG CƠ & VẬN HÀNH ---
                        "Loại động cơ", "155cc, 4 thì, SOHC, VVA, làm mát bằng dung dịch",
                        "Dung tích xy-lanh", "155 cm³",
                        "Tỷ số nén", "≈ 11,6 : 1",
                        "Hộp số", "Côn tay 6 cấp",
                        "Công suất & mô-men", "Hiệu suất tương đương YZF-R15, mạnh ở dải tua cao",
                        "Tiện ích vận hành", "Ly hợp A&S (hỗ trợ & chống trượt), đèn LED, bảng đồng hồ kỹ thuật số"
                    )
                },

                new()
                {
                    Name = "Yamaha PG-1",
                    SKU = "YM-PG1",
                    Price = 30437000,
                    Stock = 35,
                    Year = 2024,
                    BrandId = GetBrandId("Yamaha"),
                    CategoryId = GetCatId("Xe Số"),
                    ImageName = "PG-1-Cam-Bac.png",
                    Specs = NewDictionary(
                        // --- KÍCH THƯỚC & TRỌNG LƯỢNG ---
                        "Kích thước (D x R x C)", "1.980 x 805 x 1.050 mm",
                        "Chiều cao yên", "795 mm",
                        "Khoảng cách trục bánh xe", "≈ 1.320 – 1.330 mm",
                        "Khoảng sáng gầm xe", "190 mm (rất cao)",

                        // --- DUNG TÍCH & LỐP / PHUỘC ---
                        "Dung tích bình xăng", "5,1 lít",
                        "Kích cỡ lốp trước", "90/100-16 (lốp gai)",
                        "Kích cỡ lốp sau", "90/100-16 (lốp gai)",
                        "Giảm xóc trước", "Phuộc ống lồng, hành trình dài",
                        "Giảm xóc sau", "Lò xo đôi, tăng tải tốt",

                        // --- ĐỘNG CƠ & VẬN HÀNH ---
                        "Loại động cơ", "4 kỳ, 1 xy-lanh, làm mát bằng không khí",
                        "Dung tích xy-lanh", "113,7 cm³",
                        "Đường kính x hành trình piston", "50,0 x 57,9 mm",
                        "Tỷ số nén", "9,3 : 1",
                        "Công suất tối đa", "6,6 kW / 7.000 vòng/phút",
                        "Mô-men xoắn cực đại", "9,5 N.m / 5.500 vòng/phút",
                        "Hệ thống nhiên liệu", "Phun xăng điện tử Fi",

                        // --- TRUYỀN ĐỘNG & TÍNH NĂNG ---
                        "Hộp số", "4 số (côn tự động)",
                        "Hệ thống khởi động", "Điện (có cần đạp dự phòng trên một số thị trường)",
                        "Tiện ích nổi bật", "Thiết kế Adventure Underbone, tư thế lái thoải mái, phù hợp đường xấu"
                    )
                },


                // ==================== SUZUKI ====================
                // ==================== SUZUKI ====================
                new()
                {
                    Name = "Suzuki Raider R150",
                    SKU = "SZ-RAI150",
                    Price = 51190000,
                    Stock = 15,
                    Year = 2023,
                    BrandId = GetBrandId("Suzuki"),
                    CategoryId = GetCatId("Xe Côn Tay"),
                    ImageName = "Raider-R150-Xanh-Den.png",
                    Specs = NewDictionary(
                        // --- KÍCH THƯỚC & TRỌNG LƯỢNG ---
                        "Kiểu dáng", "Hyper Underbone thể thao",
                        "Kích thước (D x R x C)", "1.960 x 675 x 980 mm",
                        "Chiều cao yên", "765 mm",
                        "Chiều dài cơ sở", "1.280 mm",
                        "Khoảng sáng gầm xe", "150 mm",
                        "Khối lượng bản thân", "109 kg",

                        // --- LỐP, PHANH, GIẢM XÓC ---
                        "Kích cỡ lốp trước", "70/90-17 M/C (không săm)",
                        "Kích cỡ lốp sau", "80/90-17 M/C (không săm)",
                        "Loại mâm", "Mâm đúc thể thao",
                        "Phanh trước", "Đĩa thủy lực",
                        "Phanh sau", "Đĩa thủy lực",
                        "Giảm xóc trước", "Ống lồng, lò xo, giảm chấn dầu",
                        "Giảm xóc sau", "Monoshock, lò xo trụ",

                        // --- ĐỘNG CƠ & HIỆU NĂNG ---
                        "Loại động cơ", "4 thì, 1 xy-lanh, DOHC, 4 van, làm mát bằng dung dịch, phun xăng điện tử",
                        "Dung tích xy-lanh", "147,3 cm³",
                        "Đường kính x hành trình piston", "62,0 x 48,8 mm",
                        "Tỷ số nén", "11,5 : 1",
                        "Công suất tối đa", "13,6 kW (≈ 18,2 HP) / 10.000 vòng/phút",
                        "Mô-men xoắn cực đại", "13,8 N.m / 8.500 vòng/phút",
                        "Hệ thống bôi trơn", "Cácte ướt (wet sump)",
                        "Hệ thống đánh lửa", "DC-CDI (điện tử)",
                        "Hệ thống nhiên liệu", "Phun xăng điện tử (FI)",
                        "Dung tích bình xăng", "4,0 lít",

                        // --- TRUYỀN ĐỘNG & TIỆN ÍCH ---
                        "Hộp số", "Côn tay 6 cấp, sang số thể thao",
                        "Truyền động cuối", "Nhông xích",
                        "Khởi động", "Đề điện & cần đạp",
                        "Phong cách vận hành", "Bốc, tăng tốc nhanh, phù hợp người thích cảm giác thể thao"
                    )
                },
                new()
                {
                    Name = "Suzuki Satria F150",
                    SKU = "SZ-SAT150",
                    Price = 53490000,
                    Stock = 15,
                    Year = 2023,
                    BrandId = GetBrandId("Suzuki"),
                    CategoryId = GetCatId("Xe Côn Tay"),
                    ImageName = "Satria-F150-Xanh-Bac-Den.png",
                    Specs = NewDictionary(
                        // --- ĐẶC ĐIỂM CHUNG ---
                        "Nguồn gốc", "Nhập khẩu nguyên chiếc từ Indonesia",
                        "Kiểu dáng", "Hyper Underbone côn tay 150cc",

                        // --- KÍCH THƯỚC & TRỌNG LƯỢNG ---
                        "Kích thước (D x R x C)", "1.960 x 675 x 980 mm (xấp xỉ Raider FI)",
                        "Chiều cao yên", "765 mm",
                        "Chiều dài cơ sở", "1.280 mm",
                        "Khoảng sáng gầm xe", "150 mm",
                        "Khối lượng bản thân", "≈ 112 kg (tùy phiên bản)",

                        // --- LỐP, PHANH, HỆ THỐNG TREO ---
                        "Kích cỡ lốp trước", "70/90-17 M/C (không săm)",
                        "Kích cỡ lốp sau", "80/90-17 M/C (không săm)",
                        "Phanh trước", "Đĩa thủy lực",
                        "Phanh sau", "Đĩa thủy lực",
                        "Giảm xóc trước", "Phuộc ống lồng, giảm chấn dầu",
                        "Giảm xóc sau", "Monoshock, lò xo trụ",

                        // --- ĐỘNG CƠ & HIỆU NĂNG ---
                        "Loại động cơ", "Xăng, 4 thì, 1 xy-lanh, DOHC, 4 van, làm mát bằng dung dịch",
                        "Dung tích xy-lanh", "147,3 cm³",
                        "Đường kính x hành trình piston", "62,0 x 48,8 mm",
                        "Tỷ số nén", "11,5 : 1",
                        "Công suất tối đa", "≈ 18,2 HP / 10.000 vòng/phút",
                        "Mô-men xoắn cực đại", "13,8 N.m / 8.500 vòng/phút",
                        "Hệ thống nhiên liệu", "Phun xăng điện tử (FI)",
                        "Hệ thống bôi trơn", "Cácte ướt (wet sump)",
                        "Hệ thống làm mát", "Làm mát bằng dung dịch",

                        // --- TRUYỀN ĐỘNG & TÍNH NĂNG ---
                        "Hộp số", "Côn tay 6 cấp",
                        "Khởi động", "Đề điện & cần đạp",
                        "Tốc độ tối đa (theo hãng/giới thiệu)", "≈ 142 km/h (trong điều kiện lý tưởng)",
                        "Đặc điểm nổi bật", "Máy vọt, đề-pa tốt, được mệnh danh là “Vua tốc độ” phân khúc underbone 150cc"
                    )
                },
                new()
                {
                    Name = "Suzuki Burgman Street",
                    SKU = "SZ-BURG125",
                    Price = 48600000,
                    Stock = 10,
                    Year = 2023,
                    BrandId = GetBrandId("Suzuki"),
                    CategoryId = GetCatId("Xe Tay Ga"),
                    ImageName = "Burgman-Street-Trang-Vang-Dong.png",
                    Specs = NewDictionary(
                        // --- KIỂU DÁNG & ĐỐI TƯỢNG ---
                        "Phong cách", "Maxi Scooter cỡ nhỏ, dáng to, ngồi rất êm",
                        "Đối tượng phù hợp", "Người cần xe tay ga êm ái, cốp rộng, đi phố và đi xa thoải mái",

                        // --- KÍCH THƯỚC & TRỌNG LƯỢNG ---
                        "Kích thước (D x R x C)", "1.880 x 715 x 1.140 mm",
                        "Chiều cao yên", "780 mm",
                        "Chiều dài cơ sở", "1.265 mm",
                        "Khoảng sáng gầm xe", "≈ 110 – 160 mm (tùy thị trường, cấu hình)",
                        "Khối lượng bản thân", "≈ 109 – 112 kg",

                        // --- LỐP, MÂM, PHANH, TREO ---
                        "Kích cỡ lốp trước", "90/90-12 (không săm)",
                        "Kích cỡ lốp sau", "90/100-10 (không săm)",
                        "Loại mâm", "Mâm đúc hợp kim",
                        "Phanh trước", "Đĩa thủy lực",
                        "Phanh sau", "Tang trống, có CBS hỗ trợ phân bố lực phanh",
                        "Giảm xóc trước", "Phuộc ống lồng, giảm chấn dầu",
                        "Giảm xóc sau", "Giảm xóc sau dạng gắp, thủy lực",

                        // --- ĐỘNG CƠ & TIÊU HAO NHIÊN LIỆU ---
                        "Loại động cơ", "4 thì, 1 xy-lanh, SOHC, 2 van, làm mát bằng không khí, SEP (Suzuki Eco Performance)",
                        "Dung tích xy-lanh", "124 cm³",
                        "Đường kính x hành trình piston", "52,5 x 57,4 mm",
                        "Tỷ số nén", "10,3 : 1",
                        "Công suất tối đa", "≈ 8,6 – 8,7 PS / 6.750 vòng/phút",
                        "Mô-men xoắn cực đại", "≈ 10 N.m / 5.500 vòng/phút",
                        "Hệ thống nhiên liệu", "Phun xăng điện tử (FI)",
                        "Dung tích bình xăng", "5,5 lít",
                        "Mức tiêu thụ nhiên liệu tham khảo", "≈ 48 km/l (tùy điều kiện sử dụng)",

                        // --- TRUYỀN ĐỘNG & TIỆN ÍCH ---
                        "Hộp số", "Tự động, truyền động CVT",
                        "Loại truyền động", "Dây đai (belt drive)",
                        "Hộc chứa đồ", "Cốp lớn dưới yên, hộc đồ trước có cổng sạc DC",
                        "Tiện ích nổi bật", "Tư thế lái duỗi chân thoải mái, yên to dày, xe êm, phù hợp đi làm hàng ngày và đi chơi xa"
                    )
                },


                // ==================== VESPA / PIAGGIO ====================
               // ==================== VESPA / PIAGGIO ====================
                new()
                {
                    Name = "Vespa Sprint S 150 TFT",
                    SKU = "PG-VESPA-SPR",
                    Price = 110000000,
                    Stock = 8,
                    Year = 2024,
                    BrandId = GetBrandId("Vespa"),
                    CategoryId = GetCatId("Xe Tay Ga"),
                    ImageName = "vespa-sprint-s-150-tft-bronze-antico.png",
                    Specs = NewDictionary(
                        // --- KÍCH THƯỚC & TRỌNG LƯỢNG ---
                        "Kích thước (D x R)", "1.863 x 695 mm",
                        "Chiều dài cơ sở", "1.334 mm",
                        "Chiều cao yên", "790 mm",
                        "Khối lượng bản thân", "≈ 130 kg",

                        // --- KHUNG SƯỜN, LỐP & PHANH ---
                        "Cấu trúc khung", "Thân xe bằng thép liền khối, đặc trưng Vespa",
                        "Kích cỡ lốp trước", "110/70-12, lốp không săm",
                        "Kích cỡ lốp sau", "120/70-12, lốp không săm",
                        "Vành xe", "Mâm đúc 12 inch, đa chấu thể thao",
                        "Giảm xóc trước", "Giảm chấn thủy lực đơn hiệu ứng kép, lò xo ống lồng",
                        "Giảm xóc sau", "Giảm chấn thủy lực hiệu ứng kép, lò xo ống lồng, 4 mức điều chỉnh",
                        "Phanh trước", "Đĩa 200 mm, tích hợp ABS",
                        "Phanh sau", "Phanh tang trống",

                        // --- ĐỘNG CƠ & HIỆU NĂNG ---
                        "Loại động cơ", "iGet, xi-lanh đơn, 4 kỳ, 3 van, phun xăng điện tử",
                        "Dung tích xy-lanh", "154,8 cm³",
                        "Công suất cực đại", "9,5 kW (≈ 13 HP) / 7.750 vòng/phút",
                        "Mô-men xoắn cực đại", "12,8 N.m / 6.500 vòng/phút",
                        "Hệ thống làm mát", "Làm mát bằng gió cưỡng bức",
                        "Hệ thống nhiên liệu", "Phun xăng điện tử i-Get, đạt chuẩn khí thải mới",
                        "Dung tích bình xăng", "7,0 ± 0,5 lít",
                        "Mức tiêu thụ nhiên liệu", "≈ 2,7 lít/100 km (điều kiện tiêu chuẩn)",

                        // --- TIỆN ÍCH & TRANG BỊ ---
                        "Màn hình", "TFT màu 4,3 inch, hiển thị đa thông tin",
                        "Kết nối điện thoại", "Hệ thống Vespa MIA (Bluetooth, thông báo, trip…)",
                        "Hệ thống chiếu sáng", "Đèn pha, đèn hậu, đèn định vị Full LED",
                        "Cốp xe", "Dung tích lớn, để vừa mũ bảo hiểm và vật dụng cá nhân",
                        "Tiện ích khác", "Cổng sạc USB, móc treo đồ, yên 2 tầng phong cách thể thao"
                    )
                },
                new()
                {
                    Name = "Vespa Primavera Color Vibe",
                    SKU = "PG-VESPA-PRI",
                    Price = 86600000,
                    Stock = 12,
                    Year = 2023,
                    BrandId = GetBrandId("Vespa"),
                    CategoryId = GetCatId("Xe Tay Ga"),
                    ImageName = "primavera-color-vibe-125-arancio-impulsivo.png",
                    Specs = NewDictionary(
                        // --- PHIÊN BẢN & PHONG CÁCH ---
                        "Phiên bản", "Primavera Color Vibe đặc biệt, phối 2 tông màu trẻ trung",
                        "Đối tượng phù hợp", "Người dùng thành thị, thích phong cách thời trang, nhẹ nhàng",

                        // --- KÍCH THƯỚC & TRỌNG LƯỢNG ---
                        "Kích thước (D x R)", "1.852 x 680 mm",
                        "Chiều cao yên", "790 mm",
                        "Chiều dài cơ sở", "≈ 1.340 mm",
                        "Khối lượng bản thân", "≈ 129 kg (tùy cấu hình)",

                        // --- KHUNG SƯỜN, LỐP & PHANH ---
                        "Cấu trúc khung", "Thân xe bằng thép liền khối, tăng độ cứng và ổn định",
                        "Kích cỡ lốp trước", "110/70-11, lốp không săm",
                        "Kích cỡ lốp sau", "120/70-11 (hoặc 120/70-10 tùy thị trường), lốp không săm",
                        "Vành xe", "Mâm đúc, thiết kế 5 chấu thanh mảnh",
                        "Giảm xóc trước", "Giảm chấn thủy lực đơn hiệu ứng kép, lò xo ống lồng",
                        "Giảm xóc sau", "Giảm chấn thủy lực hiệu ứng kép, lò xo ống lồng 4 vị trí điều chỉnh",
                        "Phanh trước", "Đĩa 200 mm, tích hợp ABS một kênh",
                        "Phanh sau", "Tang trống",

                        // --- ĐỘNG CƠ & HIỆU NĂNG ---
                        "Loại động cơ", "Piaggio iGet, xi-lanh đơn, 4 kỳ, 3 van, phun xăng điện tử",
                        "Dung tích xy-lanh", "124,5 cm³",
                        "Đường kính x hành trình piston", "52 mm x 58,6 mm",
                        "Công suất cực đại", "7,9 kW (≈ 10,7 HP) / 7.700 vòng/phút",
                        "Mô-men xoắn cực đại", "10,4 N.m / 6.000 vòng/phút",
                        "Hệ thống làm mát", "Làm mát bằng gió cưỡng bức",
                        "Hệ thống nhiên liệu", "Phun xăng điện tử, đạt chuẩn khí thải Euro mới",
                        "Dung tích bình xăng", "7,0 ± 0,5 lít",
                        "Mức tiêu thụ nhiên liệu", "≈ 2,6–2,7 lít/100 km (tham khảo thực tế)",

                        // --- TIỆN ÍCH & THIẾT KẾ ---
                        "Hệ thống chiếu sáng", "Đèn LED hiện đại (tùy phiên bản), đèn định vị trước sau",
                        "Cốp xe", "Cốp lớn dưới yên, để được mũ bảo hiểm cùng đồ cá nhân",
                        "Hộc đồ trước", "Có ngăn chứa nhỏ kèm cổng sạc (tùy phiên bản thị trường)",
                        "Hệ thống khóa", "Khoá từ Immobilizer chống trộm",
                        "Tiện ích khác", "Móc treo đồ, sàn để chân phẳng, yên êm, phù hợp đi phố hằng ngày"
                    )
                },
                new()
                {
                    Name = "Piaggio Liberty S 125 ABS",
                    SKU = "PG-LIB-S",
                    Price = 57700000,
                    Stock = 20,
                    Year = 2023,
                    BrandId = GetBrandId("Vespa"), // cùng Brand 'Vespa' trong CSDL hiện tại
                    CategoryId = GetCatId("Xe Tay Ga"),
                    ImageName = "liberty-s-125-nero-meteora.png",
                    Specs = NewDictionary(
                        // --- KIỂU DÁNG & ĐỐI TƯỢNG ---
                        "Dòng xe", "Liberty S 125 ABS – bánh lớn, dáng cao ráo",
                        "Đối tượng phù hợp", "Người cần xe tay ga dễ điều khiển, quan sát tốt, đi phố ổn định",

                        // --- KÍCH THƯỚC & TRỌNG LƯỢNG ---
                        "Chiều dài tổng thể", "1.945 mm",
                        "Chiều rộng", "690 mm",
                        "Chiều cao yên", "790 mm",
                        "Chiều dài cơ sở", "≈ 1.340 mm",
                        "Khối lượng ướt (đầy đủ dung dịch)", "≈ 128 kg",

                        // --- KHUNG GẦM, LỐP & PHANH ---
                        "Cấu trúc khung", "Khung ống thép kết hợp tấm dập, thiết kế bánh lớn",
                        "Kích cỡ lốp trước", "90/80-16, lốp không săm",
                        "Kích cỡ lốp sau", "100/80-14, lốp không săm",
                        "Bánh xe", "Trước 16 inch / Sau 14 inch, mâm 3 chấu thể thao",
                        "Phanh trước", "Đĩa 240 mm tích hợp ABS",
                        "Phanh sau", "Phanh tang trống 140 mm",
                        "Hệ thống ABS", "ABS 1 kênh trên bánh trước, can thiệp khi phanh gấp",
                        "Giảm xóc trước", "Phuộc ống lồng, giảm chấn thủy lực",
                        "Giảm xóc sau", "Giảm chấn thủy lực, lò xo trụ, điều chỉnh tải",

                        // --- ĐỘNG CƠ & TIÊU HAO NHIÊN LIỆU ---
                        "Loại động cơ", "Piaggio iGet, xi-lanh đơn, 4 kỳ, 3 van, phun xăng điện tử",
                        "Dung tích xy-lanh", "124,5 cm³",
                        "Đường kính x hành trình piston", "52 mm x 58,6 mm",
                        "Công suất cực đại", "7,6 kW / 7.600 vòng/phút (≈ 10,2 HP)",
                        "Mô-men xoắn cực đại", "10,2 N.m / 6.000 vòng/phút",
                        "Hệ thống làm mát", "Làm mát bằng gió",
                        "Hệ thống nhiên liệu", "Phun xăng điện tử (FI), tiết kiệm nhiên liệu",
                        "Dung tích bình xăng", "≈ 6 lít",
                        "Mức tiêu thụ nhiên liệu tham khảo", "≈ 45–50 km/l (tùy điều kiện vận hành)",

                        // --- TIỆN ÍCH & CÔNG NĂNG ---
                        "Màn hình", "Đồng hồ analog kết hợp LCD hiển thị đa thông tin",
                        "Cốp xe dưới yên", "Để được 1 mũ bảo hiểm nửa đầu + vật dụng cá nhân",
                        "Hộc đồ trước", "Chia 2 ngăn, có nắp và điều khiển qua ổ khóa",
                        "Ổ khóa", "Ổ khóa đa năng, mở yên và hộc đồ, tích hợp chống trộm cơ bản",
                        "Tiện ích khác", "Móc treo đồ, sàn để chân phẳng, tư thế lái thẳng lưng, tầm nhìn cao"
                    )
                },


                // ==================== VINFAST (XE ĐIỆN) ====================
               // ==================== VINFAST (XE ĐIỆN) ====================
                new()
                {
                    Name = "VinFast Klara S (2022)",
                    SKU = "VF-KLARA-S",
                    Price = 35000000,
                    Stock = 30,
                    Year = 2024,
                    BrandId = GetBrandId("VinFast"),
                    CategoryId = GetCatId("Xe Điện"),
                    ImageName = "klara-s-red.png",
                    Specs = NewDictionary(
                        // --- KIỂU DÁNG & KÍCH THƯỚC ---
                        "Loại xe", "Xe máy điện thông minh, thân cao, phù hợp đi phố",
                        "Dài x Rộng x Cao", "1.895 x 678 x 1.130 mm",
                        "Chiều dài trục bánh xe", "1.313 mm",
                        "Chiều cao yên", "760 mm",
                        "Khoảng sáng gầm xe", "125 mm",
                        "Khối lượng (kèm pin)", "112 kg",

                        // --- KHUNG GẦM, LỐP, PHANH, GIẢM XÓC ---
                        "Kích thước lốp trước", "90/90-14, lốp không săm",
                        "Kích thước lốp sau", "120/70-12, lốp không săm",
                        "Thể tích cốp", "23 lít (đựng được mũ + đồ cá nhân)",
                        "Phanh trước", "Phanh đĩa thủy lực",
                        "Phanh sau", "Phanh đĩa thủy lực",
                        "Giảm xóc trước", "Ống lồng, giảm chấn thủy lực",
                        "Giảm xóc sau", "Giảm xóc đôi, giảm chấn thủy lực",

                        // --- ĐỘNG CƠ & VẬN HÀNH ---
                        "Loại động cơ", "Động cơ Inhub đặt tại bánh sau",
                        "Công suất danh định", "1.800 W",
                        "Công suất tối đa", "3.000 W",
                        "Tốc độ tối đa", "78 km/h",
                        "Thời gian tăng tốc 0–50 km/h (1 người 65 kg)", "≈ 12 giây",
                        "Tiêu chuẩn chống nước", "IP67 (động cơ & pin, chịu ngập nước tốt)",

                        // --- PIN & SẠC ---
                        "Loại pin", "01 pin LFP (Lithium iron phosphate)",
                        "Dung lượng pin", "3,5 kWh",
                        "Quãng đường 1 lần sạc (30 km/h, 1 người 65 kg)", "≈ 194 km",
                        "Loại sạc tiêu chuẩn", "Sạc 1.000 W",
                        "Thời gian sạc 0–100%", "Khoảng 6–8 giờ (điều kiện tiêu chuẩn)"
                    )
                },
                new()
                {
                    Name = "VinFast Vento S",
                    SKU = "VF-VENTO-S",
                    Price = 50000000,
                    Stock = 20,
                    Year = 2024,
                    BrandId = GetBrandId("VinFast"),
                    CategoryId = GetCatId("Xe Điện"),
                    ImageName = "vento-s-yellow.png",
                    Specs = NewDictionary(
                        // --- KIỂU DÁNG & KÍCH THƯỚC ---
                        "Loại xe", "Xe máy điện cao cấp, dáng maxi scooter thể thao",
                        "Dài x Rộng x Cao", "1.863 x 692 x 1.100 mm",
                        "Chiều cao yên", "780 mm",
                        "Khoảng sáng gầm xe", "135 mm",
                        "Khối lượng (kèm pin)", "122 kg",
                        "Thể tích cốp", "25 lít",

                        // --- KHUNG GẦM, LỐP, PHANH, GIẢM XÓC ---
                        "Lốp xe", "Lốp không săm, kích thước lớn, bám đường tốt",
                        "Giảm xóc trước", "Lò xo, ống lồng, giảm chấn thủy lực",
                        "Giảm xóc sau", "Giảm xóc đôi, giảm chấn thủy lực",
                        "Phanh trước", "Phanh đĩa, ABS Continental",
                        "Phanh sau", "Phanh đĩa",

                        // --- ĐỘNG CƠ & VẬN HÀNH ---
                        "Loại động cơ", "Side Motor (động cơ đặt bên), công nghệ IPM",
                        "Công suất danh định", "3.000 W",
                        "Công suất tối đa", "5.200 W",
                        "Tốc độ tối đa", "89 km/h",
                        "Thời gian tăng tốc 0–50 km/h (1 người 65 kg)", "≈ 6,2 giây",
                        "Tiêu chuẩn chống nước", "IP67 (ngập nước 0,5 m trong 30 phút)",

                        // --- PIN & SẠC ---
                        "Loại pin", "01 pin LFP",
                        "Dung lượng pin", "3,5 kWh",
                        "Quãng đường 1 lần sạc (30 km/h, 1 người 65 kg)", "≈ 160 km",
                        "Loại sạc tiêu chuẩn", "Sạc 1.000 W",
                        "Thời gian sạc 0–100%", "Khoảng 6 giờ",
                        "Tuổi thọ pin", "Còn ~70% dung lượng sau 2.000 chu kỳ sạc/xả"
                    )
                },
                new()
                {
                    Name = "VinFast Evo 200 Lite",
                    SKU = "VF-EVO200",
                    Price = 18000000,
                    Stock = 60,
                    Year = 2024,
                    BrandId = GetBrandId("VinFast"),
                    CategoryId = GetCatId("Xe Điện"),
                    ImageName = "evo200-yellow.png",
                    Specs = NewDictionary(
                        // --- KIỂU DÁNG & ĐỐI TƯỢNG ---
                        "Đối tượng ưu tiên", "Học sinh, sinh viên – không cần bằng lái",
                        "Phong cách thiết kế", "Gọn, trẻ trung, dễ điều khiển trong phố",

                        // --- KÍCH THƯỚC & KHỐI LƯỢNG ---
                        "Dài x Rộng x Cao", "1.804 x 683 x 1.127 mm",
                        "Chiều cao yên", "≈ 750 mm",
                        "Khối lượng (kèm pin)", "97 kg",

                        // --- KHUNG GẦM, LỐP, PHANH, GIẢM XÓC ---
                        "Kích thước lốp trước", "90/90-12, lốp không săm",
                        "Kích thước lốp sau", "90/90-12, lốp không săm",
                        "Giảm xóc trước", "Ống lồng, giảm chấn thủy lực",
                        "Giảm xóc sau", "Giảm xóc đôi, giảm chấn thủy lực",
                        "Phanh trước/sau", "Phanh đĩa/cơ (tùy phiên bản, thị trường)",
                        "Tiêu chuẩn chống nước", "IP67 (động cơ & pin)",

                        // --- ĐỘNG CƠ & VẬN HÀNH ---
                        "Loại động cơ", "Inhub đặt tại bánh sau",
                        "Công suất danh định", "1.500 W",
                        "Công suất tối đa", "2.450 W",
                        "Tốc độ tối đa", "49 km/h (chuẩn xe máy điện dành cho học sinh)",

                        // --- PIN & SẠC ---
                        "Loại pin", "01 pin LFP",
                        "Dung lượng pin", "3,5 kWh",
                        "Quãng đường 1 lần sạc (30 km/h, 1 người 65 kg)", "≈ 200+ km (khoảng 203 km theo công bố)",
                        "Loại sạc tiêu chuẩn", "Sạc 400 W đi kèm xe",
                        "Thời gian sạc 0–100%", "Khoảng 10 giờ",
                        "Tuổi thọ pin", "≈ 2.000 lần sạc/xả còn ~70% dung lượng"
                    )
                },


                // ==================== PKL: DUCATI – BMW ====================
                // ==================== PKL (28-31) ====================
new()
{
    Name = "Ducati Panigale V4 S",
    SKU = "DC-PANV4S",
    Price = 945000000,
    Stock = 2,
    Year = 2024,
    BrandId = GetBrandId("Ducati"),
    CategoryId = GetCatId("Sportbike"),
    ImageName = "panigale-v4-s-red.png",
    Specs = NewDictionary(
        // --- KIỂU DÁNG & KÍCH THƯỚC ---
        "Phân khúc", "Superbike PKL cao cấp đường phố & đường đua",
        "Dài x Rộng x Cao", "2.105 x 810 x 1.135 mm (xấp xỉ)",
        "Chiều dài cơ sở", "1.485 mm",
        "Chiều cao yên", "850 mm",
        "Khoảng sáng gầm xe", "110 mm (xấp xỉ)",
        "Trọng lượng khô", "187 kg (không tính xăng, dung dịch)",

        // --- ĐỘNG CƠ & HIỆU NĂNG ---
        "Loại động cơ", "Desmosedici Stradale V4 90°, 4 thì, DOHC, 4 van/xy-lanh, làm mát bằng dung dịch",
        "Dung tích xy-lanh", "1.103 cm³",
        "Công suất cực đại", "≈ 216 hp (158,9 kW) @ 13.500 vòng/phút",
        "Mô-men xoắn cực đại", "≈ 121 Nm @ 11.250 vòng/phút",
        "Đường kính x hành trình piston", "81,0 x 53,5 mm",
        "Tỷ số nén", "≈ 14,0 : 1",
        "Hệ thống phun xăng", "Phun xăng điện tử, 2 kim phun/xy-lanh, Ride-by-Wire, cổ hút biến thiên",
        "Tiêu chuẩn khí thải", "Euro 5 (tùy thị trường)",

        // --- HỘP SỐ & TRUYỀN ĐỘNG ---
        "Hộp số", "6 cấp, Quickshifter 2 chiều (DQS)",
        "Ly hợp", "Ly hợp chống trượt (Slipper Clutch), hỗ trợ ga lớn xuống số gấp",
        "Truyền động cuối", "Xích tải, nhông đĩa hiệu suất cao",

        // --- KHUNG SƯỜN & HỆ THỐNG TREO ---
        "Khung sườn", "Nhôm Front Frame, gắn trực tiếp vào động cơ",
        "Gắp sau", "Gắp đơn bằng nhôm",
        "Giảm xóc trước", "Phuộc USD Ohlins NIX30, điều khiển điện tử Smart EC 2.0, hành trình ~120 mm",
        "Giảm xóc sau", "Phuộc Ohlins TTX36, điều khiển điện tử Smart EC 2.0, hành trình ~130 mm",

        // --- PHANH & LỐP ---
        "Phanh trước", "2 đĩa 330 mm, kẹp phanh Brembo Stylema monobloc, ABS cornering",
        "Phanh sau", "Đĩa đơn 245 mm, kẹp phanh Brembo 2 piston",
        "Lốp trước", "120/70 ZR17 (Pirelli Diablo Supercorsa / Rosso, tùy cấu hình)",
        "Lốp sau", "200/60 ZR17",

        // --- BÌNH XĂNG & TIỆN ÍCH ---
        "Dung tích bình xăng", "≈ 17 lít",
        "Bảng đồng hồ", "Màn hình màu TFT đa chức năng",
        "Điện tử hỗ trợ", "IMU 6 trục, Riding Mode, Power Mode, Cornering ABS, Traction Control, Wheelie Control, Slide Control, Engine Brake Control, Launch Control"
    )
},
new()
{
    Name = "Ducati Monster 937",
    SKU = "DC-MNS937",
    Price = 439000000,
    Stock = 4,
    Year = 2023,
    BrandId = GetBrandId("Ducati"),
    CategoryId = GetCatId("Naked Bike"),
    ImageName = "monster-937-red.png",
    Specs = NewDictionary(
        // --- KIỂU DÁNG & KÍCH THƯỚC ---
        "Phân khúc", "Naked Bike đường phố, tư thế lái thoải mái",
        "Chiều dài cơ sở", "1.474 mm",
        "Chiều cao yên", "820 mm (có tùy chọn hạ thấp)",
        "Khối lượng ướt (đầy đủ dung dịch)", "≈ 188 kg",
        "Tổng thể", "Gọn gàng, yên thấp, dễ chống chân hơn so với nhiều PKL khác",

        // --- ĐỘNG CƠ & HIỆU NĂNG ---
        "Loại động cơ", "Testastretta 11°, L-Twin, 4 thì, DOHC, 4 van/xy-lanh, làm mát bằng dung dịch",
        "Dung tích xy-lanh", "937 cm³",
        "Công suất cực đại", "≈ 111 hp (82 kW) @ 9.250 vòng/phút",
        "Mô-men xoắn cực đại", "≈ 93 Nm @ 6.500 vòng/phút",
        "Tỷ số nén", "13,3 : 1",
        "Hệ thống nhiên liệu", "Phun xăng điện tử, thân bướm ga Ride-by-Wire",
        "Tiêu chuẩn khí thải", "Euro 5 (tùy thị trường)",

        // --- HỘP SỐ & TRUYỀN ĐỘNG ---
        "Hộp số", "6 cấp, sang số nhanh 2 chiều (Quickshifter) trên bản cao",
        "Ly hợp", "Ly hợp ướt, trợ lực & chống trượt (Assist & Slipper)",
        "Truyền động cuối", "Nhông xích",

        // --- KHUNG SƯỜN & HỆ THỐNG TREO ---
        "Khung sườn", "Front Frame nhôm, sử dụng động cơ làm kết cấu chịu lực",
        "Gắp sau", "Gắp đơn (single sided swingarm)",
        "Giảm xóc trước", "Phuộc USD 43 mm, điều chỉnh cơ bản",
        "Giảm xóc sau", "Monoshock, điều chỉnh tải sơ cấp",

        // --- PHANH & LỐP ---
        "Phanh trước", "2 đĩa 320 mm, kẹp Brembo M4.32 monobloc, ABS cornering",
        "Phanh sau", "Đĩa đơn 245 mm, kẹp Brembo 2 piston",
        "Lốp trước", "120/70 ZR17",
        "Lốp sau", "180/55 ZR17",

        // --- BÌNH XĂNG & TIỆN ÍCH ---
        "Dung tích bình xăng", "14 lít",
        "Bảng đồng hồ", "Màn hình màu TFT",
        "Điện tử hỗ trợ", "Riding Mode, Power Mode, Traction Control, Wheelie Control, ABS cornering, Quickshifter (tùy phiên bản)"
    )
},
new()
{
    Name = "BMW S 1000 RR",
    SKU = "BMW-S1000RR",
    Price = 949000000,
    Stock = 3,
    Year = 2023,
    BrandId = GetBrandId("BMW Motorrad"),
    CategoryId = GetCatId("Sportbike"),
    ImageName = "s-1000-rr-m-package.png",
    Specs = NewDictionary(
        // --- KIỂU DÁNG & KÍCH THƯỚC ---
        "Phân khúc", "Superbike 1.000cc, DNA đường đua WSBK",
        "Chiều dài tổng thể", "≈ 2.073 mm",
        "Chiều rộng (gồm gương)", "≈ 848 mm",
        "Chiều cao tổng thể", "≈ 1.151 mm",
        "Chiều cao yên", "824 mm",
        "Chiều dài cơ sở", "≈ 1.441 mm",
        "Trọng lượng ướt (DIN)", "≈ 197 kg (không đồ M-Performance)",

        // --- ĐỘNG CƠ & HIỆU NĂNG ---
        "Loại động cơ", "4 xy-lanh thẳng hàng, 4 thì, DOHC, 4 van/xy-lanh, làm mát nước/dầu, BMW ShiftCam",
        "Dung tích xy-lanh", "999 cm³",
        "Đường kính x hành trình piston", "80,0 x 49,7 mm",
        "Tỷ số nén", "13,3 : 1",
        "Công suất cực đại", "≈ 205 hp @ 13.000 vòng/phút",
        "Mô-men xoắn cực đại", "≈ 113 Nm @ 11.000 vòng/phút",
        "Tốc độ tối đa", "Trên 300 km/h (≈ 302–303 km/h)",
        "Hệ thống phun xăng", "Phun xăng điện tử, ống nạp biến thiên",

        // --- HỘP SỐ & TRUYỀN ĐỘNG ---
        "Hộp số", "6 cấp, hỗ trợ sang số nhanh (Shift Assistant Pro)",
        "Ly hợp", "Ly hợp ướt, chống trượt khi trả số",
        "Truyền động cuối", "Xích tải",

        // --- KHUNG SƯỜN & HỆ THỐNG TREO ---
        "Khung sườn", "Khung nhôm Composite Bridge Frame",
        "Giảm xóc trước", "Phuộc USD 45 mm, điều chỉnh được (hoặc Dynamic Damping Control tùy gói)",
        "Giảm xóc sau", "Monoshock, điều chỉnh tải & độ hồi",
        "Góc lái & trail", "Thiết kế thiên về ổn định ở tốc độ cao",

        // --- PHANH & LỐP ---
        "Phanh trước", "2 đĩa 320 mm, kẹp 4 piston, ABS Pro (cornering ABS)",
        "Phanh sau", "Đĩa đơn 220 mm, kẹp 1 piston",
        "Lốp trước", "120/70 ZR17",
        "Lốp sau", "190/55 ZR17 (hoặc 200/55 ZR17 tùy cấu hình)",

        // --- NHIÊN LIỆU & ĐIỆN TỬ ---
        "Dung tích bình xăng", "16,5 lít",
        "Bảng đồng hồ", "Màn hình màu TFT 6,5 inch",
        "Điện tử hỗ trợ", "Riding Mode (Rain/Road/Dynamic/Race + Race Pro), Traction Control, Slide Control, Wheelie Control, ABS Pro, Launch Control, Pit Lane Limiter"
    )
},
new()
{
    Name = "BMW R 1250 GS Adventure",
    SKU = "BMW-R1250GSA",
    Price = 739000000,
    Stock = 2,
    Year = 2023,
    BrandId = GetBrandId("BMW Motorrad"),
    CategoryId = GetCatId("Sportbike"), // theo CSDL hiện tại của bạn
    ImageName = "r-1250-gs-adventure-triple-black.png",
    Specs = NewDictionary(
        // --- KIỂU DÁNG & KÍCH THƯỚC ---
        "Phân khúc", "Adventure Touring – vua địa hình & đường trường",
        "Chiều dài tổng thể", "≈ 2.270 mm",
        "Chiều rộng (gồm thùng, tay lái)", "≈ 980–1.000 mm",
        "Chiều cao tổng thể (kể cả kính gió)", "≈ 1.460–1.500 mm",
        "Chiều cao yên", "890 / 910 mm (2 mức điều chỉnh)",
        "Chiều dài cơ sở", "1.504 mm",
        "Trọng lượng ướt (DIN)", "≈ 268 kg",
        "Tải trọng cho phép", "≈ 217 kg",

        // --- ĐỘNG CƠ & HIỆU NĂNG ---
        "Loại động cơ", "Boxer 2 xy-lanh đối xứng, 4 thì, DOHC, ShiftCam, làm mát bằng dung dịch & không khí",
        "Dung tích xy-lanh", "1.254 cm³",
        "Công suất cực đại", "≈ 136 hp (100 kW) @ 7.750 vòng/phút",
        "Mô-men xoắn cực đại", "143 Nm @ 6.250 vòng/phút",
        "Tiêu chuẩn khí thải", "Euro 5",
        "Mức tiêu hao nhiên liệu tham khảo", "≈ 4,8–5,5 lít/100 km (tùy điều kiện tải & đường)",

        // --- HỘP SỐ & TRUYỀN ĐỘNG ---
        "Hộp số", "6 cấp, sang số mượt, hỗ trợ quickshifter (tùy gói)",
        "Truyền động cuối", "Trục các-đăng (Shaft Drive) – ít bảo dưỡng, bền bỉ đường dài",

        // --- KHUNG GẦM & HỆ THỐNG TREO ---
        "Khung sườn", "Khung ống thép, subframe bắt bulong",
        "Hệ thống treo trước", "BMW Telelever, hành trình dài, chống chúi khi phanh",
        "Hệ thống treo sau", "BMW Paralever, giảm xoắn trục các-đăng",
        "Hệ thống treo điện tử", "Dynamic ESA (tùy gói) tự điều chỉnh theo tải & chế độ chạy",

        // --- PHANH & LỐP ---
        "Phanh trước", "2 đĩa 305 mm, kẹp 4 piston hướng tâm, ABS Pro",
        "Phanh sau", "Đĩa đơn 276 mm, kẹp 2 piston",
        "Kích cỡ lốp trước", "120/70 R19",
        "Kích cỡ lốp sau", "170/60 R17",

        // --- BÌNH XĂNG & TIỆN ÍCH TOURING ---
        "Dung tích bình xăng", "30 lít (đi rất xa, phù hợp touring dài ngày)",
        "Bảng đồng hồ", "Màn hình màu TFT hiện đại, hỗ trợ kết nối điện thoại",
        "Trang bị & tiện ích", "Nhiều chế độ lái, kiểm soát lực kéo, ABS Pro, hỗ trợ lên/xuống số, kiểm soát hành trình, chế độ Off-road, sưởi tay lái (tùy gói), chuẩn bị sẵn cho thùng hông & thùng sau"
    )
},
// ==================== PHỤ TÙNG & LINH KIỆN (PHU TUNG) ====================
new()
{
    Name = "Nhớt máy Honda 10W-30 Chính hãng 0.8L",
    SKU = "PT-HD-OIL-10W30",
    Price = 135000,
    Stock = 200,
    Year = 2025,
    BrandId = GetBrandId("Honda"),
    CategoryId = GetCatId("Phụ tùng & Linh kiện"),
    ImageName = "nhot-honda-10w30-genuine.png",
    Specs = NewDictionary(
        "Loại sản phẩm", "Dầu nhớt động cơ xe máy 4 thì",
        "Thương hiệu", "Honda Genuine Oil",
        "Độ nhớt (SAE)", "10W-30",
        "Tiêu chuẩn API", "API SN",
        "Tiêu chuẩn JASO", "JASO MA",
        "Dung tích", "0,8 lít/chai",
        "Loại động cơ phù hợp", "Xe số và xe tay ga 4 thì của Honda",
        "Ứng dụng", "Thay định kỳ cho Vision, Wave, Future, Winner X...",
        "Chu kỳ thay khuyến nghị", "2.500 – 3.000 km hoặc 3 – 4 tháng",
        "Ưu điểm", "Bảo vệ động cơ, giảm mài mòn, giúp máy êm hơn",
        "Xuất xứ", "Sản xuất theo tiêu chuẩn Honda Việt Nam",
        "Lưu ý sử dụng", "Thay đúng dung tích khuyến nghị, không trộn lẫn nhiều loại nhớt"
    )
},
new()
{
    Name = "Lọc gió Honda Vision / Lead / SH Mode Chính hãng",
    SKU = "PT-HD-AIR-VIS-LEAD",
    Price = 165000,
    Stock = 150,
    Year = 2025,
    BrandId = GetBrandId("Honda"),
    CategoryId = GetCatId("Phụ tùng & Linh kiện"),
    ImageName = "loc-gio-honda-vision-lead-shmode.png",
    Specs = NewDictionary(
        "Loại sản phẩm", "Lọc gió động cơ (lọc gió gió hút)",
        "Thương hiệu", "Honda chính hãng",
        "Dòng xe phù hợp", "Vision, Lead, SH Mode (đời mới dùng phun xăng điện tử)",
        "Vật liệu", "Lõi giấy lọc chuyên dụng, khung nhựa",
        "Công dụng", "Lọc bụi bẩn trước khi không khí vào buồng đốt",
        "Chu kỳ thay khuyến nghị", "10.000 – 15.000 km (hoặc sớm hơn khi đi môi trường bụi)",
        "Ưu điểm", "Giúp máy đốt cháy nhiên liệu sạch hơn, tiết kiệm xăng, tăng độ bền động cơ",
        "Cách bảo dưỡng", "Có thể vệ sinh nhẹ bằng khí nén, không giặt nước",
        "Lưu ý", "Nên dùng đúng lọc gió đúng mã cho từng dòng xe"
    )
},
new()
{
    Name = "Dây curoa truyền động Honda Vario / Air Blade 125/160",
    SKU = "PT-HD-BELT-VAR-AB",
    Price = 420000,
    Stock = 80,
    Year = 2025,
    BrandId = GetBrandId("Honda"),
    CategoryId = GetCatId("Phụ tùng & Linh kiện"),
    ImageName = "day-curoa-honda-vario-airblade.png",
    Specs = NewDictionary(
        "Loại sản phẩm", "Dây curoa truyền động cho xe tay ga",
        "Thương hiệu", "Honda / OEM chuẩn hãng",
        "Dòng xe phù hợp", "Vario 125/160, Air Blade 125/160 (tùy mã cụ thể)",
        "Vật liệu", "Cao su tổng hợp, gia cố sợi bố chịu lực",
        "Chức năng", "Truyền công suất từ động cơ tới bánh sau qua bộ nồi",
        "Chu kỳ thay khuyến nghị", "20.000 – 30.000 km (tùy điều kiện sử dụng)",
        "Ưu điểm", "Độ bám tốt, hạn chế trượt dây, tăng tuổi thọ bộ nồi",
        "Dấu hiệu cần thay", "Xe rung giật khi đề-pa, tiếng hú lớn, tăng ga nhưng không bốc",
        "Lưu ý", "Nên kiểm tra đồng thời bi nồi, bố nồi, mặt nạ bố khi thay dây curoa"
    )
},
new()
{
    Name = "Bố thắng đĩa trước Honda Winner X / Exciter 150/155",
    SKU = "PT-HD-BR-FR-WINX",
    Price = 195000,
    Stock = 120,
    Year = 2025,
    BrandId = GetBrandId("Honda"),
    CategoryId = GetCatId("Phụ tùng & Linh kiện"),
    ImageName = "bo-thang-dia-truoc-winnerx-exciter.png",
    Specs = NewDictionary(
        "Loại sản phẩm", "Má phanh (bố thắng) đĩa trước",
        "Thương hiệu", "Honda / nhà cung cấp OEM",
        "Dòng xe tương thích", "Winner X, một số dòng Exciter 150/155 dùng chung cùm phanh (tham khảo mã bố)",
        "Vật liệu bề mặt ma sát", "Hợp chất gốm – kim loại (semi-metallic)",
        "Ưu điểm", "Độ bám tốt, ít hao đĩa, ổn định khi phanh tốc độ cao",
        "Vị trí lắp đặt", "Heo phanh đĩa trước, bên tay phải khi ngồi lái",
        "Chu kỳ thay khuyến nghị", "Khi má mòn còn ~1–1,5 mm hoặc có tiếng kêu khi phanh",
        "Lưu ý an toàn", "Nên thay theo cặp, chạy rà phanh nhẹ 100–200 km đầu sau khi thay"
    )
},
new()
{
    Name = "Nhớt Yamaha Genuine Oil 10W-40 0.8L",
    SKU = "PT-YM-OIL-10W40",
    Price = 145000,
    Stock = 180,
    Year = 2025,
    BrandId = GetBrandId("Yamaha"),
    CategoryId = GetCatId("Phụ tùng & Linh kiện"),
    ImageName = "nhot-yamaha-genuine-10w40.png",
    Specs = NewDictionary(
        "Loại sản phẩm", "Dầu nhớt động cơ 4 thì cho xe Yamaha",
        "Thương hiệu", "Yamaha Genuine Oil",
        "Độ nhớt (SAE)", "10W-40",
        "Tiêu chuẩn API", "API SJ/SL (tùy lô sản xuất)",
        "Tiêu chuẩn JASO", "JASO MA2",
        "Dung tích", "0,8 lít/chai",
        "Dòng xe phù hợp", "Exciter, Sirius, NVX, Grande, Janus...",
        "Chu kỳ thay khuyến nghị", "2.000 – 2.500 km đối với xe đi điều kiện bình thường",
        "Ưu điểm", "Bảo vệ động cơ khi vận hành ở tua cao, giúp động cơ sạch hơn",
        "Lưu ý", "Nên dùng loại nhớt đúng khuyến cáo trong sổ bảo hành của xe"
    )
},
new()
{
    Name = "Lọc gió Yamaha NVX / Aerox / Grande Chính hãng",
    SKU = "PT-YM-AIR-NVX-GRD",
    Price = 175000,
    Stock = 100,
    Year = 2025,
    BrandId = GetBrandId("Yamaha"),
    CategoryId = GetCatId("Phụ tùng & Linh kiện"),
    ImageName = "loc-gio-yamaha-nvx-grande.png",
    Specs = NewDictionary(
        "Loại sản phẩm", "Lọc gió động cơ",
        "Thương hiệu", "Yamaha chính hãng",
        "Dòng xe phù hợp", "NVX, Aerox, Grande, một số dòng FreeGo (tùy mã cụ thể)",
        "Cấu tạo", "Giấy lọc dạng gấp khúc, khung nhựa định hình",
        "Chức năng", "Ngăn bụi, tạp chất trước khi không khí vào buồng đốt",
        "Chu kỳ thay khuyến nghị", "10.000 – 12.000 km",
        "Ưu điểm", "Giúp xe giữ được độ bốc và tiết kiệm xăng ổn định",
        "Lưu ý", "Không dùng lọc gió nhái, kém chất lượng vì sẽ ảnh hưởng công suất và độ bền máy"
    )
},
new()
{
    Name = "Nhớt máy Suzuki Ecstar 10W-40 0.8L",
    SKU = "PT-SZ-OIL-ECSTAR",
    Price = 150000,
    Stock = 90,
    Year = 2025,
    BrandId = GetBrandId("Suzuki"),
    CategoryId = GetCatId("Phụ tùng & Linh kiện"),
    ImageName = "nhot-suzuki-ecstar-10w40.png",
    Specs = NewDictionary(
        "Loại sản phẩm", "Dầu nhớt Ecstar cho xe Suzuki",
        "Thương hiệu", "Suzuki Ecstar",
        "Độ nhớt (SAE)", "10W-40",
        "Tiêu chuẩn", "Phù hợp động cơ DOHC, tua cao như Raider, Satria",
        "Dung tích", "0,8 lít",
        "Dòng xe phù hợp", "Raider R150, Satria F150, GSX, các xe số & côn tay Suzuki",
        "Ưu điểm", "Độ ổn định nhiệt tốt, phù hợp chạy tua cao lâu dài",
        "Chu kỳ thay khuyến nghị", "2.000 – 2.500 km",
        "Lưu ý", "Nên thay lọc nhớt (nếu có) theo định kỳ kèm thay nhớt"
    )
},
new()
{
    Name = "Bộ nhông sên dĩa Suzuki Raider/Satria 428H",
    SKU = "PT-SZ-SPRK-RAI-SAT",
    Price = 520000,
    Stock = 60,
    Year = 2025,
    BrandId = GetBrandId("Suzuki"),
    CategoryId = GetCatId("Phụ tùng & Linh kiện"),
    ImageName = "nhong-sen-dia-raider-satria-428h.png",
    Specs = NewDictionary(
        "Loại sản phẩm", "Bộ nhông – sên – dĩa (NSD)",
        "Thương hiệu", "Suzuki / OEM chất lượng cao",
        "Bước sên", "428H (tăng độ bền so với 428 thường)",
        "Dòng xe phù hợp", "Raider R150, Satria F150",
        "Cụm chi tiết", "1 nhông trước, 1 dĩa sau, 1 sên 428H đúng chiều dài tiêu chuẩn",
        "Chu kỳ thay khuyến nghị", "20.000 – 25.000 km (tùy cách sử dụng và bảo dưỡng)",
        "Ưu điểm", "Truyền lực ổn định, tiếng ồn thấp, độ bền cao",
        "Gợi ý bảo dưỡng", "Thường xuyên vệ sinh và tra mỡ/nhớt sên định kỳ 500 – 700 km/lần",
        "Lưu ý", "Không nên chỉnh sên quá căng hoặc quá chùng"
    )
},
new()
{
    Name = "Nhớt máy Piaggio 5W-40 1L Cho Vespa/Liberty",
    SKU = "PT-PG-OIL-5W40",
    Price = 260000,
    Stock = 70,
    Year = 2025,
    BrandId = GetBrandId("Vespa"),
    CategoryId = GetCatId("Phụ tùng & Linh kiện"),
    ImageName = "nhot-piaggio-5w40-1l.png",
    Specs = NewDictionary(
        "Loại sản phẩm", "Dầu nhớt động cơ 4 thì, tổng hợp/ bán tổng hợp",
        "Thương hiệu", "Piaggio / Vespa",
        "Độ nhớt (SAE)", "5W-40",
        "Dung tích", "1 lít/chai",
        "Dòng xe phù hợp", "Vespa Sprint, Primavera, Liberty 125/150 iGet",
        "Ưu điểm", "Độ nhớt loãng ở nhiệt độ thấp, bảo vệ động cơ tốt khi khởi động lạnh",
        "Chu kỳ thay khuyến nghị", "6.000 – 7.000 km (theo khuyến cáo Piaggio)",
        "Lưu ý", "Nên kiểm tra mức nhớt định kỳ, châm thêm nếu thiếu"
    )
},
new()
{
    Name = "Lọc gió Vespa Sprint/Primavera/Liberty iGet",
    SKU = "PT-PG-AIR-VESPA",
    Price = 210000,
    Stock = 50,
    Year = 2025,
    BrandId = GetBrandId("Vespa"),
    CategoryId = GetCatId("Phụ tùng & Linh kiện"),
    ImageName = "loc-gio-vespa-sprint-primavera-liberty.png",
    Specs = NewDictionary(
        "Loại sản phẩm", "Lọc gió động cơ cho động cơ iGet",
        "Thương hiệu", "Piaggio chính hãng",
        "Dòng xe phù hợp", "Vespa Sprint, Primavera, Liberty 125/150 iGet",
        "Cấu tạo", "Lõi mút/giấy lọc, khung nhựa theo tiêu chuẩn Piaggio",
        "Chu kỳ thay khuyến nghị", "10.000 – 12.000 km (tùy điều kiện bụi bẩn)",
        "Ưu điểm", "Giúp máy êm, giữ độ bốc và tiết kiệm nhiên liệu",
        "Lưu ý", "Thay đúng loại, không tự chế lọc khác kích thước"
    )
},
new()
{
    Name = "Má phanh trước VinFast Klara/Vento/Evo",
    SKU = "PT-VF-BR-FR-KLV",
    Price = 230000,
    Stock = 90,
    Year = 2025,
    BrandId = GetBrandId("VinFast"),
    CategoryId = GetCatId("Phụ tùng & Linh kiện"),
    ImageName = "ma-phanh-truoc-vinfast-klara-vento-evo.png",
    Specs = NewDictionary(
        "Loại sản phẩm", "Má phanh (bố thắng) đĩa trước",
        "Thương hiệu", "VinFast / OEM",
        "Dòng xe phù hợp", "Klara, Vento, một số phiên bản Evo (tùy mã heo phanh)",
        "Vật liệu", "Hợp chất ma sát chịu nhiệt cao",
        "Chức năng", "Tạo ma sát với đĩa phanh để giảm tốc xe",
        "Chu kỳ thay khuyến nghị", "Tùy theo thói quen sử dụng phanh, thường 15.000 – 20.000 km",
        "Lưu ý an toàn", "Không dùng má phanh trôi nổi, kém chất lượng vì dễ bị cháy, nứt má"
    )
},
new()
{
    Name = "Lốp sau VinFast Evo 200 90/90-12",
    SKU = "PT-VF-TIRE-EVO200",
    Price = 380000,
    Stock = 40,
    Year = 2025,
    BrandId = GetBrandId("VinFast"),
    CategoryId = GetCatId("Phụ tùng & Linh kiện"),
    ImageName = "lop-sau-vinfast-evo200-90-90-12.png",
    Specs = NewDictionary(
        "Loại sản phẩm", "Lốp sau không săm",
        "Kích thước", "90/90-12",
        "Dòng xe phù hợp", "VinFast Evo 200 / Evo 200 Lite",
        "Hoa văn", "Hoa văn tối ưu cho chạy phố, bám đường tốt khi trời mưa",
        "Áp suất lốp khuyến nghị", "Tham khảo tem thông số trên thân xe (thường 2,5 – 2,8 bar)",
        "Chu kỳ thay khuyến nghị", "Khi gai mòn gần tới vạch chỉ thị hoặc lốp bị lão hóa, nứt",
        "Lưu ý", "Kiểm tra áp suất định kỳ để tránh mòn lệch và tăng độ bền lốp"
    )
},
// ==================== PHỤ TÙNG & LINH KIỆN (BỔ SUNG) ====================
new()
{
    Name = "Bugi NGK CR7HSA Cho Xe Số/Tay Ga 110–125cc",
    SKU = "PT-NGK-CR7HSA",
    Price = 85000,
    Stock = 120,
    Year = 2025,
    BrandId = GetBrandId("Honda"), // dùng chung cho dòng Honda/Yamaha 110–125cc
    CategoryId = GetCatId("Phụ tùng & Linh kiện"),
    ImageName = "bugi-ngk-cr7hsa-110-125cc.png",
    Specs = NewDictionary(
        "Loại sản phẩm", "Bugi đánh lửa cho động cơ xăng 4 thì",
        "Thương hiệu bugi", "NGK (Nhật Bản)",
        "Mã bugi", "CR7HSA",
        "Động cơ phù hợp", "Xe số & tay ga 110–125cc (Vision, Wave, Sirius, Janus...)",
        "Kiểu ren", "M10 x 1,0",
        "Độ dài ren", "12,7 mm",
        "Chân bugi", "Cổ ngắn",
        "Ưu điểm", "Đánh lửa ổn định, dễ nổ máy, giảm hụt ga",
        "Chu kỳ thay khuyến nghị", "8.000 – 12.000 km (tùy điều kiện sử dụng)",
        "Lưu ý lắp đặt", "Vặn đúng lực siết, không siết quá tay tránh tuôn ren đầu quy lát"
    )
},
new()
{
    Name = "Ắc quy khô 12V-5Ah Cho Vision/Janus/Grande",
    SKU = "PT-BATT-12V5AH",
    Price = 420000,
    Stock = 70,
    Year = 2025,
    BrandId = GetBrandId("Yamaha"),
    CategoryId = GetCatId("Phụ tùng & Linh kiện"),
    ImageName = "ac-quy-kho-12v-5ah-vision-janus.png",
    Specs = NewDictionary(
        "Loại sản phẩm", "Ắc quy khô (MF) không cần châm nước định kỳ",
        "Điện áp định mức", "12V",
        "Dung lượng", "5Ah",
        "Dòng xe phù hợp", "Vision, Lead, Janus, Grande, một số tay ga 110–125cc khác",
        "Cấu tạo", "Ắc quy kín khí, bản cực chì–canxi",
        "Ưu điểm", "Đề máy khỏe, ít tự phóng điện khi để lâu",
        "Bảo dưỡng", "Không cần châm nước, chỉ cần giữ sạch cực ắc quy",
        "Lưu ý", "Không để cạn điện quá sâu thường xuyên, tránh rút tuổi thọ ắc quy"
    )
},
new()
{
    Name = "Lốp trước Michelin City Grip 2 80/90-14",
    SKU = "PT-TIRE-MICH-809014",
    Price = 650000,
    Stock = 50,
    Year = 2025,
    BrandId = GetBrandId("Honda"),
    CategoryId = GetCatId("Phụ tùng & Linh kiện"),
    ImageName = "lop-michelin-citygrip2-80-90-14.png",
    Specs = NewDictionary(
        "Loại sản phẩm", "Lốp không săm cho xe tay ga",
        "Kích thước", "80/90-14",
        "Hãng lốp", "Michelin City Grip 2",
        "Vị trí lắp", "Lốp trước",
        "Dòng xe phù hợp", "Vision, Lead, Janus, Grande, FreeGo, một số tay ga 14 inch",
        "Ưu điểm", "Bám đường tốt khi trời mưa, má lốp êm, chống trượt",
        "Hoa văn", "Thiết kế rãnh thoát nước đa chiều",
        "Áp suất lốp khuyến nghị", "2,0 – 2,25 bar tùy tải trọng",
        "Chu kỳ thay khuyến nghị", "Khi gai mòn đến vạch báo mòn hoặc lốp chai nứt"
    )
},
new()
{
    Name = "Lốp sau Michelin City Grip 2 90/90-14",
    SKU = "PT-TIRE-MICH-909014",
    Price = 720000,
    Stock = 50,
    Year = 2025,
    BrandId = GetBrandId("Honda"),
    CategoryId = GetCatId("Phụ tùng & Linh kiện"),
    ImageName = "lop-michelin-citygrip2-90-90-14.png",
    Specs = NewDictionary(
        "Loại sản phẩm", "Lốp không săm cho xe tay ga",
        "Kích thước", "90/90-14",
        "Hãng lốp", "Michelin City Grip 2",
        "Vị trí lắp", "Lốp sau",
        "Dòng xe phù hợp", "Vision, Janus, Grande, các tay ga dùng cỡ 90/90-14",
        "Ưu điểm", "Độ bám tốt, êm ái, giảm rung khi chạy tốc độ cao",
        "Hoa văn", "Rãnh sâu, thoát nước tốt, hạn chế trượt trên đường ướt",
        "Áp suất lốp khuyến nghị", "2,25 – 2,5 bar tùy tải trọng",
        "Lưu ý", "Nên thay đồng bộ cặp trước–sau nếu lốp đã quá cũ"
    )
},
new()
{
    Name = "Kính chắn gió cao Honda Winner X / Exciter 155",
    SKU = "PT-SCRN-WINX-EXC",
    Price = 480000,
    Stock = 60,
    Year = 2025,
    BrandId = GetBrandId("Honda"),
    CategoryId = GetCatId("Phụ tùng & Linh kiện"),
    ImageName = "kinh-chan-gio-winnerx-exciter155.png",
    Specs = NewDictionary(
        "Loại sản phẩm", "Kính chắn gió (windscreen) dạng cao",
        "Vật liệu", "Nhựa PC/ABS trong suốt hoặc khói",
        "Dòng xe lắp được", "Honda Winner X, Yamaha Exciter 150/155 (có pat theo xe)",
        "Công dụng", "Giảm gió tạt ngực khi chạy tốc độ cao, tăng tính thẩm mỹ",
        "Kiểu dáng", "Thể thao, bo theo dàn áo đầu xe",
        "Lắp đặt", "Bắt vào vị trí ốc zin trên chóa đèn / mặt nạ",
        "Lưu ý", "Nên siết ốc vừa đủ, tránh nứt kính do siết quá chặt"
    )
},
new()
{
    Name = "Gương chiếu hậu kiểu thể thao ốc 10mm",
    SKU = "PT-MIR-SPT-10MM",
    Price = 220000,
    Stock = 100,
    Year = 2025,
    BrandId = GetBrandId("Yamaha"),
    CategoryId = GetCatId("Phụ tùng & Linh kiện"),
    ImageName = "guong-chieu-hau-the-thao-10mm.png",
    Specs = NewDictionary(
        "Loại sản phẩm", "Cặp gương chiếu hậu kiểu thể thao",
        "Cỡ chân gương", "Ốc 10 mm (phù hợp đa số xe Nhật)",
        "Dòng xe phổ biến", "Wave, Sirius, Winner, Exciter, NVX, Vario...",
        "Màu sắc", "Đen mờ (matte black)",
        "Ưu điểm", "Thiết kế nhỏ gọn, góc cạnh, tăng tính thẩm mỹ",
        "Điều chỉnh", "Có thể chỉnh góc xoay gương nhiều hướng",
        "Lưu ý", "Cần căn chỉnh góc nhìn an toàn, không để gương quá nhỏ khó quan sát"
    )
},
new()
{
    Name = "Bao tay + gù CNC Thể thao cho xe côn tay / tay ga",
    SKU = "PT-GRIP-CNC-SPT",
    Price = 260000,
    Stock = 130,
    Year = 2025,
    BrandId = GetBrandId("Yamaha"),
    CategoryId = GetCatId("Phụ tùng & Linh kiện"),
    ImageName = "bao-tay-va-gu-cnc-the-thao.png",
    Specs = NewDictionary(
        "Loại sản phẩm", "Bao tay lái kèm gù CNC",
        "Vật liệu bao tay", "Cao su mềm, bề mặt vân chống trượt",
        "Vật liệu gù", "Nhôm CNC anodized",
        "Dòng xe phù hợp", "Hầu hết xe tay ga & xe côn tay dùng cùm ga tiêu chuẩn",
        "Công dụng", "Tăng độ êm ái khi cầm lái, giảm rung, trang trí xe",
        "Màu sắc", "Nhiều màu (đen, đỏ, xanh…) tùy lô hàng",
        "Lưu ý", "Khi lắp cho xe có côn dây/ga dây cần căn chỉnh tránh cấn bao tay"
    )
},
new()
{
    Name = "Bộ sên DID 520VX3 cho PKL 600–1000cc",
    SKU = "PT-DID-520VX3",
    Price = 2450000,
    Stock = 25,
    Year = 2025,
    BrandId = GetBrandId("BMW Motorrad"), // dùng cho PKL nói chung
    CategoryId = GetCatId("Phụ tùng & Linh kiện"),
    ImageName = "sen-did-520vx3-pkl-600-1000.png",
    Specs = NewDictionary(
        "Loại sản phẩm", "Sên (xích tải) DID 520VX3",
        "Bước sên", "520, tiêu chuẩn cho nhiều xe PKL 600–1000cc",
        "Chiều dài", "118–120 mắt (có thể cắt bớt theo xe)",
        "Công nghệ", "X-Ring giảm ma sát, tăng tuổi thọ",
        "Dòng xe phù hợp", "Naked/Sport 600–1000cc, như MT-07, GSX-S750, S1000RR (tùy tỷ số truyền)",
        "Ưu điểm", "Độ bền cao, vận hành êm, ít dãn hơn sên thường",
        "Bảo dưỡng", "Vệ sinh & xịt dưỡng sên định kỳ 500–700 km",
        "Lưu ý", "Cần thiết lập đúng độ chùng, tránh căng/chùng quá mức"
    )
},
new()
{
    Name = "Má phanh trước Brembo Sinter Cho PKL",
    SKU = "PT-BREMBO-FR-PAD",
    Price = 1850000,
    Stock = 15,
    Year = 2025,
    BrandId = GetBrandId("Ducati"),
    CategoryId = GetCatId("Phụ tùng & Linh kiện"),
    ImageName = "ma-phanh-truoc-brembo-sinter-pkl.png",
    Specs = NewDictionary(
        "Loại sản phẩm", "Má phanh (bố thắng) trước Brembo sinter",
        "Vật liệu", "Hợp kim sinter, chịu nhiệt cao",
        "Dòng xe phù hợp", "Nhiều mẫu PKL dùng heo Brembo 4 piston (tùy mã heo)",
        "Ưu điểm", "Hiệu suất phanh rất mạnh, ổn định khi phanh gấp ở tốc độ cao",
        "Ứng dụng", "Đi tour nhanh, chạy track, đường đèo dốc",
        "Lưu ý lắp đặt", "Cần chạy rà (bed-in) nhẹ 100–200 km đầu để đạt hiệu suất tối ưu",
        "Khuyến cáo an toàn", "Chỉ dùng má phanh chính hãng/uy tín cho xe PKL để đảm bảo an toàn"
    )
},
new()
{
    Name = "Đĩa phanh trước 320mm Cho xe độ PKL",
    SKU = "PT-DISC-FR-320",
    Price = 2100000,
    Stock = 20,
    Year = 2025,
    BrandId = GetBrandId("Ducati"),
    CategoryId = GetCatId("Phụ tùng & Linh kiện"),
    ImageName = "dia-phanh-truoc-320mm-pkl.png",
    Specs = NewDictionary(
        "Loại sản phẩm", "Đĩa phanh trước đường kính 320 mm",
        "Vật liệu", "Thép chịu nhiệt, mài phẳng bề mặt ma sát",
        "Đường kính ngoài", "320 mm",
        "Kiểu đĩa", "Đĩa tròn/đĩa bông (tùy lô hàng)",
        "Dòng xe phù hợp", "Nhiều xe PKL khi độ phanh, cần dùng pat nâng heo tương ứng",
        "Ưu điểm", "Tăng mô-men phanh, giúp phanh nhạy hơn khi chạy tốc độ cao",
        "Lưu ý kỹ thuật", "Cần thợ có kinh nghiệm lắp đặt & căn chỉnh chuẩn"
    )
},
new()
{
    Name = "Bộ sạc pin VinFast 1.000W Cho Klara/Vento",
    SKU = "PT-VF-CHARGER-1KW",
    Price = 3900000,
    Stock = 10,
    Year = 2025,
    BrandId = GetBrandId("VinFast"),
    CategoryId = GetCatId("Phụ tùng & Linh kiện"),
    ImageName = "sac-pin-vinfast-1000w-klara-vento.png",
    Specs = NewDictionary(
        "Loại sản phẩm", "Bộ sạc pin xe máy điện công suất 1.000W",
        "Công suất", "1.000 W",
        "Điện áp ngõ vào", "220V AC",
        "Dòng xe phù hợp", "VinFast Klara S, Vento S (tùy chuẩn pin LFP)",
        "Cổng kết nối", "Chuẩn jack sạc theo xe VinFast",
        "Ưu điểm", "Thời gian sạc nhanh hơn so với các bộ sạc công suất thấp",
        "Lưu ý sử dụng", "Dùng ổ cắm riêng, hạn chế dùng chung với thiết bị công suất lớn khác",
        "An toàn", "Tránh để sạc nơi ẩm ướt, không che kín bộ sạc khi đang hoạt động"
    )
}


            };


            // 3. Tạo Product + Specifications + Images
            var products = new List<Product>();

            foreach (var item in seedItems)
            {
                var brandName = brands.First(b => b.Id == item.BrandId).Name;
                var brandFolder = GetBrandFolder(brandName);
                var baseFolder = $"/images/products/{brandFolder}";
                var fileName = item.ImageName;
                var fileNameNoExt = Path.GetFileNameWithoutExtension(fileName);
                var ext = Path.GetExtension(fileName);

                var mainImageUrl = $"{baseFolder}/{fileNameNoExt}{ext}";

                var product = new Product
                {
                    Name = item.Name,
                    SKU = item.SKU,
                    Price = item.Price,
                    StockQuantity = item.Stock,
                    Year = item.Year,
                    BrandId = item.BrandId,
                    CategoryId = item.CategoryId,
                    IsActive = true,
                    IsPublished = true,
                    ImageUrl = mainImageUrl,
                    CreatedAt = DateTime.UtcNow,
                    Specifications = new List<ProductSpecification>(),
                    Images = new List<ProductImage>()
                };

                // a. Thêm thông số kỹ thuật
                int sortOrder = 1;
                foreach (var spec in item.Specs)
                {
                    product.Specifications.Add(new ProductSpecification
                    {
                        Name = spec.Key,
                        Value = spec.Value,
                        SortOrder = sortOrder++
                    });
                }

                // b. Mô tả HTML chi tiết (KHÔNG có footer cam kết)
                product.Description = GenerateHtmlDescription(product, item.Specs);

                // c. Ảnh phụ (góc 1–3)
                for (int i = 1; i <= 3; i++)
                {
                    product.Images.Add(new ProductImage
                    {
                        ImageUrl = $"{baseFolder}/{fileNameNoExt}-goc{i}{ext}",
                        Caption = $"{item.Name} - Góc {i}",
                        SortOrder = i,
                        IsPrimary = (i == 1)
                    });
                }

                products.Add(product);
            }

            context.Products.AddRange(products);
            await context.SaveChangesAsync();
        }

        // ======== Helper types & methods =========

        private class ProductSeedItem
        {
            public string Name { get; set; } = null!;
            public string SKU { get; set; } = null!;
            public decimal Price { get; set; }
            public int Stock { get; set; }
            public int Year { get; set; }
            public int BrandId { get; set; }
            public int CategoryId { get; set; }
            public string ImageName { get; set; } = null!;
            public Dictionary<string, string> Specs { get; set; } = new();
        }

        private static Dictionary<string, string> NewDictionary(params string[] args)
        {
            var dict = new Dictionary<string, string>();
            for (int i = 0; i < args.Length; i += 2)
            {
                if (i + 1 < args.Length)
                {
                    dict[args[i]] = args[i + 1];
                }
            }
            return dict;
        }
        // Wrapper để dùng được cả newDictionary(...) và NewDictionary(...)
        private static Dictionary<string, string> newDictionary(params string[] args)
        {
            return NewDictionary(args);
        }

        // Map tên Brand -> thư mục ảnh (để không lệch với cấu trúc ảnh hiện có)
        private static string GetBrandFolder(string brandName) =>
            brandName switch
            {
                "Honda" => "honda",
                "Yamaha" => "yamaha",
                "Suzuki" => "suzuki",
                "Vespa" => "vespa",
                "Piaggio" => "vespa",        // dùng chung folder vespa
                "VinFast" => "vinfast",
                "Ducati" => "ducati",
                "BMW Motorrad" => "bmw",
                _ => "others"
            };

        // Tạo nội dung HTML mô tả chi tiết (KHÔNG có footer cam kết)
        // Tạo nội dung HTML mô tả chi tiết dạng “blog” cho sản phẩm
        // Blog giới thiệu chi tiết sản phẩm, KHÔNG chứa bảng thông số kỹ thuật
        private static string GenerateHtmlDescription(Product p, Dictionary<string, string> specs)
        {
            var sb = new StringBuilder();

            // Xác định đây có phải phụ tùng / linh kiện không
            bool isAccessory = !string.IsNullOrWhiteSpace(p.SKU) &&
                               p.SKU.StartsWith("PT-", StringComparison.OrdinalIgnoreCase);

            // Map BrandId -> tên hãng (theo thứ tự seed trong MasterDataSeeder)
            string brandName = p.BrandId switch
            {
                1 => "Honda",
                2 => "Yamaha",
                3 => "Suzuki",
                4 => "Piaggio",
                5 => "Vespa",
                6 => "SYM",
                7 => "Ducati",
                8 => "BMW Motorrad",
                9 => "Kawasaki",
                10 => "VinFast",
                _ => "MotorShop"
            };

            // Map CategoryId -> tên danh mục chính (tham khảo)
            string categoryName = p.CategoryId switch
            {
                1 => "Xe Tay Ga",
                2 => "Xe Số",
                3 => "Xe Côn Tay",
                4 => "Sportbike",
                5 => "Naked Bike",
                6 => "Adventure / Touring",
                7 => "Cruiser",
                8 => "Classic / Retro",
                9 => "Xe Điện",
                10 => "Phụ tùng & Linh kiện",
                _ => "Sản phẩm"
            };

            // "Tính cách" thương hiệu dùng để viết blog
            string brandTone = brandName switch
            {
                "Honda" => "bền bỉ, tiết kiệm xăng và rất dễ sử dụng trong điều kiện đường phố Việt Nam",
                "Yamaha" => "trẻ trung, cá tính với cảm giác lái thể thao",
                "Suzuki" => "mạnh mẽ, tăng tốc tốt – phù hợp người thích cảm giác bốc",
                "Vespa" => "thời trang, sang trọng và đậm chất phong cách Ý",
                "Piaggio" => "thời trang, sang trọng và đậm chất phong cách Ý",
                "VinFast" => "hiện đại, vận hành êm và tiết kiệm chi phí nhờ động cơ điện thông minh",
                "Ducati" => "đậm chất thể thao đường đua, dành cho người chơi xe thực thụ",
                "BMW Motorrad" => "cao cấp, ổn định và cực kỳ phù hợp cho những chuyến đi xa",
                _ => "được tối ưu cho nhu cầu đi lại thực tế tại Việt Nam"
            };

            sb.Append("<div class='product-detail-content' style='font-family:Arial,sans-serif;color:#333;line-height:1.6;'>");

            // ==================== PHỤ TÙNG / LINH KIỆN ====================
            if (isAccessory)
            {
                sb.AppendFormat(
                    "<h2 style='color:#0056b3;font-size:24px;margin-bottom:15px;'>Giới thiệu chi tiết {0}</h2>",
                    System.Net.WebUtility.HtmlEncode(p.Name)
                );

                sb.AppendFormat(
                    "<p style='font-size:16px;margin-bottom:16px;'><strong>{0}</strong> là phụ tùng/linh kiện nằm trong hệ sinh thái sản phẩm của <strong>{1}</strong>, " +
                    "phù hợp cho khách hàng muốn bảo dưỡng, thay thế hoặc nâng cấp xe một cách an toàn và đúng tiêu chuẩn.</p>",
                    System.Net.WebUtility.HtmlEncode(p.Name),
                    System.Net.WebUtility.HtmlEncode(brandName)
                );

                // Nội dung blog riêng theo từng loại phụ tùng (dựa trên SKU, Name) – dùng hàm bạn đã có
                // Hàm này KHÔNG nên in bảng thông số, chỉ nên mô tả kiểu công dụng / lợi ích
                BuildAccessoryDescription(sb, p);

                sb.Append("<h3 style='font-size:18px;margin:20px 0 8px;'>Lợi ích khi sử dụng đúng phụ tùng</h3>");
                sb.Append("<p style='margin-bottom:10px;'>Việc sử dụng phụ tùng phù hợp giúp xe vận hành ổn định hơn, giảm nguy cơ hư hỏng " +
                          "và đảm bảo các hệ thống phanh, truyền động, treo, điện hoạt động đúng như thiết kế ban đầu của nhà sản xuất.</p>");
                sb.Append("<p style='margin-bottom:10px;'>Khi thay thế định kỳ các chi tiết hao mòn như nhớt, lọc gió, bugi, bố thắng, vỏ xe..., " +
                          "bạn không chỉ giữ được cảm giác lái tốt mà còn tiết kiệm chi phí sửa chữa lớn về sau.</p>");

                sb.Append("<h3 style='font-size:18px;margin:20px 0 8px;'>Tư vấn lựa chọn & lắp đặt</h3>");
                sb.Append("<p style='margin-bottom:10px;'>Nếu bạn chưa chắc mã phụ tùng có phù hợp với dòng xe đang sử dụng hay không, " +
                          "hãy liên hệ đội ngũ tư vấn của MotorShop để được kiểm tra theo model xe hoặc số khung. " +
                          "Việc chọn đúng phụ tùng sẽ giúp quá trình lắp đặt nhanh hơn và hạn chế phát sinh lỗi.</p>");
                sb.Append("<p style='margin-bottom:0;'>MotorShop khuyến khích khách hàng lắp đặt tại các đại lý, gara uy tín " +
                          "hoặc trung tâm dịch vụ được ủy quyền nhằm đảm bảo đúng quy trình kỹ thuật và chế độ bảo hành.</p>");
            }
            // ==================== XE HOÀN CHỈNH (VISION, SH, EXCITER, PKL, XE ĐIỆN, …) ====================
            else
            {
                sb.AppendFormat(
                    "<h2 style='color:#0056b3;font-size:24px;margin-bottom:15px;'>Giới thiệu chi tiết {0}</h2>",
                    System.Net.WebUtility.HtmlEncode(p.Name)
                );

                sb.AppendFormat(
                    "<p style='font-size:16px;margin-bottom:16px;'><strong>{0}</strong> là mẫu <strong>{1}</strong> của thương hiệu <strong>{2}</strong>, " +
                    "được phát triển dành cho khách hàng đang tìm kiếm một chiếc xe {1} {3}. Đây là lựa chọn phù hợp cho nhu cầu di chuyển hằng ngày, " +
                    "đi làm, đi học cũng như những chuyến dạo phố cuối tuần.</p>",
                    System.Net.WebUtility.HtmlEncode(p.Name),
                    System.Net.WebUtility.HtmlEncode(categoryName.ToLower()),
                    System.Net.WebUtility.HtmlEncode(brandName),
                    System.Net.WebUtility.HtmlEncode(brandTone)
                );

                // 1. Thiết kế & hoàn thiện
                sb.Append("<h3 style='font-size:18px;margin:20px 0 8px;'>Thiết kế & hoàn thiện tổng thể</h3>");
                sb.Append("<p style='margin-bottom:10px;'>Mẫu xe sở hữu thiết kế cân đối với các đường nét được xử lý gọn gàng, " +
                          "phù hợp với vóc dáng của đa số người dùng Việt Nam. Cụm đèn chiếu sáng, mặt nạ trước, thân xe và đuôi xe " +
                          "được phối hài hòa, giúp xe trông hiện đại nhưng vẫn giữ được sự thực dụng trong quá trình sử dụng lâu dài.</p>");
                sb.Append("<p style='margin-bottom:10px;'>Bề mặt sơn và chi tiết nhựa được hoàn thiện tốt, hạn chế trầy xước trong điều kiện sử dụng thực tế. " +
                          "Tem và logo thương hiệu được bố trí nổi bật vừa đủ để khẳng định chất riêng mà không gây rối mắt.</p>");

                // 2. Tư thế ngồi & cảm giác lái
                sb.Append("<h3 style='font-size:18px;margin:20px 0 8px;'>Tư thế ngồi & cảm giác sử dụng</h3>");
                sb.Append("<p style='margin-bottom:10px;'>Tư thế ngồi trên xe khá thoải mái, tay lái đặt ở vị trí vừa tầm, " +
                          "giúp bạn dễ dàng quan sát phía trước và làm chủ tay lái trong nhiều tình huống. Yên xe êm, " +
                          "chiều dài đủ rộng cho cả người lái và người ngồi sau, phù hợp cho việc di chuyển hằng ngày trong phố cũng như đi đường dài.</p>");
                sb.Append("<p style='margin-bottom:10px;'>Xe cho cảm giác điều khiển thân thiện, dễ làm quen kể cả với người mới. " +
                          "Độ vặn ga, độ nặng tay lái, hành trình phanh đều được tinh chỉnh để mang lại sự tự tin cho người sử dụng.</p>");

                // 3. Trải nghiệm vận hành (chung, không lôi số liệu chi tiết)
                sb.Append("<h3 style='font-size:18px;margin:20px 0 8px;'>Trải nghiệm vận hành thực tế</h3>");
                sb.Append("<p style='margin-bottom:10px;'>Trong điều kiện vận hành thực tế, xe đáp ứng tốt nhu cầu đi lại nội đô với khả năng tăng tốc mượt mà, " +
                          "chạy ổn định ở dải tốc độ thường dùng. Khi di chuyển trên các đoạn đường xấu hoặc có nhiều gờ giảm tốc, " +
                          "hệ thống giảm xóc và khung sườn vẫn giữ được sự êm ái ở mức phù hợp.</p>");
                sb.Append("<p style='margin-bottom:10px;'>Khả năng tiết kiệm nhiên liệu, độ bền động cơ và chi phí bảo dưỡng là những điểm mạnh " +
                          "giúp mẫu xe này trở thành lựa chọn được nhiều khách hàng tin tưởng trong thời gian dài.</p>");

                // 4. Tiện ích sử dụng hằng ngày
                sb.Append("<h3 style='font-size:18px;margin:20px 0 8px;'>Tiện ích & tính thực dụng</h3>");
                sb.Append("<p style='margin-bottom:10px;'>Bên cạnh khả năng vận hành, mẫu xe còn được chú trọng vào trải nghiệm sử dụng: " +
                          "cốp xe rộng giúp chứa được nhiều vật dụng cá nhân, hộc đồ phía trước tiện để khẩu trang, găng tay; " +
                          "móc treo đồ hỗ trợ mang theo túi xách hoặc đồ mua sắm một cách gọn gàng.</p>");
                sb.Append("<p style='margin-bottom:10px;'>Tùy từng dòng xe, người dùng còn có thể hưởng lợi từ các tính năng như hệ thống khóa thông minh, " +
                          "cổng sạc thiết bị di động, đèn định vị ban ngày, bảng đồng hồ điện tử dễ quan sát… " +
                          "giúp việc sử dụng xe hằng ngày trở nên tiện nghi và an toàn hơn.</p>");

                // 5. Đối tượng khách hàng
                sb.Append("<h3 style='font-size:18px;margin:20px 0 8px;'>Đối tượng khách hàng phù hợp</h3>");
                sb.Append("<p style='margin-bottom:10px;'>Mẫu xe này phù hợp với nhiều nhóm khách hàng khác nhau: " +
                          "từ sinh viên cần phương tiện đi học, nhân viên văn phòng cần xe đi làm ổn định, " +
                          "cho tới các hộ gia đình cần một chiếc xe bền bỉ, dễ sử dụng cho nhiều thành viên.</p>");
                sb.Append("<p style='margin-bottom:10px;'>Nếu bạn ưu tiên sự cân bằng giữa thiết kế đẹp, chi phí sử dụng hợp lý " +
                          "và độ bền cao theo thời gian, đây là lựa chọn rất đáng cân nhắc trong phân khúc.</p>");

                // 6. Bảo dưỡng & sử dụng lâu dài
                sb.Append("<h3 style='font-size:18px;margin:20px 0 8px;'>Bảo dưỡng & sử dụng lâu dài</h3>");
                sb.Append("<p style='margin-bottom:10px;'>Để xe luôn trong tình trạng tốt, bạn nên tuân thủ lịch bảo dưỡng cơ bản như thay nhớt định kỳ, " +
                          "kiểm tra lốp, phanh, hệ thống chiếu sáng và ắc quy. Việc bảo dưỡng tại các trung tâm uy tín sẽ giúp phát hiện sớm " +
                          "những chi tiết cần thay thế, từ đó tối ưu chi phí và đảm bảo an toàn khi vận hành.</p>");
                sb.Append("<p style='margin-bottom:0;'>Khi có nhu cầu nâng cấp phụ tùng, trang bị thêm đồ chơi hoặc thay đổi phong cách xe, " +
                          "bạn có thể tham khảo các gói phụ tùng, linh kiện chính hãng được MotorShop giới thiệu để đảm bảo độ tương thích và tính thẩm mỹ.</p>");
            }

            sb.Append("</div>");

            return sb.ToString();
        }


        private static void BuildAccessoryDescription(StringBuilder sb, Product p)
        {
            switch (p.SKU)
            {
                // ============== NHỚT / DẦU ĐỘNG CƠ ==============

                case "PT-HD-OIL-10W30":
                    sb.Append("<h3>Nhớt Honda 10W-30 chính hãng</h3>");
                    sb.Append("<p>Nhớt máy Honda 10W-30 chuyên dùng cho Vision, Air Blade, Lead... " +
                              "giúp động cơ vận hành êm ái, giảm tiếng ồn và tiết kiệm nhiên liệu.</p>");
                    sb.Append("<ul>");
                    sb.Append("<li>Chuẩn nhớt phù hợp khí hậu Việt Nam, khởi động dễ ở mọi điều kiện.</li>");
                    sb.Append("<li>Giảm mài mòn chi tiết trong quá trình vận hành.</li>");
                    sb.Append("<li>Khuyến nghị thay định kỳ 2.000 – 3.000 km/lần.</li>");
                    sb.Append("</ul>");
                    break;

                case "PT-YM-OIL-BLUECORE":
                    sb.Append("<h3>Nhớt Yamaha Genuine Oil cho động cơ Blue Core</h3>");
                    sb.Append("<p>Dòng nhớt chính hãng Yamaha dành riêng cho động cơ Blue Core (Grande, Janus, FreeGo...). " +
                              "Giữ máy mát hơn, tăng tuổi thọ động cơ và tối ưu mức tiêu hao nhiên liệu.</p>");
                    sb.Append("<ul>");
                    sb.Append("<li>Công thức phụ gia làm sạch buồng đốt, hạn chế cặn bám.</li>");
                    sb.Append("<li>Giảm rung giật khi lên ga, đề-pa mượt hơn.</li>");
                    sb.Append("<li>Phù hợp chạy phố hằng ngày lẫn đi xa.</li>");
                    sb.Append("</ul>");
                    break;

                case "PT-SZ-OIL-ECSTAR":
                    sb.Append("<h3>Nhớt Suzuki Ecstar Chính Hãng</h3>");
                    sb.Append("<p>Ecstar là dòng nhớt được Suzuki phát triển riêng cho các mẫu Raider, Satria, GSX... " +
                              "giúp động cơ bốc hơn nhưng vẫn đảm bảo độ bền.</p>");
                    sb.Append("<ul>");
                    sb.Append("<li>Khả năng chịu nhiệt tốt, phù hợp vòng tua cao.</li>");
                    sb.Append("<li>Giảm hao nhớt khi chạy đường trường, đi tour.</li>");
                    sb.Append("<li>Tối ưu cho cả sử dụng hàng ngày và chạy thể thao.</li>");
                    sb.Append("</ul>");
                    break;

                case "PT-PG-OIL-5W40":
                    sb.Append("<h3>Dầu máy tổng hợp 5W-40 cho Vespa/Piaggio</h3>");
                    sb.Append("<p>Dầu nhớt 5W-40 cao cấp dành cho các dòng Vespa/Piaggio iGet, " +
                              "giúp động cơ vận hành êm, giảm rung giật và đáp ứng chuẩn khí thải mới.</p>");
                    sb.Append("<ul>");
                    sb.Append("<li>Độ nhớt ổn định, bảo vệ tốt khi nổ máy nguội.</li>");
                    sb.Append("<li>Giảm gõ máy, giữ máy êm khi chạy tốc độ cao.</li>");
                    sb.Append("<li>Phù hợp cho khách hàng muốn chăm xe kỹ, chạy êm, sạch máy.</li>");
                    sb.Append("</ul>");
                    break;

                // ============== LỌC GIÓ / LỌC GIÓ HIỆU NĂNG ==============

                case "PT-HD-AIR-VIS-LEAD":
                    sb.Append("<h3>Lọc gió Honda Vision/Lead chính hãng</h3>");
                    sb.Append("<p>Lọc gió dùng cho Vision, Lead và một số tay ga Honda 110–125cc. " +
                              "Giúp không khí vào buồng đốt sạch hơn, máy nổ êm và tiết kiệm xăng.</p>");
                    sb.Append("<ul>");
                    sb.Append("<li>Vật liệu lọc chuẩn Honda, độ bền cao.</li>");
                    sb.Append("<li>Khuyến nghị vệ sinh/kiểm tra mỗi 5.000 km, thay mới khoảng 12.000 km.</li>");
                    sb.Append("<li>Lắp vừa khít, không cần chế cháo, không báo lỗi FI.</li>");
                    sb.Append("</ul>");
                    break;

                case "PT-YM-AIR-NVX-GRD":
                    sb.Append("<h3>Lọc gió hiệu suất cao cho NVX/Grande</h3>");
                    sb.Append("<p>Lọc gió nâng cấp dành cho Yamaha NVX, Grande... tối ưu lượng gió vào động cơ, " +
                              "phù hợp cho khách hay chạy xa hoặc muốn xe bốc hơn.</p>");
                    sb.Append("<ul>");
                    sb.Append("<li>Lưu lượng gió cao hơn lọc zin nhưng vẫn đảm bảo lọc bụi tốt.</li>");
                    sb.Append("<li>Có thể vệ sinh, tái sử dụng (tùy loại), tiết kiệm chi phí lâu dài.</li>");
                    sb.Append("<li>Giúp nước ga đầu nhạy hơn, xe vọt hơn khi đề-pa.</li>");
                    sb.Append("</ul>");
                    break;

                // ============== NHÔNG SÊN DĨA / DÂY CUROA / TRUYỀN ĐỘNG ==============

                case "PT-HD-BELT-VAR-AB":
                    sb.Append("<h3>Dây curoa truyền động Honda Air Blade/Vision</h3>");
                    sb.Append("<p>Dây curoa zin cho các dòng tay ga Honda phổ biến. " +
                              "Đảm bảo truyền lực êm, hạn chế trượt dây và rung giật khi đề-pa.</p>");
                    sb.Append("<ul>");
                    sb.Append("<li>Chất liệu cao su chịu nhiệt, cốt sợi bố thép bền bỉ.</li>");
                    sb.Append("<li>Khuyến khích kiểm tra/ thay khi xe có hiện tượng hú, trượt ga.</li>");
                    sb.Append("<li>Lắp đúng size, không cần chế pat hay chỉnh lại nồi.</li>");
                    sb.Append("</ul>");
                    break;

                case "PT-SZ-SPRK-RAI-SAT":
                    sb.Append("<h3>Bộ nhông sên dĩa Suzuki Raider/Satria</h3>");
                    sb.Append("<p>Bộ nhông sên dĩa dành riêng cho Raider/Satria, " +
                              "tối ưu cho phong cách chạy thể thao và đi tour xa.</p>");
                    sb.Append("<ul>");
                    sb.Append("<li>Nhông dĩa bằng thép cứng, ít mòn, chịu tải tốt.</li>");
                    sb.Append("<li>Sên có phốt (O-ring/X-ring) giúp giữ mỡ bôi trơn lâu hơn.</li>");
                    sb.Append("<li>Giảm tiếng ồn, hạn chế chùng sên nhanh.</li>");
                    sb.Append("</ul>");
                    break;

                case "PT-DID-520VX3":
                    sb.Append("<h3>Sên DID 520VX3 cho PKL 600–1000cc</h3>");
                    sb.Append("<p>Dòng sên DID 520VX3 được nhiều biker PKL tin dùng, phù hợp xe 600–1000cc " +
                              "chạy tour, chạy track hoặc sử dụng hằng ngày.</p>");
                    sb.Append("<ul>");
                    sb.Append("<li>Công nghệ X-ring giúp giữ mỡ bôi trơn bên trong lâu hơn.</li>");
                    sb.Append("<li>Giảm ma sát, cải thiện khả năng tăng tốc.</li>");
                    sb.Append("<li>Độ bền cao, giảm tần suất tăng sên và căn chỉnh.</li>");
                    sb.Append("</ul>");
                    break;

                // ============== PHANH / ĐĨA / MÁ PHANH ==============

                case "PT-HD-BR-FR-WINX":
                    sb.Append("<h3>Má phanh trước Honda Winner X chính hãng</h3>");
                    sb.Append("<p>Bố thắng (má phanh) trước cho Winner X, " +
                              "đảm bảo lực phanh ổn định, êm và ít hư đĩa phanh.</p>");
                    sb.Append("<ul>");
                    sb.Append("<li>Hợp chất má phanh tối ưu cho chạy phố lẫn chạy xa.</li>");
                    sb.Append("<li>Ít bụi, hạn chế tiếng rít khi phanh gấp.</li>");
                    sb.Append("<li>Khuyến nghị thay khi mòn còn ~1/3 độ dày ban đầu.</li>");
                    sb.Append("</ul>");
                    break;

                case "PT-PKL-BR-BREMBO-FR":
                    sb.Append("<h3>Má phanh Brembo cho PKL (trước)</h3>");
                    sb.Append("<p>Một trong những lựa chọn phổ biến cho xe PKL nâng cấp phanh. " +
                              "Lực phanh mạnh, tuyến tính, giúp người lái dễ kiểm soát khi rà phanh.</p>");
                    sb.Append("<ul>");
                    sb.Append("<li>Phù hợp các dàn heo Brembo phổ biến (tùy mã cụ thể).</li>");
                    sb.Append("<li>Hiệu quả phanh tốt khi chạy đèo, track hoặc tốc độ cao.</li>");
                    sb.Append("<li>Khuyến nghị chạy rà (bed-in) má phanh sau khi lắp mới.</li>");
                    sb.Append("</ul>");
                    break;

                case "PT-PKL-DISC-FR320":
                    sb.Append("<h3>Đĩa phanh trước 320mm cho PKL</h3>");
                    sb.Append("<p>Đĩa phanh kích thước lớn 320mm giúp tăng hiệu quả phanh, " +
                              "phù hợp xe PKL độ heo lớn hoặc nhu cầu phanh mạnh hơn.</p>");
                    sb.Append("<ul>");
                    sb.Append("<li>Vật liệu thép chịu nhiệt, ít bị cong vênh.</li>");
                    sb.Append("<li>Thiết kế bông gió giúp tản nhiệt tốt, hạn chế fade phanh.</li>");
                    sb.Append("<li>Cần chọn pat heo & thớt lòng đúng chuẩn khi lắp.</li>");
                    sb.Append("</ul>");
                    break;

                // ============== ĐIỆN / BUGI / ẮC QUY ==============

                case "PT-NGK-CR7HSA":
                    sb.Append("<h3>Bugi NGK CR7HSA cho xe số/tay ga 110–125cc</h3>");
                    sb.Append("<p>Bugi NGK CR7HSA giúp đánh lửa mạnh, nổ máy dễ và ga lên đều hơn " +
                              "cho các dòng Vision, Wave, Sirius, Janus...</p>");
                    sb.Append("<ul>");
                    sb.Append("<li>Thương hiệu NGK Nhật Bản, độ bền cao.</li>");
                    sb.Append("<li>Giảm hụt ga, hạn chế hiện tượng giật khi tăng tốc.</li>");
                    sb.Append("<li>Nên thay định kỳ 8.000 – 12.000 km.</li>");
                    sb.Append("</ul>");
                    break;

                case "PT-BATT-12V5AH":
                    sb.Append("<h3>Ắc quy khô 12V–5Ah cho Vision/Janus/Grande</h3>");
                    sb.Append("<p>Ắc quy khô (MF) 12V–5Ah phù hợp nhiều dòng tay ga 110–125cc, " +
                              "đề máy khỏe, ít phải bảo dưỡng.</p>");
                    sb.Append("<ul>");
                    sb.Append("<li>Ắc quy kín khí, không cần châm nước định kỳ.</li>");
                    sb.Append("<li>Giữ điện tốt khi lâu không sử dụng xe.</li>");
                    sb.Append("<li>Thay đúng chuẩn dung lượng để tránh quá tải hệ thống điện.</li>");
                    sb.Append("</ul>");
                    break;

                // ============== LỐP / VỎ XE / KÍNH / ĐỒ CHƠI ==============

                case "PT-VF-TIRE-EVO200":
                    sb.Append("<h3>Lốp sau Evo200 cho xe máy điện</h3>");
                    sb.Append("<p>Lốp Evo200 dùng cho các dòng xe điện VinFast, " +
                              "giúp bám đường tốt, vận hành êm và an toàn trong điều kiện đường trơn trượt.</p>");
                    sb.Append("<ul>");
                    sb.Append("<li>Thiết kế gai lốp tối ưu cho đường phố Việt Nam.</li>");
                    sb.Append("<li>Hợp chất cao su bền, ít bị chai khi để lâu.</li>");
                    sb.Append("<li>Kết hợp tốt với hệ thống phanh đĩa/ABS trên xe điện.</li>");
                    sb.Append("</ul>");
                    break;

                case "PT-SCRN-WINX-EXC":
                    sb.Append("<h3>Kính chắn gió cao Winner X / Exciter 155</h3>");
                    sb.Append("<p>Kính chắn gió dạng cao giúp giảm gió tạt ngực khi chạy tốc độ cao, " +
                              "đồng thời tăng vẻ thể thao cho xe.</p>");
                    sb.Append("<ul>");
                    sb.Append("<li>Lắp vừa zin cho Winner X và Exciter 150/155.</li>");
                    sb.Append("<li>Vật liệu PC/ABS trong hoặc khói, chống nứt tốt.</li>");
                    sb.Append("<li>Nên siết ốc vừa tay, tránh nứt kính.</li>");
                    sb.Append("</ul>");
                    break;

                case "PT-MIR-SPT-10MM":
                    sb.Append("<h3>Cặp gương chiếu hậu kiểu thể thao ốc 10mm</h3>");
                    sb.Append("<p>Gương thể thao phù hợp đa số xe Nhật (Wave, Sirius, Winner, Exciter, NVX...). " +
                              "Giúp xe gọn gàng hơn nhưng vẫn đảm bảo tầm quan sát.</p>");
                    sb.Append("<ul>");
                    sb.Append("<li>Thiết kế nhỏ gọn, góc cạnh, phù hợp style xe độ.</li>");
                    sb.Append("<li>Ốc chân gương 10mm, lắp được cho nhiều dòng xe phổ biến.</li>");
                    sb.Append("<li>Cần chỉnh góc hợp lý để không mất tầm nhìn phía sau.</li>");
                    sb.Append("</ul>");
                    break;

                case "PT-GRIP-CNC-SPT":
                    sb.Append("<h3>Bao tay + gù CNC thể thao</h3>");
                    sb.Append("<p>Bộ bao tay cao su mềm kèm gù CNC giúp tăng độ êm khi cầm lái, " +
                              "giảm rung và trang trí xe nổi bật hơn.</p>");
                    sb.Append("<ul>");
                    sb.Append("<li>Bề mặt bao tay có vân chống trượt, cầm chắc tay khi trời mưa.</li>");
                    sb.Append("<li>Gù CNC anodized nhiều màu, phù hợp nhiều phong cách xe.</li>");
                    sb.Append("<li>Khi lắp cho xe côn dây/ga dây cần căn chỉnh tránh cấn bao tay.</li>");
                    sb.Append("</ul>");
                    break;

                // ============== MẶC ĐỊNH CHO CÁC PHỤ TÙNG KHÁC ==============

                default:
                    sb.Append("<h3>Phụ tùng & linh kiện chính hãng / cao cấp</h3>");
                    sb.Append("<p>Sản phẩm phụ tùng/linh kiện được chọn lọc, phù hợp cho nhu cầu sử dụng thực tế tại Việt Nam. " +
                              "Thông số chi tiết xem ở bảng bên dưới.</p>");
                    sb.Append("<ul>");
                    sb.Append("<li>Đảm bảo tương thích tốt với các dòng xe phổ biến.</li>");
                    sb.Append("<li>Nguồn gốc rõ ràng, chất lượng ổn định.</li>");
                    sb.Append("<li>Phù hợp lắp đặt tại xưởng dịch vụ/HEAD hoặc gara uy tín.</li>");
                    sb.Append("</ul>");
                    break;
            }
        }



    }
}
