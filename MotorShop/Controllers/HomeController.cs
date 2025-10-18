using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MotorShop.Data;
using MotorShop.Models;
using MotorShop.ViewModels; // <-- THÊM DÒNG NÀY
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

        // Action này đã được cập nhật để gửi HomeViewModel
        public async Task<IActionResult> Index()
        {
            // Tạo một đối tượng ViewModel để chứa tất cả dữ liệu
            var viewModel = new HomeViewModel
            {
                // Lấy 8 sản phẩm mới nhất làm sản phẩm nổi bật
                FeaturedProducts = await _context.Products
                    .Include(p => p.Brand)
                    .OrderByDescending(p => p.Id)
                    .Take(8)
                    .AsNoTracking()
                    .ToListAsync(),

                // Lấy tất cả danh sách loại xe
                Categories = await _context.Categories
                    .AsNoTracking()
                    .ToListAsync(),

                // Lấy 8 thương hiệu để hiển thị logo
                Brands = await _context.Brands
                    .Take(8)
                    .AsNoTracking()
                    .ToListAsync()
            };

            // Gửi viewModel chứa đầy đủ dữ liệu đến View
            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}