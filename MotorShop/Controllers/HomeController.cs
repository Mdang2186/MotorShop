// Controllers/HomeController.cs
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MotorShop.Data;
using MotorShop.Models;
using MotorShop.Utilities;
using MotorShop.ViewModels.Home;

namespace MotorShop.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<HomeController> _logger;
        private readonly IMemoryCache _cache;

        private const string CacheKey_Featured = "home:featured";
        private const string CacheKey_OnSaleCnt = "home:onsaleCount";

        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger, IMemoryCache cache)
        {
            _db = context;
            _logger = logger;
            _cache = cache;
        }

        // GET: /
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            try
            {
                // Featured: cache ngắn 2'
                var featured = await _cache.GetOrCreateAsync(CacheKey_Featured, async e =>
                {
                    e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2);
                    return await _db.Products
                        .Where(p => p.IsPublished)
                        .Include(p => p.Brand)
                        .OrderByDescending(p => p.CreatedAt) // hợp lý hơn Id
                        .Take(8)
                        .AsNoTracking()
                        .ToListAsync(ct);
                });

                // Categories: cache 20'
                var categories = await _cache.GetOrCreateAsync(SD.Cache_Categories, async e =>
                {
                    e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(20);
                    return await _db.Categories.OrderBy(c => c.Name).AsNoTracking().ToListAsync(ct);
                });

                // Brands: cache 20'
                var brands = await _cache.GetOrCreateAsync(SD.Cache_Brands, async e =>
                {
                    e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(20);
                    return await _db.Brands.OrderBy(b => b.Name).Take(8).AsNoTracking().ToListAsync(ct);
                });

                // OnSaleCount: cache 2'
                ViewBag.OnSaleCount = await _cache.GetOrCreateAsync(CacheKey_OnSaleCnt, async e =>
                {
                    e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2);
                    return await _db.Products
                        .Where(p => p.IsPublished && p.OriginalPrice != null && p.OriginalPrice > p.Price)
                        .CountAsync(ct);
                });

                var vm = new HomeViewModel
                {
                    FeaturedProducts = featured ?? new List<Product>(),
                    Categories = categories ?? new List<Category>(),
                    Brands = brands ?? new List<Brand>()
                };
                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Home/Index failed");
                // fallback an toàn
                return View(new HomeViewModel
                {
                    FeaturedProducts = new List<Product>(),
                    Categories = new List<Category>(),
                    Brands = new List<Brand>()
                });
            }
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // ====== Gợi ý tìm kiếm (2 alias) ======
        // 1) /Home/SearchSuggest?q=vision&take=8
        [HttpGet]
        [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "q", "take" })]
        public Task<IActionResult> SearchSuggest(string? q, int take = 8, CancellationToken ct = default)
            => SearchSuggestCore(q ?? "", take, ct);

        // 2) /products/suggest?term=vision&take=8 (khớp JS ở Home/Products)
        [HttpGet("/products/suggest")]
        [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "term", "take" })]
        public Task<IActionResult> ProductsSuggest(string? term, int take = 8, CancellationToken ct = default)
            => SearchSuggestCore(term ?? "", take, ct);

        private async Task<IActionResult> SearchSuggestCore(string term, int take, CancellationToken ct)
        {
            var q = (term ?? "").Trim();
            if (q.Length < 2) return Ok(Array.Empty<object>());
            take = Math.Clamp(take, 1, 20);

            var results = await _db.Products
                .Where(p => p.IsPublished &&
                    (EF.Functions.Like(p.Name, $"%{q}%") ||
                     (p.Brand != null && EF.Functions.Like(p.Brand.Name, $"%{q}%"))))
                .OrderBy(p => p.Name)
                .Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    price = p.Price,
                    image = p.ImageUrl,            // nhẹ, không Include gallery
                    brand = p.Brand != null ? p.Brand.Name : null
                })
                .Take(take)
                .AsNoTracking()
                .ToListAsync(ct);

            return Ok(results);
        }

        // ====== Điều hướng nhanh: /brand/5 → Products/Index?brandFilter=5 ======
        [HttpGet("brand/{id:int}")]
        public IActionResult Brand(int id)
            => RedirectToAction("Index", "Products", new { brandFilter = id });

        [HttpGet("category/{id:int}")]
        public IActionResult Category(int id)
            => RedirectToAction("Index", "Products", new { categoryFilter = id });

        // ====== Admin: làm tươi cache trang chủ ======
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RefreshHomeCache()
        {
            _cache.Remove(SD.Cache_Categories);
            _cache.Remove(SD.Cache_Brands);
            _cache.Remove(CacheKey_Featured);
            _cache.Remove(CacheKey_OnSaleCnt);
            TempData[SD.Temp_Success] = "Đã làm mới cache trang chủ.";
            return RedirectToAction(nameof(Index));
        }
    }
}
