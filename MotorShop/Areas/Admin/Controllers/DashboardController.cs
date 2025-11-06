using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MotorShop.Data;
using MotorShop.Models;
using MotorShop.Models.Enums;
using MotorShop.ViewModels;

namespace MotorShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Manager")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateTime.UtcNow.Date;
            var startOfMonth = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var vm = new DashboardViewModel
            {
                MonthlyRevenue = await _db.Orders.Where(o => o.OrderDate >= startOfMonth && o.Status != OrderStatus.Cancelled).SumAsync(o => o.TotalAmount),
                TodayRevenue = await _db.Orders.Where(o => o.OrderDate >= today && o.Status != OrderStatus.Cancelled).SumAsync(o => o.TotalAmount),
                NewOrdersToday = await _db.Orders.CountAsync(o => o.OrderDate >= today),
                PendingOrders = await _db.Orders.CountAsync(o => o.Status == OrderStatus.Pending),
                NewCustomersToday = await _userManager.Users.CountAsync(u => u.CreatedAt >= today),
                TotalProducts = await _db.Products.CountAsync()
            };

            // Biểu đồ doanh thu 7 ngày
            for (int i = 6; i >= 0; i--)
            {
                var date = today.AddDays(-i);
                var revenue = await _db.Orders.Where(o => o.OrderDate >= date && o.OrderDate < date.AddDays(1) && o.Status != OrderStatus.Cancelled).SumAsync(o => o.TotalAmount);
                vm.RevenueChartLabels.Add(date.ToString("dd/MM"));
                vm.RevenueChartData.Add(revenue / 1000000M);
            }

            // Biểu đồ trạng thái đơn hàng
            var statusCounts = await _db.Orders.GroupBy(o => o.Status).Select(g => new { Status = g.Key, Count = g.Count() }).ToListAsync();
            foreach (OrderStatus status in Enum.GetValues(typeof(OrderStatus)))
            {
                vm.OrderStatusLabels.Add(status.ToString());
                vm.OrderStatusCounts.Add(statusCounts.FirstOrDefault(x => x.Status == status)?.Count ?? 0);
            }

            vm.RecentOrders = await _db.Orders.Include(o => o.User).OrderByDescending(o => o.OrderDate).Take(5).AsNoTracking().ToListAsync();

            return View(vm);
        }
    }
}