using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MotorShop.Data;
using MotorShop.Models;
using System.Diagnostics;

namespace MotorShop.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Action này xử lý trang chủ
        public async Task<IActionResult> Index()
        {
            // Lấy 8 sản phẩm mới nhất để hiển thị
            var featuredProducts = await _context.Products
                                                .Include(p => p.Brand)
                                                .OrderByDescending(p => p.Id)
                                                .Take(8)
                                                .ToListAsync();
            return View(featuredProducts);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            // Cần tạo một ErrorViewModel trong thư mục Models/ViewModels
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}