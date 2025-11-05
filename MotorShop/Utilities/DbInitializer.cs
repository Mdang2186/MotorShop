// Utilities/DbInitializer.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MotorShop.Data;
using MotorShop.Models;
using NuGet.Packaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MotorShop.Utilities
{
    public class DbInitializer
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public DbInitializer(
            RoleManager<IdentityRole> roleManager,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _context = context;
        }

        public Task Initialize() => InitializeAsync();

        public async Task InitializeAsync()
        {
            // 1) Migrate
            try
            {
                var pending = await _context.Database.GetPendingMigrationsAsync();
                if (pending.Any()) await _context.Database.MigrateAsync();
            }
            catch
            {
                // TODO: log nếu cần
            }

            // 2) Roles
            if (!await _roleManager.RoleExistsAsync(SD.Role_Admin))
                await _roleManager.CreateAsync(new IdentityRole(SD.Role_Admin));
            if (!await _roleManager.RoleExistsAsync(SD.Role_User))
                await _roleManager.CreateAsync(new IdentityRole(SD.Role_User));

            // 3) Admin mặc định
            var adminEmail = "admin@motorshop.vn";
            var admin = await _userManager.FindByEmailAsync(adminEmail);
            if (admin == null)
            {
                admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "Administrator",
                    EmailConfirmed = true
                };
                await _userManager.CreateAsync(admin, "Admin@123"); // đổi khi deploy
                await _userManager.AddToRoleAsync(admin, SD.Role_Admin);
            }

            // 4) Branches
            if (!await _context.Branches.AnyAsync())
            {
                _context.Branches.AddRange(
                    new Branch { Name = "MotorShop Hoàn Kiếm", Address = "123 Tràng Tiền, Hoàn Kiếm, Hà Nội", Phone = "024-1234-5678", IsActive = true },
                    new Branch { Name = "MotorShop Cầu Giấy", Address = "88 Xuân Thuỷ, Cầu Giấy, Hà Nội", Phone = "024-8765-4321", IsActive = true },
                    new Branch { Name = "MotorShop Quận 1", Address = "15 Nguyễn Huệ, Q1, TP.HCM", Phone = "028-5678-1234", IsActive = true }
                );
                await _context.SaveChangesAsync();
            }

            // 5) Shippers (chỉ tạo nếu có DbSet)
            if (_context.Shippers != null && !await _context.Shippers.AnyAsync())
            {
                _context.Shippers.AddRange(
                    new Shipper { Name = "Giao Nhanh 247", Phone = "1900 1111", IsActive = true },
                    new Shipper { Name = "Siêu Tốc Express", Phone = "1900 2222", IsActive = true },
                    new Shipper { Name = "Tiết Kiệm Post", Phone = "1900 3333", IsActive = true }
                );
                await _context.SaveChangesAsync();
            }

            // 6) Banks — KHỚP VỚI Bank.cs (Name, Code, Logo, IsActive)
            if (_context.Banks != null && !await _context.Banks.AnyAsync())
            {
                _context.Banks.AddRange(
                    new Bank { Name = "Vietcombank", ShortName = "VCB", Code = "vcb", Bin = "970436", LogoUrl = "/images/banks/vcb.svg", SortOrder = 1, IsActive = true },
                    new Bank { Name = "Techcombank", ShortName = "TCB", Code = "tcb", Bin = "970407", LogoUrl = "/images/banks/tcb.svg", SortOrder = 2, IsActive = true },
                    new Bank { Name = "VietinBank", ShortName = "VTB", Code = "vtb", Bin = "970415", LogoUrl = "/images/banks/vietin.svg", SortOrder = 3, IsActive = true },
                    new Bank { Name = "ACB", ShortName = "ACB", Code = "acb", Bin = "970416", LogoUrl = "/images/banks/acb.svg", SortOrder = 4, IsActive = true },
                    new Bank { Name = "MB Bank", ShortName = "MB", Code = "mb", Bin = "970422", LogoUrl = "/images/banks/mb.svg", SortOrder = 5, IsActive = true },
                    new Bank { Name = "VPBank", ShortName = "VPB", Code = "vpb", Bin = "970432", LogoUrl = "/images/banks/vpb.svg", SortOrder = 6, IsActive = true }
                );
                await _context.SaveChangesAsync();
            }

            // 7) Brands & Categories
            if (!await _context.Brands.AnyAsync())
            {
                _context.Brands.AddRange(
                    new Brand { Name = "Honda" },
                    new Brand { Name = "Yamaha" },
                    new Brand { Name = "Suzuki" },
                    new Brand { Name = "Piaggio" },
                    new Brand { Name = "SYM" },
                    new Brand { Name = "Kymco" },
                    new Brand { Name = "Ducati" },
                    new Brand { Name = "BMW Motorrad" }
                );
                await _context.SaveChangesAsync();
            }
            if (!await _context.Categories.AnyAsync())
            {
                _context.Categories.AddRange(
                    new Category { Name = "Xe tay ga" },
                    new Category { Name = "Xe số" },
                    new Category { Name = "Sportbike" },
                    new Category { Name = "Naked bike" },
                    new Category { Name = "Cruiser" },
                    new Category { Name = "Adventure" },
                    new Category { Name = "Classic" }
                );
                await _context.SaveChangesAsync();
            }

            // Lấy thực thể
            var honda = await _context.Brands.FirstAsync(b => b.Name == "Honda");
            var yamaha = await _context.Brands.FirstAsync(b => b.Name == "Yamaha");
            var suzuki = await _context.Brands.FirstAsync(b => b.Name == "Suzuki");
            var piaggio = await _context.Brands.FirstAsync(b => b.Name == "Piaggio");
            var sym = await _context.Brands.FirstAsync(b => b.Name == "SYM");
            var kymco = await _context.Brands.FirstAsync(b => b.Name == "Kymco");
            var ducati = await _context.Brands.FirstAsync(b => b.Name == "Ducati");
            var bmw = await _context.Brands.FirstAsync(b => b.Name == "BMW Motorrad");

            var tayGa = await _context.Categories.FirstAsync(c => c.Name == "Xe tay ga");
            var xeSo = await _context.Categories.FirstAsync(c => c.Name == "Xe số");
            var sport = await _context.Categories.FirstAsync(c => c.Name == "Sportbike");
            var naked = await _context.Categories.FirstAsync(c => c.Name == "Naked bike");
            var cruiser = await _context.Categories.FirstAsync(c => c.Name == "Cruiser");
            var adv = await _context.Categories.FirstAsync(c => c.Name == "Adventure");
            var classic = await _context.Categories.FirstAsync(c => c.Name == "Classic");

            // 8) Products (giữ nguyên danh sách của bạn)
            if (!await _context.Products.AnyAsync())
            {
                var products = new Product[]
 {
   
    // --- HONDA / Tay ga ---
    new Product { Name = "Honda Vision 2024 - Cao cấp", Price = 32_000_000M, StockQuantity = 50, Year = 2024, BrandId = honda.Id, CategoryId = tayGa.Id, ImageUrl="/images/products/honda/vision-2024-cao-cap.png" },
    new Product { Name = "Honda Air Blade 160 - Đặc biệt", Price = 58_000_000M, OriginalPrice = 60_000_000M, StockQuantity = 30, Year = 2024, BrandId = honda.Id, CategoryId = tayGa.Id, ImageUrl="/images/products/honda/air-blade-160-dac-biet.png" },
    new Product { Name = "Honda SH 125i - Thể thao", Price = 81_000_000M, StockQuantity = 25, Year = 2023, BrandId = honda.Id, CategoryId = tayGa.Id, ImageUrl="/images/products/honda/sh-125i-the-thao.png" },
    new Product { Name = "Honda SH 160i - Đặc biệt", Price = 101_000_000M, StockQuantity = 15, Year = 2024, BrandId = honda.Id, CategoryId = tayGa.Id, ImageUrl="/images/products/honda/sh-160i-dac-biet.png" },
    new Product { Name = "Honda SH Mode - Thể thao", Price = 64_000_000M, StockQuantity = 40, Year = 2023, BrandId = honda.Id, CategoryId = tayGa.Id, ImageUrl="/images/products/honda/sh-mode-the-thao.png" },
    new Product { Name = "Honda Lead 125 - Đặc biệt", Price = 42_500_000M, StockQuantity = 60, Year = 2022, BrandId = honda.Id, CategoryId = tayGa.Id, ImageUrl="/images/products/honda/lead-125-dac-biet.png" },
    new Product { Name = "Honda Vario 160 - Thể thao", Price = 55_000_000M, StockQuantity = 28, Year = 2024, BrandId = honda.Id, CategoryId = tayGa.Id, ImageUrl="/images/products/honda/vario-160-the-thao.png" },
    new Product { Name = "Honda PCX 160", Price = 80_000_000M, StockQuantity = 12, Year = 2023, BrandId = honda.Id, CategoryId = tayGa.Id, ImageUrl="/images/products/honda/pcx-160.png" },

    // --- HONDA / Xe số ---
    new Product { Name = "Honda Wave Alpha 110 - Đặc biệt", Price = 18_500_000M, StockQuantity = 100, Year = 2024, BrandId = honda.Id, CategoryId = xeSo.Id, ImageUrl="/images/products/honda/wave-alpha-110-dac-biet.png" },
    new Product { Name = "Honda Blade 110 - Thể thao", Price = 21_000_000M, StockQuantity = 70, Year = 2023, BrandId = honda.Id, CategoryId = xeSo.Id, ImageUrl="/images/products/honda/blade-110-the-thao.png" },
    new Product { Name = "Honda Future 125 FI - Đặc biệt", Price = 32_000_000M, StockQuantity = 55, Year = 2024, BrandId = honda.Id, CategoryId = xeSo.Id, ImageUrl="/images/products/honda/future-125-fi-dac-biet.png" },
    new Product { Name = "Honda Wave RSX FI 110", Price = 22_500_000M, StockQuantity = 80, Year = 2023, BrandId = honda.Id, CategoryId = xeSo.Id, ImageUrl="/images/products/honda/wave-rsx-fi-110.png" },

    // --- HONDA / Classic ---
    new Product { Name = "Honda Super Cub C125", Price = 86_000_000M, StockQuantity = 10, Year = 2022, BrandId = honda.Id, CategoryId = classic.Id, ImageUrl="/images/products/honda/super-cub-c125.png" },
    new Product { Name = "Honda Monkey", Price = 85_000_000M, StockQuantity = 7, Year = 2023, BrandId = honda.Id, CategoryId = classic.Id, ImageUrl="/images/products/honda/monkey.png" },

    // --- HONDA / Sportbike ---
    new Product { Name = "Honda CBR150R - Thể thao", Price = 72_000_000M, StockQuantity = 20, Year = 2023, BrandId = honda.Id, CategoryId = sport.Id, ImageUrl="/images/products/honda/cbr150r-the-thao.png" },
    new Product { Name = "Honda CBR250RR", Price = 150_000_000M, StockQuantity = 8, Year = 2022, BrandId = honda.Id, CategoryId = sport.Id, ImageUrl="/images/products/honda/cbr250rr.png" },

    // --- HONDA / Naked ---
    new Product { Name = "Honda Winner X 150 - Đặc biệt", Price = 50_000_000M, StockQuantity = 45, Year = 2024, BrandId = honda.Id, CategoryId = naked.Id, ImageUrl="/images/products/honda/winner-x-150-dac-biet.png" },
    new Product { Name = "Honda CB150R The Streetster", Price = 105_000_000M, StockQuantity = 8, Year = 2021, BrandId = honda.Id, CategoryId = naked.Id, ImageUrl="/images/products/honda/cb150r-the-streetster.png" },
    new Product { Name = "Honda CB300R", Price = 140_000_000M, StockQuantity = 6, Year = 2023, BrandId = honda.Id, CategoryId = naked.Id, ImageUrl="/images/products/honda/cb300r.png" },
    new Product { Name = "Honda MSX 125", Price = 50_000_000M, StockQuantity = 14, Year = 2022, BrandId = honda.Id, CategoryId = naked.Id, ImageUrl="/images/products/honda/msx-125.png" },

    // --- HONDA / Cruiser ---
    new Product { Name = "Honda Rebel 300", Price = 125_000_000M, StockQuantity = 5, Year = 2022, BrandId = honda.Id, CategoryId = cruiser.Id, ImageUrl="/images/products/honda/rebel-300.png" },
    new Product { Name = "Honda Rebel 500", Price = 180_000_000M, StockQuantity = 9, Year = 2024, BrandId = honda.Id, CategoryId = cruiser.Id, ImageUrl="/images/products/honda/rebel-500.png" },
    new Product { Name = "Honda Shadow Phantom 750", Price = 250_000_000M, StockQuantity = 3, Year = 2021, BrandId = honda.Id, CategoryId = cruiser.Id, ImageUrl="/images/products/honda/shadow-phantom-750.png" },

    // --- HONDA / Adventure ---
    new Product { Name = "Honda CB500X", Price = 194_000_000M, StockQuantity = 7, Year = 2023, BrandId = honda.Id, CategoryId = adv.Id, ImageUrl="/images/products/honda/cb500x.png" },
    new Product { Name = "Honda Africa Twin", Price = 590_000_000M, StockQuantity = 4, Year = 2024, BrandId = honda.Id, CategoryId = adv.Id, ImageUrl="/images/products/honda/africa-twin.png" },
    new Product { Name = "Honda Transalp", Price = 309_000_000M, StockQuantity = 6, Year = 2024, BrandId = honda.Id, CategoryId = adv.Id, ImageUrl="/images/products/honda/transalp.png" },
    new Product { Name = "Honda CRF300L", Price = 130_000_000M, StockQuantity = 11, Year = 2023, BrandId = honda.Id, CategoryId = adv.Id, ImageUrl="/images/products/honda/crf300l.png" },
    new Product { Name = "Honda Gold Wing", Price = 1_230_000_000M, StockQuantity = 2, Year = 2023, BrandId = honda.Id, CategoryId = cruiser.Id, ImageUrl="/images/products/honda/gold-wing.png" },
    new Product { Name = "Honda ADV 160", Price = 95_000_000M, StockQuantity = 18, Year = 2024, BrandId = honda.Id, CategoryId = adv.Id, ImageUrl="/images/products/honda/adv-160.png" },

    // --- YAMAHA ---
    new Product { Name = "Yamaha Janus - Giới hạn", Price = 29_000_000M, OriginalPrice = 32_000_000M, StockQuantity = 55, Year = 2024, BrandId = yamaha.Id, CategoryId = tayGa.Id, ImageUrl="/images/products/yamaha/janus-gioi-han.png" },
    new Product { Name = "Yamaha Grande - Giới hạn", Price = 51_000_000M, StockQuantity = 35, Year = 2023, BrandId = yamaha.Id, CategoryId = tayGa.Id, ImageUrl="/images/products/yamaha/grande-gioi-han.png" },
    new Product { Name = "Yamaha Latte", Price = 38_000_000M, StockQuantity = 48, Year = 2022, BrandId = yamaha.Id, CategoryId = tayGa.Id, ImageUrl="/images/products/yamaha/latte.png" },
    new Product { Name = "Yamaha FreeGo - Đặc biệt", Price = 34_000_000M, StockQuantity = 33, Year = 2023, BrandId = yamaha.Id, CategoryId = tayGa.Id, ImageUrl="/images/products/yamaha/freego-dac-biet.png" },
    new Product { Name = "Yamaha NVX 155 VVA", Price = 55_000_000M, StockQuantity = 22, Year = 2024, BrandId = yamaha.Id, CategoryId = tayGa.Id, ImageUrl="/images/products/yamaha/nvx-155-vva.png" },
    new Product { Name = "Yamaha Sirius FI", Price = 21_500_000M, StockQuantity = 90, Year = 2024, BrandId = yamaha.Id, CategoryId = xeSo.Id, ImageUrl="/images/products/yamaha/sirius-fi.png" },
    new Product { Name = "Yamaha Jupiter Finn", Price = 28_000_000M, StockQuantity = 65, Year = 2023, BrandId = yamaha.Id, CategoryId = xeSo.Id, ImageUrl="/images/products/yamaha/jupiter-finn.png" },
    new Product { Name = "Yamaha Jupiter FI", Price = 30_000_000M, StockQuantity = 60, Year = 2022, BrandId = yamaha.Id, CategoryId = xeSo.Id, ImageUrl="/images/products/yamaha/jupiter-fi.png" },
    new Product { Name = "Yamaha Exciter 155 VVA - GP", Price = 51_000_000M, OriginalPrice = 52_000_000M, StockQuantity = 40, Year = 2024, BrandId = yamaha.Id, CategoryId = naked.Id, ImageUrl="/images/products/yamaha/exciter-155-vva-gp.png" },
    new Product { Name = "Yamaha MT-15", Price = 69_000_000M, StockQuantity = 24, Year = 2022, BrandId = yamaha.Id, CategoryId = naked.Id, ImageUrl="/images/products/yamaha/mt-15.png" },
    new Product { Name = "Yamaha MT-03", Price = 129_000_000M, StockQuantity = 7, Year = 2022, BrandId = yamaha.Id, CategoryId = naked.Id, ImageUrl="/images/products/yamaha/mt-03.png" },
    new Product { Name = "Yamaha MT-09", Price = 345_000_000M, StockQuantity = 5, Year = 2023, BrandId = yamaha.Id, CategoryId = naked.Id, ImageUrl="/images/products/yamaha/mt-09.png" },
    new Product { Name = "Yamaha YZF-R15", Price = 78_000_000M, StockQuantity = 18, Year = 2023, BrandId = yamaha.Id, CategoryId = sport.Id, ImageUrl="/images/products/yamaha/yzf-r15.png" },
    new Product { Name = "Yamaha YZF-R3", Price = 132_000_000M, StockQuantity = 9, Year = 2021, BrandId = yamaha.Id, CategoryId = sport.Id, ImageUrl="/images/products/yamaha/yzf-r3.png" },
    new Product { Name = "Yamaha YZF-R7", Price = 269_000_000M, StockQuantity = 6, Year = 2024, BrandId = yamaha.Id, CategoryId = sport.Id, ImageUrl="/images/products/yamaha/yzf-r7.png" },
    new Product { Name = "Yamaha XS155R", Price = 77_000_000M, StockQuantity = 14, Year = 2023, BrandId = yamaha.Id, CategoryId = classic.Id, ImageUrl="/images/products/yamaha/xs155r.png" },
    new Product { Name = "Yamaha XSR900", Price = 359_000_000M, StockQuantity = 4, Year = 2023, BrandId = yamaha.Id, CategoryId = classic.Id, ImageUrl="/images/products/yamaha/xsr900.png" },
    new Product { Name = "Yamaha PG-1", Price = 31_000_000M, StockQuantity = 30, Year = 2024, BrandId = yamaha.Id, CategoryId = adv.Id, ImageUrl="/images/products/yamaha/pg-1.png" },
    new Product { Name = "Yamaha Tenere 700", Price = 399_000_000M, StockQuantity = 3, Year = 2023, BrandId = yamaha.Id, CategoryId = adv.Id, ImageUrl="/images/products/yamaha/tenere-700.png" },
    new Product { Name = "Yamaha Tracer 9 GT", Price = 369_000_000M, StockQuantity = 5, Year = 2024, BrandId = yamaha.Id, CategoryId = adv.Id, ImageUrl="/images/products/yamaha/tracer-9-gt.png" },

    // --- SUZUKI ---
    new Product { Name = "Suzuki Address 110 FI", Price = 29_000_000M, StockQuantity = 25, Year = 2022, BrandId = suzuki.Id, CategoryId = tayGa.Id, ImageUrl="/images/products/suzuki/address-110-fi.png" },
    new Product { Name = "Suzuki Burgman Street 125", Price = 49_000_000M, StockQuantity = 15, Year = 2023, BrandId = suzuki.Id, CategoryId = tayGa.Id, ImageUrl="/images/products/suzuki/burgman-street-125.png" },
    new Product { Name = "Suzuki Viva 115 FI", Price = 23_000_000M, StockQuantity = 30, Year = 2020, BrandId = suzuki.Id, CategoryId = xeSo.Id, ImageUrl="/images/products/suzuki/viva-115-fi.png" },
    new Product { Name = "Suzuki Axelo 125", Price = 28_000_000M, StockQuantity = 18, Year = 2019, BrandId = suzuki.Id, CategoryId = xeSo.Id, ImageUrl="/images/products/suzuki/axelo-125.png" },
    new Product { Name = "Suzuki Raider R150 FI", Price = 51_000_000M, OriginalPrice = 51_500_000M, StockQuantity = 28, Year = 2024, BrandId = suzuki.Id, CategoryId = naked.Id, ImageUrl="/images/products/suzuki/raider-r150-fi.png" },
    new Product { Name = "Suzuki Satria F150", Price = 53_000_000M, StockQuantity = 26, Year = 2023, BrandId = suzuki.Id, CategoryId = naked.Id, ImageUrl="/images/products/suzuki/satria-f150.png" },
    new Product { Name = "Suzuki GSX-S150", Price = 69_000_000M, StockQuantity = 13, Year = 2022, BrandId = suzuki.Id, CategoryId = naked.Id, ImageUrl="/images/products/suzuki/gsx-s150.png" },
    new Product { Name = "Suzuki GSX-R150", Price = 72_000_000M, StockQuantity = 11, Year = 2022, BrandId = suzuki.Id, CategoryId = sport.Id, ImageUrl="/images/products/suzuki/gsx-r150.png" },
    new Product { Name = "Suzuki Gixxer SF250", Price = 125_000_000M, StockQuantity = 8, Year = 2023, BrandId = suzuki.Id, CategoryId = sport.Id, ImageUrl="/images/products/suzuki/gixxer-sf250.png" },
    new Product { Name = "Suzuki GZ150-A", Price = 64_000_000M, StockQuantity = 9, Year = 2021, BrandId = suzuki.Id, CategoryId = cruiser.Id, ImageUrl="/images/products/suzuki/gz150-a.png" },
    new Product { Name = "Suzuki Intruder 150", Price = 89_000_000M, StockQuantity = 6, Year = 2022, BrandId = suzuki.Id, CategoryId = cruiser.Id, ImageUrl="/images/products/suzuki/intruder-150.png" },
    new Product { Name = "Suzuki V-Strom 250SX", Price = 135_000_000M, StockQuantity = 7, Year = 2024, BrandId = suzuki.Id, CategoryId = adv.Id, ImageUrl="/images/products/suzuki/v-strom-250sx.png" },

    // --- PIAGGIO ---
    new Product { Name = "Piaggio Vespa Sprint S 125", Price = 81_000_000M, StockQuantity = 15, Year = 2024, BrandId = piaggio.Id, CategoryId = tayGa.Id, ImageUrl="/images/products/piaggio/vespa-sprint-s-125.png" },
    new Product { Name = "Piaggio Vespa Primavera S 125", Price = 78_000_000M, StockQuantity = 18, Year = 2023, BrandId = piaggio.Id, CategoryId = tayGa.Id, ImageUrl="/images/products/piaggio/vespa-primavera-s-125.png" },
    new Product { Name = "Piaggio Liberty S 125", Price = 60_000_000M, StockQuantity = 22, Year = 2024, BrandId = piaggio.Id, CategoryId = tayGa.Id, ImageUrl="/images/products/piaggio/liberty-s-125.png" },
    new Product { Name = "Piaggio Medley S 150", Price = 97_000_000M, StockQuantity = 12, Year = 2023, BrandId = piaggio.Id, CategoryId = tayGa.Id, ImageUrl="/images/products/piaggio/medley-s-150.png" },
    new Product { Name = "Piaggio Vespa GTS Super Sport 150", Price = 116_000_000M, StockQuantity = 10, Year = 2022, BrandId = piaggio.Id, CategoryId = tayGa.Id, ImageUrl="/images/products/piaggio/vespa-gts-super-sport-150.png" },
    new Product { Name = "Piaggio Zip 100", Price = 36_000_000M, StockQuantity = 25, Year = 2021, BrandId = piaggio.Id, CategoryId = tayGa.Id, ImageUrl="/images/products/piaggio/zip-100.png" },
    new Product { Name = "Piaggio Beverly S 400", Price = 235_000_000M, StockQuantity = 5, Year = 2024, BrandId = piaggio.Id, CategoryId = tayGa.Id, ImageUrl="/images/products/piaggio/beverly-s-400.png" },
    new Product { Name = "Piaggio Vespa 946", Price = 405_000_000M, StockQuantity = 3, Year = 2023, BrandId = piaggio.Id, CategoryId = tayGa.Id, ImageUrl="/images/products/piaggio/vespa-946.png" },

    // --- SYM & KYMCO ---
    new Product { Name = "SYM Attila 125", Price = 34_000_000M, StockQuantity = 30, Year = 2022, BrandId = sym.Id, CategoryId = tayGa.Id, ImageUrl="/images/products/sym/attila-125.png" },
    new Product { Name = "SYM Passing 50", Price = 25_000_000M, StockQuantity = 40, Year = 2023, BrandId = sym.Id, CategoryId = tayGa.Id, ImageUrl="/images/products/sym/passing-50.png" },
    new Product { Name = "SYM Shark 125", Price = 48_000_000M, StockQuantity = 15, Year = 2024, BrandId = sym.Id, CategoryId = tayGa.Id, ImageUrl="/images/products/sym/shark-125.png" },
    new Product { Name = "SYM Elite 50", Price = 22_000_000M, StockQuantity = 35, Year = 2022, BrandId = sym.Id, CategoryId = tayGa.Id, ImageUrl="/images/products/sym/elite-50.png" },
    new Product { Name = "Kymco Like 125", Price = 36_000_000M, StockQuantity = 28, Year = 2023, BrandId = kymco.Id, CategoryId = tayGa.Id, ImageUrl="/images/products/kymco/like-125.png" },
    new Product { Name = "Kymco Candy Hermosa 50", Price = 26_000_000M, StockQuantity = 45, Year = 2024, BrandId = kymco.Id, CategoryId = tayGa.Id, ImageUrl="/images/products/kymco/candy-hermosa-50.png" },
    new Product { Name = "SYM Elegant 110", Price = 17_000_000M, StockQuantity = 50, Year = 2023, BrandId = sym.Id, CategoryId = xeSo.Id, ImageUrl="/images/products/sym/elegant-110.png" },
    new Product { Name = "Kymco K-Pipe 125", Price = 37_000_000M, StockQuantity = 20, Year = 2022, BrandId = kymco.Id, CategoryId = naked.Id, ImageUrl="/images/products/kymco/k-pipe-125.png" },

    // --- DUCATI ---
    new Product { Name = "Ducati Monster 937", Price = 440_000_000M, StockQuantity = 6, Year = 2023, BrandId = ducati.Id, CategoryId = naked.Id, ImageUrl="/images/products/ducati/monster-937.png" },
    new Product { Name = "Ducati Streetfighter V4 S", Price = 790_000_000M, StockQuantity = 4, Year = 2024, BrandId = ducati.Id, CategoryId = naked.Id, ImageUrl="/images/products/ducati/streetfighter-v4-s.png" },
    new Product { Name = "Ducati Panigale V4 S", Price = 940_000_000M, StockQuantity = 3, Year = 2023, BrandId = ducati.Id, CategoryId = sport.Id, ImageUrl="/images/products/ducati/panigale-v4-s.png" },
    new Product { Name = "Ducati SuperSport 950 S", Price = 580_000_000M, StockQuantity = 5, Year = 2024, BrandId = ducati.Id, CategoryId = sport.Id, ImageUrl="/images/products/ducati/supersport-950-s.png" },
    new Product { Name = "Ducati Multistrada V4 S", Price = 800_000_000M, StockQuantity = 4, Year = 2023, BrandId = ducati.Id, CategoryId = adv.Id, ImageUrl="/images/products/ducati/multistrada-v4-s.png" },
    new Product { Name = "Ducati Scrambler Icon", Price = 330_000_000M, StockQuantity = 7, Year = 2024, BrandId = ducati.Id, CategoryId = classic.Id, ImageUrl="/images/products/ducati/scrambler-icon.png" },

    // --- BMW MOTORRAD ---
    new Product { Name = "BMW G 310 R", Price = 189_000_000M, StockQuantity = 8, Year = 2023, BrandId = bmw.Id, CategoryId = naked.Id, ImageUrl="/images/products/bmw/g-310-r.png" },
    new Product { Name = "BMW S 1000 R", Price = 670_000_000M, StockQuantity = 4, Year = 2024, BrandId = bmw.Id, CategoryId = naked.Id, ImageUrl="/images/products/bmw/s-1000-r.png" },
    new Product { Name = "BMW S 1000 RR", Price = 950_000_000M, StockQuantity = 3, Year = 2023, BrandId = bmw.Id, CategoryId = sport.Id, ImageUrl="/images/products/bmw/s-1000-rr.png" },
    new Product { Name = "BMW G 310 GS", Price = 219_000_000M, StockQuantity = 9, Year = 2023, BrandId = bmw.Id, CategoryId = adv.Id, ImageUrl="/images/products/bmw/g-310-gs.png" },
    new Product { Name = "BMW R 1250 GS Adventure", Price = 720_000_000M, StockQuantity = 5, Year = 2024, BrandId = bmw.Id, CategoryId = adv.Id, ImageUrl="/images/products/bmw/r-1250-gs-adventure.png" },
    new Product { Name = "BMW R nineT Scrambler", Price = 540_000_000M, StockQuantity = 6, Year = 2023, BrandId = bmw.Id, CategoryId = classic.Id, ImageUrl="/images/products/bmw/r-ninet-scrambler.png" }
 };

                _context.Products.AddRange(products);
                await _context.SaveChangesAsync();
            }

            // 9) Gallery + Specs
            var allProducts = await _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Specifications)
                .ToListAsync();

            var brandGallery = BuildBrandGallery();

            foreach (var p in allProducts)
            {
                // Mô tả dài
                if (string.IsNullOrWhiteSpace(p.Description) || p.Description!.Length < 220)
                    p.Description = BuildLongDescription(p);

                // Ảnh: >= 3 ảnh
                if (p.Images == null) p.Images = new List<ProductImage>();
                if (p.Images.Count < 3)
                {
                    p.Images.Clear();

                    List<string> urls;
                    if (!brandGallery.TryGetValue(p.Brand.Name, out urls) || urls.Count < 3)
                    {
                        urls = new List<string>
                        {
                            "https://images.unsplash.com/photo-1502877338535-766e1452684a?auto=format&fit=crop&w=1600&q=80",
                            "https://images.unsplash.com/photo-1519861531473-9200262188bf?auto=format&fit=crop&w=1600&q=80",
                            "https://images.unsplash.com/photo-1542362567-b07e54358753?auto=format&fit=crop&w=1600&q=80"
                        };
                    }

                    for (int i = 0; i < urls.Count; i++)
                    {
                        p.Images.Add(new ProductImage
                        {
                            ImageUrl = urls[i],
                            // Nếu Caption NOT NULL trong DB → gán luôn:
                            Caption = $"{p.Name} - ảnh {i + 1}",
                            SortOrder = i + 1,
                            IsPrimary = (i == 0)
                        });
                    }

                    // Ảnh chính cho Product
                    p.ImageUrl = p.Images.OrderBy(i => i.SortOrder).First().ImageUrl;
                }

                // Specs
                if (p.Specifications == null) p.Specifications = new List<ProductSpecification>();
                if (p.Specifications.Count == 0)
                {
                    var isScooter = p.Category.Name.Contains("ga", StringComparison.OrdinalIgnoreCase);
                    var isSport = p.Category.Name.Contains("Sport", StringComparison.OrdinalIgnoreCase);
                    var gearbox = isScooter ? "CVT" : (isSport ? "6 cấp" : "5 cấp");

                    p.Specifications.AddRange(new[]
                    {
                        new ProductSpecification { Name = "Động cơ",           Value = isScooter ? "SOHC, 4 kỳ, xilanh đơn, làm mát bằng gió" : "DOHC, 4 kỳ, xilanh đơn, làm mát bằng chất lỏng", SortOrder = 1 },
                        new ProductSpecification { Name = "Dung tích xy-lanh", Value = GuessDisplacement(p.Name), SortOrder = 2 },
                        new ProductSpecification { Name = "Công suất cực đại", Value = "11 kW @ 8.500 rpm (tham khảo)", SortOrder = 3 },
                        new ProductSpecification { Name = "Mô-men xoắn",       Value = "14 Nm @ 6.500 rpm (tham khảo)", SortOrder = 4 },
                        new ProductSpecification { Name = "Hộp số",            Value = gearbox, SortOrder = 5 },
                        new ProductSpecification { Name = "Bình xăng",         Value = isScooter ? "5.5 L" : "12 L", SortOrder = 6 },
                        new ProductSpecification { Name = "Khối lượng",        Value = isScooter ? "112 kg" : "140 kg", SortOrder = 7 },
                        new ProductSpecification { Name = "Phanh",             Value = isScooter ? "Đĩa trước / phanh sau" : "Đĩa trước/sau, ABS (tuỳ phiên bản)", SortOrder = 8 }
                    });
                }
            }

            // 10) Vá an toàn trước khi lưu: tránh NULL cho Caption nếu cột NOT NULL
            foreach (var e in _context.ChangeTracker.Entries<ProductImage>()
                         .Where(x => (x.State == EntityState.Added || x.State == EntityState.Modified)))
            {
                if (e.Entity.Caption == null) e.Entity.Caption = "";
                if (e.Entity.SortOrder <= 0) e.Entity.SortOrder = 1;
            }

            await _context.SaveChangesAsync();
        }

        // ===== Helpers =====
        private static Dictionary<string, List<string>> BuildBrandGallery()
        {
            return new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                { "Honda", new List<string>
                    {
                        "https://images.unsplash.com/photo-1558980664-10eaaffc2d4f?auto=format&fit=crop&w=1600&q=80",
                        "https://images.unsplash.com/photo-1616406432815-bf2269935a78?auto=format&fit=crop&w=1600&q=80",
                        "https://images.unsplash.com/photo-1502877338535-766e1452684a?auto=format&fit=crop&w=1600&q=80",
                        "https://images.unsplash.com/photo-1558980663-656b2a38f73a?auto=format&fit=crop&w=1600&q=80"
                    }
                },
                { "Yamaha", new List<string>
                    {
                        "https://images.unsplash.com/photo-1501691223387-dd0500403074?auto=format&fit=crop&w=1600&q=80",
                        "https://images.unsplash.com/photo-1504215680853-026ed2a45def?auto=format&fit=crop&w=1600&q=80",
                        "https://images.unsplash.com/photo-1568772052428-8086b7a373d1?auto=format&fit=crop&w=1600&q=80"
                    }
                },
                { "Suzuki", new List<string>
                    {
                        "https://images.unsplash.com/photo-1485968579580-b6d095142e6e?auto=format&fit=crop&w=1600&q=80",
                        "https://images.unsplash.com/photo-1518221976525-4b41bc7af902?auto=format&fit=crop&w=1600&q=80",
                        "https://images.unsplash.com/photo-1558980664-97f2440b7b03?auto=format&fit=crop&w=1600&q=80"
                    }
                },
                { "Piaggio", new List<string>
                    {
                        "https://images.unsplash.com/photo-1493239151812-ef20b4870fb5?auto=format&fit=crop&w=1600&q=80",
                        "https://images.unsplash.com/photo-1519558184554-3503f4fa0d59?auto=format&fit=crop&w=1600&q=80",
                        "https://images.unsplash.com/photo-1517411032315-54ef2cb783bb?auto=format&fit=crop&w=1600&q=80"
                    }
                },
                { "SYM", new List<string>
                    {
                        "https://images.unsplash.com/photo-1526661934280-676cef66eb02?auto=format&fit=crop&w=1600&q=80",
                        "https://images.unsplash.com/photo-1465447142348-e9952c393450?auto=format&fit=crop&w=1600&q=80",
                        "https://images.unsplash.com/photo-1449426468159-d96dbf08f19f?auto=format&fit=crop&w=1600&q=80"
                    }
                },
                { "Kymco", new List<string>
                    {
                        "https://images.unsplash.com/photo-1542362567-b07e54358753?auto=format&fit=crop&w=1600&q=80",
                        "https://images.unsplash.com/photo-1518221976525-4b41bc7af902?auto=format&fit=crop&w=1600&q=80",
                        "https://images.unsplash.com/photo-1485968579580-b6d095142e6e?auto=format&fit=crop&w=1600&q=80"
                    }
                },
                { "Ducati", new List<string>
                    {
                        "https://images.unsplash.com/photo-1533139502658-0198f920d8ae?auto=format&fit=crop&w=1600&q=80",
                        "https://images.unsplash.com/photo-1616410011234-4c7f0f2ce096?auto=format&fit=crop&w=1600&q=80",
                        "https://images.unsplash.com/photo-1525973132060-7f53f1cd1bdc?auto=format&fit=crop&w=1600&q=80"
                    }
                },
                { "BMW Motorrad", new List<string>
                    {
                        "https://images.unsplash.com/photo-1517153295259-74eb0b5de3c5?auto=format&fit=crop&w=1600&q=80",
                        "https://images.unsplash.com/photo-1544620347-c4fd4a3d5957?auto=format&fit=crop&w=1600&q=80",
                        "https://images.unsplash.com/photo-1521635464505-56c4c1b41f58?auto=format&fit=crop&w=1600&q=80"
                    }
                }
            };
        }

        private static string BuildLongDescription(Product p)
        {
            var cat = p.Category?.Name ?? "xe";
            var brand = p.Brand?.Name ?? "hãng";
            var cc = GuessDisplacement(p.Name);
            var isScooter = cat.Contains("ga", StringComparison.OrdinalIgnoreCase);

            return
$@"{p.Name} là mẫu {cat.ToLower()} của {brand}, hướng tới nhu cầu {(isScooter ? "đi phố tiện nghi, tiết kiệm nhiên liệu" : "lái hằng ngày linh hoạt cùng cảm giác điều khiển hứng khởi")}.
Thiết kế hiện đại, hoàn thiện chắc chắn, khả năng vận hành ổn định. Động cơ dung tích {cc} cho nước ga mượt, tăng tốc mạch lạc và độ bền cao—“đặc sản” của {brand}.
Trang bị tiêu biểu: hệ thống chiếu sáng LED, phanh {(isScooter ? "đĩa trước / phanh sau" : "đĩa trước/sau, ABS (tuỳ phiên bản)")}, cụm đồng hồ trực quan, cốp/ giá chở đồ hợp lý.
Vị trí ngồi thoải mái, trọng tâm cân bằng giúp kiểm soát tốt khi đi phố lẫn chạy đường trường. Bảo hành chính hãng, phụ tùng dễ kiếm, chi phí bảo dưỡng hợp lý.

Tóm lại, {p.Name} cân bằng giữa phong cách, trải nghiệm lái và chi phí sở hữu. MotorShop hỗ trợ đăng ký, giao xe toàn quốc, cùng các ưu đãi định kỳ dành riêng cho khách hàng thân thiết.";
        }

        private static string GuessDisplacement(string name)
        {
            foreach (var cc in new[] { "50", "90", "100", "110", "115", "125", "150", "155", "160", "200", "250", "300", "400", "500", "650", "700", "750", "800", "900", "1000", "1200", "1600" })
                if (name.Contains(cc)) return cc + " cc";
            return "150 cc";
        }
    }
}
