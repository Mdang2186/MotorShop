using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MotorShop.Data;
using MotorShop.Data.Seeders;
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

        public async Task InitializeAsync()
        {
            // 1. Auto Migrate
            try
            {
                if ((await _context.Database.GetPendingMigrationsAsync()).Any())
                    await _context.Database.MigrateAsync();
            }
            catch (Exception ex) { Console.WriteLine($"Migrate Error: {ex.Message}"); }

            // 2. Gọi các Seeder theo thứ tự
            await IdentitySeeder.SeedAsync(_roleManager, _userManager);
            await MasterDataSeeder.SeedAsync(_context);
            await ProductSeeder.SeedAsync(_context); // Dữ liệu sản phẩm THẬT
            await OrderSeeder.SeedAsync(_context, _userManager); // Đơn hàng mẫu từ sản phẩm thật
        }
    }
}