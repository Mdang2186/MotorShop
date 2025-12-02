// File: Utilities/DbInitializer.cs
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
            // 1. Auto Migrate (chỉ chạy khi còn migration pending)
            try
            {
                if ((await _context.Database.GetPendingMigrationsAsync()).Any())
                {
                    await _context.Database.MigrateAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Migrate Error: {ex.Message}");
            }

            // 2. Dọn dữ liệu chat "mồ côi" nếu có
            await CleanupChatAsync();

            // 3. Seed dữ liệu chuẩn cho hệ thống

            // 3.1. Tài khoản & vai trò (Admin, User, khách hàng mẫu)
            await IdentitySeeder.SeedAsync(_roleManager, _userManager);

            // 3.2. Dữ liệu gốc: Chi nhánh, Thương hiệu, Danh mục, Ngân hàng...
            await MasterDataSeeder.SeedAsync(_context);

            // 3.3. Sản phẩm + hình ảnh
            await ProductSeeder.SeedAsync(_context);

            // 3.4. Tag + ProductTag cho AI (rất quan trọng cho gợi ý)
            await TagSeeder.SeedAsync(_context);

            // 3.5. Đơn hàng mẫu từ sản phẩm thật (cho báo cáo + ML.NET sau này)
            await OrderSeeder.SeedAsync(_context, _userManager);
        }

        /// <summary>
        /// Xóa các ChatThread/ChatMessage trỏ tới user không còn tồn tại
        /// (trường hợp bạn đổi DB / drop DB rồi seed lại).
        /// </summary>
        private async Task CleanupChatAsync()
        {
            // Tìm các thread có CustomerId không còn trong AspNetUsers
            var orphanThreads = await _context.ChatThreads
                .Where(t => !_context.Users.Any(u => u.Id == t.CustomerId))
                .ToListAsync();

            if (!orphanThreads.Any())
                return;

            var orphanThreadIds = orphanThreads.Select(t => t.Id).ToList();

            var orphanMessages = await _context.ChatMessages
                .Where(m => orphanThreadIds.Contains(m.ThreadId))
                .ToListAsync();

            _context.ChatMessages.RemoveRange(orphanMessages);
            _context.ChatThreads.RemoveRange(orphanThreads);

            await _context.SaveChangesAsync();
        }
    }
}
