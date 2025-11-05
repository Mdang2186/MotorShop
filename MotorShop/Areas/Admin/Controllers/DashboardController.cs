using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using MotorShop.Data;               // ✅ DÙNG using (không phải namespace ... ;)
using MotorShop.Models;
using MotorShop.Models.Enums;       // OrderStatus
using MotorShop.Utilities;
using MotorShop.ViewModels;

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace MotorShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<DashboardController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new DashboardViewModel();

            var today = DateTime.UtcNow.Date;
            var startOfCurrentMonth = new DateTime(today.Year, today.Month, 1);

            try
            {
                // --- Tổng quan ---
                viewModel.MonthlyRevenue = await _context.Orders
                    .Where(o => o.OrderDate >= startOfCurrentMonth && o.Status == OrderStatus.Delivered)
                    .SumAsync(o => o.TotalAmount);

                viewModel.TodayRevenue = await _context.Orders
                    .Where(o => o.OrderDate.Date == today && o.Status == OrderStatus.Delivered)
                    .SumAsync(o => o.TotalAmount);

                viewModel.NewOrdersToday = await _context.Orders
                    .CountAsync(o => o.OrderDate.Date == today);

                viewModel.PendingOrders = await _context.Orders
                    .CountAsync(o => o.Status == OrderStatus.Pending);

                viewModel.NewCustomersToday = await _userManager.Users
                    .CountAsync(u => u.CreatedAt.Date == today);

                viewModel.TotalProducts = await _context.Products.CountAsync();

                // --- Doanh thu 6 tháng ---
                for (int i = 5; i >= 0; i--)
                {
                    var month = DateTime.UtcNow.AddMonths(-i);
                    var monthStart = new DateTime(month.Year, month.Month, 1);
                    var monthEnd = monthStart.AddMonths(1);

                    var monthlyTotal = await _context.Orders
                        .Where(o => o.OrderDate >= monthStart
                                    && o.OrderDate < monthEnd
                                    && o.Status == OrderStatus.Delivered)
                        .SumAsync(o => o.TotalAmount);

                    viewModel.RevenueChartLabels.Add($"T{month.Month}");
                    viewModel.RevenueChartData.Add(Math.Round(monthlyTotal / 1_000_000m, 1));
                }

                // --- Thống kê trạng thái đơn ---
                var statusGroups = await _context.Orders
                    .GroupBy(o => o.Status)
                    .Select(g => new { g.Key, C = g.Count() })
                    .ToListAsync();

                foreach (var g in statusGroups)
                {
                    viewModel.OrderStatusLabels.Add(g.Key.ToString());
                    viewModel.OrderStatusCounts.Add(g.C);
                }

                // --- Gần đây ---
                viewModel.RecentOrders = await _context.Orders
                    .Include(o => o.User)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(5)
                    .AsNoTracking()
                    .ToListAsync();

                viewModel.RecentCustomers = await _userManager.Users
                    .OrderByDescending(u => u.CreatedAt)
                    .Take(5)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải dữ liệu Dashboard.");
                TempData[SD.Temp_Error] = "Không thể tải dữ liệu thống kê. Vui lòng thử lại.";
            }

            return View(viewModel);
        }
    }
}
