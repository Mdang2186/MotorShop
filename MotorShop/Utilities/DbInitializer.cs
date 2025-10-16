using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MotorShop.Data;
using MotorShop.Models;

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
                var adminUser = new ApplicationUser { UserName = adminEmail, Email = adminEmail, FullName = "Administrator", EmailConfirmed = true };
                await _userManager.CreateAsync(adminUser, "Admin@123");
                await _userManager.AddToRoleAsync(adminUser, "Admin");
            }

            // ✨ SEED PRODUCT DATA ✨
            // Check if there is already any product data
            if (_context.Products.Any())
            {
                return; // DB has been seeded
            }

            // Seed Brands
            var honda = new Brand { Name = "Honda" };
            var yamaha = new Brand { Name = "Yamaha" };
            var suzuki = new Brand { Name = "Suzuki" };
            _context.Brands.AddRange(honda, yamaha, suzuki);
            await _context.SaveChangesAsync();

            // Seed Categories
            var tayGa = new Category { Name = "Xe tay ga" };
            var xeSo = new Category { Name = "Xe số" };
            var theThao = new Category { Name = "Xe thể thao" };
            _context.Categories.AddRange(tayGa, xeSo, theThao);
            await _context.SaveChangesAsync();

            // Seed Products
            var products = new Product[]
            {
                new Product { Name = "Honda Vision 2024", Price = 32000000, StockQuantity = 50, Year = 2024, Brand = honda, Category = tayGa, ImageUrl="https://images.unsplash.com/photo-1571068316344-75bc76f77890?w=400&h=300&fit=crop" },
                new Product { Name = "Honda Air Blade 160", Price = 58000000, OriginalPrice = 60000000, StockQuantity = 30, Year = 2024, Brand = honda, Category = tayGa, ImageUrl="https://images.unsplash.com/photo-1558618047-3c8c76ca7d13?w=400&h=300&fit=crop" },
                new Product { Name = "Honda Wave Alpha 110", Price = 18500000, StockQuantity = 100, Year = 2024, Brand = honda, Category = xeSo, ImageUrl="https://images.unsplash.com/photo-1609630875171-b1321377ee65?w=400&h=300&fit=crop" },
                new Product { Name = "Yamaha Exciter 155", Price = 48000000, StockQuantity = 40, Year = 2024, Brand = yamaha, Category = theThao, ImageUrl="https://images.unsplash.com/photo-1568772585407-9361f9bf3a87?w=400&h=300&fit=crop" },
                new Product { Name = "Yamaha Janus", Price = 29000000, StockQuantity = 55, Year = 2024, Brand = yamaha, Category = tayGa, ImageUrl="https://images.unsplash.com/photo-1558618666-fcd25c85cd64?w=400&h=300&fit=crop" },
                new Product { Name = "Suzuki Raider R150", Price = 51000000, StockQuantity = 28, Year = 2024, Brand = suzuki, Category = theThao, ImageUrl="https://images.unsplash.com/photo-1571068316344-75bc76f77890?w=400&h=300&fit=crop" }
            };

            _context.Products.AddRange(products);
            await _context.SaveChangesAsync();
        }
    }
}