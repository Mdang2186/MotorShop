using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Logging;
using MotorShop.Data;
using MotorShop.Models;
using MotorShop.Utilities; // Đảm bảo bạn đã có using này

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

        public async Task Initialize()
        {
            // Apply pending migrations
            try
            {
                if (_context.Database.GetPendingMigrations().Any())
                {
                    _context.Database.Migrate();
                }
            }
            catch (Exception) { /* Handle error */ }

            // Create Roles if they don't exist
            if (!await _roleManager.RoleExistsAsync("Admin")) await _roleManager.CreateAsync(new IdentityRole("Admin"));
            if (!await _roleManager.RoleExistsAsync("User")) await _roleManager.CreateAsync(new IdentityRole("User"));

            // Create default Admin User
            var adminEmail = "admin@motorshop.vn";
            if (await _userManager.FindByEmailAsync(adminEmail) == null)
            {
                var adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "Administrator",
                    EmailConfirmed = true
                };
                await _userManager.CreateAsync(adminUser, "Admin@123");
                await _userManager.AddToRoleAsync(adminUser, SD.Role_Admin);
            }

            // ✨ SEED PRODUCT DATA ✨
            // Check if there is already any product data
            if (await _context.Products.AnyAsync())
            {
                return; // CSDL đã có dữ liệu, không cần khởi tạo
            }
            // 5. Seed Brands & Categories
            // SỬA LỖI Ở ĐÂY: Thêm 'Slug' cho Brand, sử dụng SlugHelper để tự động hóa
            var honda = new Brand { Name = "Honda"};
            var yamaha = new Brand { Name = "Yamaha"};
            var suzuki = new Brand { Name = "Suzuki" };
            var piaggio = new Brand { Name = "Piaggio"};
            var sym = new Brand { Name = "SYM" };
            var kymco = new Brand { Name = "Kymco" };
            var ducati = new Brand { Name = "Ducati"};
            var bmw = new Brand { Name = "BMW Motorrad" };
            _context.Brands.AddRange(honda, yamaha, suzuki, piaggio, sym, kymco, ducati, bmw);
            await _context.SaveChangesAsync();

            var tayGa = new Category { Name = "Xe tay ga"};
            var xeSo = new Category { Name = "Xe số"};
            var sportbike = new Category { Name = "Sportbike" };
            var naked = new Category { Name = "Naked bike" };
            var cruiser = new Category { Name = "Cruiser"};
            var adventure = new Category { Name = "Adventure" };
            var classic = new Category { Name = "Classic"};
            _context.Categories.AddRange(tayGa, xeSo, sportbike, naked, cruiser, adventure, classic);
            await _context.SaveChangesAsync();
            var products = new Product[]
{
    // --- HONDA (30 products) ---
    // Tay ga
    new Product { Name = "Honda Vision 2024 - Cao cấp", Price = 32000000, StockQuantity = 50, Year = 2024, BrandId = honda.Id, CategoryId = tayGa.Id, ImageUrl="/images/products/honda/vision-2024-cao-cap.png" },
    new Product { Name = "Honda Air Blade 160 - Đặc biệt", Price = 58000000, OriginalPrice = 60000000, StockQuantity = 30, Year = 2024, BrandId = honda.Id, CategoryId = tayGa.Id, ImageUrl="/images/products/honda/air-blade-160-dac-biet.png" },
    new Product { Name = "Honda SH 125i - Thể thao", Price = 81000000, StockQuantity = 25, Year = 2023, BrandId = honda.Id, CategoryId = tayGa.Id, ImageUrl="/images/products/honda/sh-125i-the-thao.png" },
    new Product { Name = "Honda SH 160i - Đặc biệt", Price = 101000000, StockQuantity = 15, Year = 2024, BrandId = honda.Id, CategoryId = tayGa.Id, ImageUrl="/images/products/honda/sh-160i-dac-biet.png" },
    new Product { Name = "Honda SH Mode - Thể thao", Price = 64000000, StockQuantity = 40, Year = 2023, BrandId = honda.Id, CategoryId = tayGa.Id, ImageUrl="/images/products/honda/sh-mode-the-thao.png" },
    new Product { Name = "Honda Lead 125 - Đặc biệt", Price = 42500000, StockQuantity = 60, Year = 2022, BrandId = honda.Id, CategoryId = tayGa.Id, ImageUrl="/images/products/honda/lead-125-dac-biet.png" },
    new Product { Name = "Honda Vario 160 - Thể thao", Price = 55000000, StockQuantity = 28, Year = 2024, BrandId = honda.Id, CategoryId = tayGa.Id, ImageUrl="/images/products/honda/vario-160-the-thao.png" },
    new Product { Name = "Honda PCX 160", Price = 80000000, StockQuantity = 12, Year = 2023, BrandId = honda.Id, CategoryId = tayGa.Id, ImageUrl="/images/products/honda/pcx-160.png" },
    // Xe số
    new Product { Name = "Honda Wave Alpha 110 - Đặc biệt", Price = 18500000, StockQuantity = 100, Year = 2024, BrandId = honda.Id, CategoryId = xeSo.Id, ImageUrl="/images/products/honda/wave-alpha-110-dac-biet.png" },
    new Product { Name = "Honda Blade 110 - Thể thao", Price = 21000000, StockQuantity = 70, Year = 2023, BrandId = honda.Id, CategoryId = xeSo.Id, ImageUrl="/images/products/honda/blade-110-the-thao.png" },
    new Product { Name = "Honda Future 125 FI - Đặc biệt", Price = 32000000, StockQuantity = 55, Year = 2024, BrandId = honda.Id, CategoryId = xeSo.Id, ImageUrl="/images/products/honda/future-125-fi-dac-biet.png" },
    new Product { Name = "Honda Wave RSX FI 110", Price = 22500000, StockQuantity = 80, Year = 2023, BrandId = honda.Id, CategoryId = xeSo.Id, ImageUrl="/images/products/honda/wave-rsx-fi-110.png" },
    // Classic
    new Product { Name = "Honda Super Cub C125", Price = 86000000, StockQuantity = 10, Year = 2022, BrandId = honda.Id, CategoryId = classic.Id, ImageUrl="/images/products/honda/super-cub-c125.png" },
    new Product { Name = "Honda Monkey", Price = 85000000, StockQuantity = 7, Year = 2023, BrandId = honda.Id, CategoryId = classic.Id, ImageUrl="/images/products/honda/monkey.png" },
    // Sportbike
    new Product { Name = "Honda CBR150R - Thể thao", Price = 72000000, StockQuantity = 20, Year = 2023, BrandId = honda.Id, CategoryId = sportbike.Id, ImageUrl="/images/products/honda/cbr150r-the-thao.png" },
    new Product { Name = "Honda CBR250RR", Price = 150000000, StockQuantity = 8, Year = 2022, BrandId = honda.Id, CategoryId = sportbike.Id, ImageUrl="/images/products/honda/cbr250rr.png" },
    // Naked bike
    new Product { Name = "Honda Winner X 150 - Đặc biệt", Price = 50000000, StockQuantity = 45, Year = 2024, BrandId = honda.Id, CategoryId = naked.Id, ImageUrl="/images/products/honda/winner-x-150-dac-biet.png" },
    new Product { Name = "Honda CB150R The Streetster", Price = 105000000, StockQuantity = 8, Year = 2021, BrandId = honda.Id, CategoryId = naked.Id, ImageUrl="/images/products/honda/cb150r-the-streetster.png" },
    new Product { Name = "Honda CB300R", Price = 140000000, StockQuantity = 6, Year = 2023, BrandId = honda.Id, CategoryId = naked.Id, ImageUrl="/images/products/honda/cb300r.png" },
    new Product { Name = "Honda MSX 125", Price = 50000000, StockQuantity = 14, Year = 2022, BrandId = honda.Id, CategoryId = naked.Id, ImageUrl="/images/products/honda/msx-125.png" },
    // Cruiser
    new Product { Name = "Honda Rebel 300", Price = 125000000, StockQuantity = 5, Year = 2022, BrandId = honda.Id, CategoryId = cruiser.Id, ImageUrl="/images/products/honda/rebel-300.png" },
    new Product { Name = "Honda Rebel 500", Price = 180000000, StockQuantity = 9, Year = 2024, BrandId = honda.Id, CategoryId = cruiser.Id, ImageUrl="/images/products/honda/rebel-500.png" },
    new Product { Name = "Honda Shadow Phantom 750", Price = 250000000, StockQuantity = 3, Year = 2021, BrandId = honda.Id, CategoryId = cruiser.Id, ImageUrl="/images/products/honda/shadow-phantom-750.png" },
    // Adventure
    new Product { Name = "Honda CB500X", Price = 194000000, StockQuantity = 7, Year = 2023, BrandId = honda.Id, CategoryId = adventure.Id, ImageUrl="/images/products/honda/cb500x.png" },
    new Product { Name = "Honda Africa Twin", Price = 590000000, StockQuantity = 4, Year = 2024, BrandId = honda.Id, CategoryId = adventure.Id, ImageUrl="/images/products/honda/africa-twin.png" },
    new Product { Name = "Honda Transalp", Price = 309000000, StockQuantity = 6, Year = 2024, BrandId = honda.Id, CategoryId = adventure.Id, ImageUrl="/images/products/honda/transalp.png" },
    new Product { Name = "Honda CRF300L", Price = 130000000, StockQuantity = 11, Year = 2023, BrandId = honda.Id, CategoryId = adventure.Id, ImageUrl="/images/products/honda/crf300l.png" },
    new Product { Name = "Honda Gold Wing", Price = 1230000000, StockQuantity = 2, Year = 2023, BrandId = honda.Id, CategoryId = cruiser.Id, ImageUrl="/images/products/honda/gold-wing.png" },
    new Product { Name = "Honda ADV 160", Price = 95000000, StockQuantity = 18, Year = 2024, BrandId = honda.Id, CategoryId = adventure.Id, ImageUrl="/images/products/honda/adv-160.png" },

    // --- YAMAHA (20 products) ---
    new Product { Name = "Yamaha Janus - Giới hạn", Price = 29000000, OriginalPrice = 32000000, StockQuantity = 55, Year = 2024, BrandId = yamaha.Id, CategoryId = tayGa.Id, ImageUrl="/images/products/yamaha/janus-gioi-han.png" },
    new Product { Name = "Yamaha Grande - Giới hạn", Price = 51000000, StockQuantity = 35, Year = 2023, BrandId = yamaha.Id, CategoryId = tayGa.Id, ImageUrl="/images/products/yamaha/grande-gioi-han.png" },
    new Product { Name = "Yamaha Latte", Price = 38000000, StockQuantity = 48, Year = 2022, BrandId = yamaha.Id, CategoryId = tayGa.Id, ImageUrl="/images/products/yamaha/latte.png" },
    new Product { Name = "Yamaha FreeGo - Đặc biệt", Price = 34000000, StockQuantity = 33, Year = 2023, BrandId = yamaha.Id, CategoryId = tayGa.Id, ImageUrl="/images/products/yamaha/freego-dac-biet.png" },
    new Product { Name = "Yamaha NVX 155 VVA", Price = 55000000, StockQuantity = 22, Year = 2024, BrandId = yamaha.Id, CategoryId = tayGa.Id, ImageUrl="/images/products/yamaha/nvx-155-vva.png" },
    new Product { Name = "Yamaha Sirius FI", Price = 21500000, StockQuantity = 90, Year = 2024, BrandId = yamaha.Id, CategoryId = xeSo.Id, ImageUrl="/images/products/yamaha/sirius-fi.png" },
    new Product { Name = "Yamaha Jupiter Finn", Price = 28000000, StockQuantity = 65, Year = 2023, BrandId = yamaha.Id, CategoryId = xeSo.Id, ImageUrl="/images/products/yamaha/jupiter-finn.png" },
    new Product { Name = "Yamaha Jupiter FI", Price = 30000000, StockQuantity = 60, Year = 2022, BrandId = yamaha.Id, CategoryId = xeSo.Id, ImageUrl="/images/products/yamaha/jupiter-fi.png" },
    new Product { Name = "Yamaha Exciter 155 VVA - GP", Price = 51000000, OriginalPrice = 52000000, StockQuantity = 40, Year = 2024, BrandId = yamaha.Id, CategoryId = naked.Id, ImageUrl="/images/products/yamaha/exciter-155-vva-gp.png" },
    new Product { Name = "Yamaha MT-15", Price = 69000000, StockQuantity = 24, Year = 2022, BrandId = yamaha.Id, CategoryId = naked.Id, ImageUrl="/images/products/yamaha/mt-15.png" },
    new Product { Name = "Yamaha MT-03", Price = 129000000, StockQuantity = 7, Year = 2022, BrandId = yamaha.Id, CategoryId = naked.Id, ImageUrl="/images/products/yamaha/mt-03.png" },
    new Product { Name = "Yamaha MT-09", Price = 345000000, StockQuantity = 5, Year = 2023, BrandId = yamaha.Id, CategoryId = naked.Id, ImageUrl="/images/products/yamaha/mt-09.png" },
    new Product { Name = "Yamaha YZF-R15", Price = 78000000, StockQuantity = 18, Year = 2023, BrandId = yamaha.Id, CategoryId = sportbike.Id, ImageUrl="/images/products/yamaha/yzf-r15.png" },
    new Product { Name = "Yamaha YZF-R3", Price = 132000000, StockQuantity = 9, Year = 2021, BrandId = yamaha.Id, CategoryId = sportbike.Id, ImageUrl="/images/products/yamaha/yzf-r3.png" },
    new Product { Name = "Yamaha YZF-R7", Price = 269000000, StockQuantity = 6, Year = 2024, BrandId = yamaha.Id, CategoryId = sportbike.Id, ImageUrl="/images/products/yamaha/yzf-r7.png" },
    new Product { Name = "Yamaha XS155R", Price = 77000000, StockQuantity = 14, Year = 2023, BrandId = yamaha.Id, CategoryId = classic.Id, ImageUrl="/images/products/yamaha/xs155r.png" },
    new Product { Name = "Yamaha XSR900", Price = 359000000, StockQuantity = 4, Year = 2023, BrandId = yamaha.Id, CategoryId = classic.Id, ImageUrl="/images/products/yamaha/xsr900.png" },
    new Product { Name = "Yamaha PG-1", Price = 31000000, StockQuantity = 30, Year = 2024, BrandId = yamaha.Id, CategoryId = adventure.Id, ImageUrl="/images/products/yamaha/pg-1.png" },
    new Product { Name = "Yamaha Tenere 700", Price = 399000000, StockQuantity = 3, Year = 2023, BrandId = yamaha.Id, CategoryId = adventure.Id, ImageUrl="/images/products/yamaha/tenere-700.png" },
    new Product { Name = "Yamaha Tracer 9 GT", Price = 369000000, StockQuantity = 5, Year = 2024, BrandId = yamaha.Id, CategoryId = adventure.Id, ImageUrl="/images/products/yamaha/tracer-9-gt.png" },

    // --- SUZUKI (12 products) ---
    new Product { Name = "Suzuki Address 110 FI", Price = 29000000, StockQuantity = 25, Year = 2022, BrandId = suzuki.Id, CategoryId = tayGa.Id, ImageUrl="/images/products/suzuki/address-110-fi.png" },
    new Product { Name = "Suzuki Burgman Street 125", Price = 49000000, StockQuantity = 15, Year = 2023, BrandId = suzuki.Id, CategoryId = tayGa.Id, ImageUrl="/images/products/suzuki/burgman-street-125.png" },
    new Product { Name = "Suzuki Viva 115 FI", Price = 23000000, StockQuantity = 30, Year = 2020, BrandId = suzuki.Id, CategoryId = xeSo.Id, ImageUrl="/images/products/suzuki/viva-115-fi.png" },
    new Product { Name = "Suzuki Axelo 125", Price = 28000000, StockQuantity = 18, Year = 2019, BrandId = suzuki.Id, CategoryId = xeSo.Id, ImageUrl="/images/products/suzuki/axelo-125.png" },
    new Product { Name = "Suzuki Raider R150 FI", Price = 51000000, OriginalPrice = 51500000, StockQuantity = 28, Year = 2024, BrandId = suzuki.Id, CategoryId = naked.Id, ImageUrl="/images/products/suzuki/raider-r150-fi.png" },
    new Product { Name = "Suzuki Satria F150", Price = 53000000, StockQuantity = 26, Year = 2023, BrandId = suzuki.Id, CategoryId = naked.Id, ImageUrl="/images/products/suzuki/satria-f150.png" },
    new Product { Name = "Suzuki GSX-S150", Price = 69000000, StockQuantity = 13, Year = 2022, BrandId = suzuki.Id, CategoryId = naked.Id, ImageUrl="/images/products/suzuki/gsx-s150.png" },
    new Product { Name = "Suzuki GSX-R150", Price = 72000000, StockQuantity = 11, Year = 2022, BrandId = suzuki.Id, CategoryId = sportbike.Id, ImageUrl="/images/products/suzuki/gsx-r150.png" },
    new Product { Name = "Suzuki Gixxer SF250", Price = 125000000, StockQuantity = 8, Year = 2023, BrandId = suzuki.Id, CategoryId = sportbike.Id, ImageUrl="/images/products/suzuki/gixxer-sf250.png" },
    new Product { Name = "Suzuki GZ150-A", Price = 64000000, StockQuantity = 9, Year = 2021, BrandId = suzuki.Id, CategoryId = cruiser.Id, ImageUrl="/images/products/suzuki/gz150-a.png" },
    new Product { Name = "Suzuki Intruder 150", Price = 89000000, StockQuantity = 6, Year = 2022, BrandId = suzuki.Id, CategoryId = cruiser.Id, ImageUrl="/images/products/suzuki/intruder-150.png" },
    new Product { Name = "Suzuki V-Strom 250SX", Price = 135000000, StockQuantity = 7, Year = 2024, BrandId = suzuki.Id, CategoryId = adventure.Id, ImageUrl="/images/products/suzuki/v-strom-250sx.png" },

    // --- PIAGGIO (8 products) ---
    new Product { Name = "Piaggio Vespa Sprint S 125", Price = 81000000, StockQuantity = 15, Year = 2024, BrandId = piaggio.Id, CategoryId = tayGa.Id, ImageUrl="/images/products/piaggio/vespa-sprint-s-125.png" },
    new Product { Name = "Piaggio Vespa Primavera S 125", Price = 78000000, StockQuantity = 18, Year = 2023, BrandId = piaggio.Id, CategoryId = tayGa.Id, ImageUrl="/images/products/piaggio/vespa-primavera-s-125.png" },
    new Product { Name = "Piaggio Liberty S 125", Price = 60000000, StockQuantity = 22, Year = 2024, BrandId = piaggio.Id, CategoryId = tayGa.Id, ImageUrl="/images/products/piaggio/liberty-s-125.png" },
    new Product { Name = "Piaggio Medley S 150", Price = 97000000, StockQuantity = 12, Year = 2023, BrandId = piaggio.Id, CategoryId = tayGa.Id, ImageUrl="/images/products/piaggio/medley-s-150.png" },
    new Product { Name = "Piaggio Vespa GTS Super Sport 150", Price = 116000000, StockQuantity = 10, Year = 2022, BrandId = piaggio.Id, CategoryId = tayGa.Id, ImageUrl="/images/products/piaggio/vespa-gts-super-sport-150.png" },
    new Product { Name = "Piaggio Zip 100", Price = 36000000, StockQuantity = 25, Year = 2021, BrandId = piaggio.Id, CategoryId = tayGa.Id, ImageUrl="/images/products/piaggio/zip-100.png" },
    new Product { Name = "Piaggio Beverly S 400", Price = 235000000, StockQuantity = 5, Year = 2024, BrandId = piaggio.Id, CategoryId = tayGa.Id, ImageUrl="/images/products/piaggio/beverly-s-400.png" },
    new Product { Name = "Piaggio Vespa 946", Price = 405000000, StockQuantity = 3, Year = 2023, BrandId = piaggio.Id, CategoryId = tayGa.Id, ImageUrl="/images/products/piaggio/vespa-946.png" },

    // --- SYM & KYMCO (8 products) ---
    new Product { Name = "SYM Attila 125", Price = 34000000, StockQuantity = 30, Year = 2022, BrandId = sym.Id, CategoryId = tayGa.Id, ImageUrl="/images/products/sym/attila-125.png" },
    new Product { Name = "SYM Passing 50", Price = 25000000, StockQuantity = 40, Year = 2023, BrandId = sym.Id, CategoryId = tayGa.Id, ImageUrl="/images/products/sym/passing-50.png" },
    new Product { Name = "SYM Shark 125", Price = 48000000, StockQuantity = 15, Year = 2024, BrandId = sym.Id, CategoryId = tayGa.Id, ImageUrl="/images/products/sym/shark-125.png" },
    new Product { Name = "SYM Elite 50", Price = 22000000, StockQuantity = 35, Year = 2022, BrandId = sym.Id, CategoryId = tayGa.Id, ImageUrl="/images/products/sym/elite-50.png" },
    new Product { Name = "Kymco Like 125", Price = 36000000, StockQuantity = 28, Year = 2023, BrandId = kymco.Id, CategoryId = tayGa.Id, ImageUrl="/images/products/kymco/like-125.png" },
    new Product { Name = "Kymco Candy Hermosa 50", Price = 26000000, StockQuantity = 45, Year = 2024, BrandId = kymco.Id, CategoryId = tayGa.Id, ImageUrl="/images/products/kymco/candy-hermosa-50.png" },
    new Product { Name = "SYM Elegant 110", Price = 17000000, StockQuantity = 50, Year = 2023, BrandId = sym.Id, CategoryId = xeSo.Id, ImageUrl="/images/products/sym/elegant-110.png" },
    new Product { Name = "Kymco K-Pipe 125", Price = 37000000, StockQuantity = 20, Year = 2022, BrandId = kymco.Id, CategoryId = naked.Id, ImageUrl="/images/products/kymco/k-pipe-125.png" },

    // --- DUCATI (6 products) ---
    new Product { Name = "Ducati Monster 937", Price = 440000000, StockQuantity = 6, Year = 2023, BrandId = ducati.Id, CategoryId = naked.Id, ImageUrl="/images/products/ducati/monster-937.png" },
    new Product { Name = "Ducati Streetfighter V4 S", Price = 790000000, StockQuantity = 4, Year = 2024, BrandId = ducati.Id, CategoryId = naked.Id, ImageUrl="/images/products/ducati/streetfighter-v4-s.png" },
    new Product { Name = "Ducati Panigale V4 S", Price = 940000000, StockQuantity = 3, Year = 2023, BrandId = ducati.Id, CategoryId = sportbike.Id, ImageUrl="/images/products/ducati/panigale-v4-s.png" },
    new Product { Name = "Ducati SuperSport 950 S", Price = 580000000, StockQuantity = 5, Year = 2024, BrandId = ducati.Id, CategoryId = sportbike.Id, ImageUrl="/images/products/ducati/supersport-950-s.png" },
    new Product { Name = "Ducati Multistrada V4 S", Price = 800000000, StockQuantity = 4, Year = 2023, BrandId = ducati.Id, CategoryId = adventure.Id, ImageUrl="/images/products/ducati/multistrada-v4-s.png" },
    new Product { Name = "Ducati Scrambler Icon", Price = 330000000, StockQuantity = 7, Year = 2024, BrandId = ducati.Id, CategoryId = classic.Id, ImageUrl="/images/products/ducati/scrambler-icon.png" },

    // --- BMW MOTORRAD (6 products) ---
    new Product { Name = "BMW G 310 R", Price = 189000000, StockQuantity = 8, Year = 2023, BrandId = bmw.Id, CategoryId = naked.Id, ImageUrl="/images/products/bmw/g-310-r.png" },
    new Product { Name = "BMW S 1000 R", Price = 670000000, StockQuantity = 4, Year = 2024, BrandId = bmw.Id, CategoryId = naked.Id, ImageUrl="/images/products/bmw/s-1000-r.png" },
    new Product { Name = "BMW S 1000 RR", Price = 950000000, StockQuantity = 3, Year = 2023, BrandId = bmw.Id, CategoryId = sportbike.Id, ImageUrl="/images/products/bmw/s-1000-rr.png" },
    new Product { Name = "BMW G 310 GS", Price = 219000000, StockQuantity = 9, Year = 2023, BrandId = bmw.Id, CategoryId = adventure.Id, ImageUrl="/images/products/bmw/g-310-gs.png" },
    new Product { Name = "BMW R 1250 GS Adventure", Price = 720000000, StockQuantity = 5, Year = 2024, BrandId = bmw.Id, CategoryId = adventure.Id, ImageUrl="/images/products/bmw/r-1250-gs-adventure.png" },
    new Product { Name = "BMW R nineT Scrambler", Price = 540000000, StockQuantity = 6, Year = 2023, BrandId = bmw.Id, CategoryId = classic.Id, ImageUrl="/images/products/bmw/r-ninet-scrambler.png" }
};
            _context.Products.AddRange(products);
            await _context.SaveChangesAsync();
        }
    }
}