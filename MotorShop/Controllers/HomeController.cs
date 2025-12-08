using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MotorShop.Data;
using MotorShop.Models;
using MotorShop.Models.Enums;
using MotorShop.Utilities;
using MotorShop.ViewModels.Home;

namespace MotorShop.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<HomeController> _logger;
        private readonly IMemoryCache _cache;

        // --- KHAI BÁO KEY CACHE ---
        private const string CacheKey_Featured = "home:featured";
        private const string CacheKey_BestSeller = "home:bestseller";
        private const string CacheKey_Parts = "home:parts";
        private const string CacheKey_Brands = "home:brands";
        private const string CacheKey_Categories = "home:categories";
        private const string CacheKey_Branches = "home:branches";
        private const string CacheKey_SoldCounts = "home:soldcounts";

        // Tên danh mục phụ tùng trong DB
        private const string CategoryName_Parts = "Phụ tùng & Linh kiện";

        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger, IMemoryCache cache)
        {
            _db = context;
            _logger = logger;
            _cache = cache;
        }

        // ==========================================
        // ACTION: TRANG CHỦ
        // ==========================================
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            try
            {
                // 1. Query cơ sở: Lấy đầy đủ thông tin để hiển thị chi tiết (như trang Details)
                // [QUAN TRỌNG] Phải Include Images và Specifications
                var baseQuery = _db.Products
                    .Include(p => p.Brand)
                    .Include(p => p.Category)
                    .Include(p => p.Images)          // Lấy ảnh phụ (góc)
                    .Include(p => p.Specifications)  // Lấy thông số kỹ thuật
                    .Include(p => p.ProductTags).ThenInclude(pt => pt.Tag)
                    .Where(p => p.IsPublished && p.StockQuantity > 0)
                    .AsNoTracking();

                // --- 2. TÍNH SỐ LƯỢNG ĐÃ BÁN (REAL-TIME) ---
                var soldCounts = await _cache.GetOrCreateAsync(CacheKey_SoldCounts, async e =>
                {
                    e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10); // Cache 10 phút

                    var validStatuses = new List<OrderStatus>
                    {
                        OrderStatus.Completed,
                        OrderStatus.Delivered,
                        OrderStatus.Confirmed,
                        OrderStatus.Processing
                    };

                    return await _db.OrderItems
                        .AsNoTracking()
                        .Where(oi => validStatuses.Contains(oi.Order.Status))
                        .GroupBy(oi => oi.ProductId)
                        .Select(g => new { Id = g.Key, Count = g.Sum(x => x.Quantity) })
                        .ToDictionaryAsync(x => x.Id, x => x.Count, ct);
                });

                // Truyền dữ liệu bán hàng sang View
                ViewBag.SoldCounts = soldCounts ?? new Dictionary<int, int>();

                // --- 3. BEST SELLERS (Sản phẩm bán chạy) ---
                var bestSellers = await _cache.GetOrCreateAsync(CacheKey_BestSeller, async e =>
                {
                    e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);

                    // Lấy sản phẩm XE MÁY (trừ phụ tùng)
                    var products = await baseQuery
                        .Where(p => p.Category.Name != CategoryName_Parts)
                        .ToListAsync(ct);

                    // Sắp xếp: Ưu tiên số lượng bán giảm dần -> Giá giảm dần
                    return products
                        .OrderByDescending(p => soldCounts != null && soldCounts.ContainsKey(p.Id) ? soldCounts[p.Id] : 0)
                        .ThenByDescending(p => p.Price)
                        .Take(5) // Lấy 5 sản phẩm (1 Spotlight + 4 Mini)
                        .ToList();
                });

                // --- 4. FEATURED (Xe mới về) ---
                var featured = await _cache.GetOrCreateAsync(CacheKey_Featured, async e =>
                {
                    e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);

                    return await baseQuery
                        .Where(p => p.Category.Name != CategoryName_Parts)
                        .OrderByDescending(p => p.CreatedAt)
                        .Take(8)
                        .ToListAsync(ct);
                });

                // --- 5. LATEST PARTS (Phụ tùng mới) ---
                var parts = await _cache.GetOrCreateAsync(CacheKey_Parts, async e =>
                {
                    e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);

                    return await baseQuery
                        .Where(p => p.Category.Name == CategoryName_Parts)
                        .OrderByDescending(p => p.CreatedAt)
                        .Take(8)
                        .ToListAsync(ct);
                });

                // --- 6. CATEGORIES & BRANDS (Thương hiệu) ---
                var categories = await _cache.GetOrCreateAsync(CacheKey_Categories, async e =>
                {
                    e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
                    return await _db.Categories
                        .OrderBy(c => c.Name == CategoryName_Parts) // Đẩy phụ tùng xuống cuối
                        .ThenBy(c => c.Name)
                        .AsNoTracking()
                        .ToListAsync(ct);
                });

                var brands = await _cache.GetOrCreateAsync(CacheKey_Brands, async e =>
                {
                    e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
                    return await _db.Brands
                        .Where(b => b.IsActive) // Chỉ lấy thương hiệu đang hoạt động
                        .OrderBy(b => b.Name)
                        .AsNoTracking()
                        .ToListAsync(ct);
                });

                // --- 7. BRANCHES (Chi nhánh / Map) ---
                var branches = await _cache.GetOrCreateAsync(CacheKey_Branches, async e =>
                {
                    e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60);
                    return await _db.Branches
                        .Where(b => b.IsActive)
                        .OrderBy(b => b.Id)
                        .AsNoTracking()
                        .ToListAsync(ct);
                });

                // Đổ dữ liệu vào ViewModel
                var vm = new HomeViewModel
                {
                    BestSellers = bestSellers ?? new List<Product>(),
                    FeaturedProducts = featured ?? new List<Product>(),
                    LatestParts = parts ?? new List<Product>(),
                    Categories = categories ?? new List<Category>(),
                    Brands = brands ?? new List<Brand>(),
                    Branches = branches ?? new List<Branch>()
                };

                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi tải trang chủ (Home/Index)");
                return View(new HomeViewModel());
            }
        }

        // ==========================================
        // ACTION: GỢI Ý TÌM KIẾM (AJAX)
        // ==========================================
        [HttpGet]
        [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "q", "take" })]
        public Task<IActionResult> SearchSuggest(string? q, int take = 8, CancellationToken ct = default)
            => SearchSuggestCore(q ?? "", take, ct);

        [HttpGet("/products/suggest")]
        [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "term", "take" })]
        public Task<IActionResult> ProductsSuggest(string? term, int take = 8, CancellationToken ct = default)
            => SearchSuggestCore(term ?? "", take, ct);

        private async Task<IActionResult> SearchSuggestCore(string term, int take, CancellationToken ct)
        {
            var q = (term ?? "").Trim();
            if (q.Length < 2) return Ok(Array.Empty<object>());
            take = Math.Clamp(take, 1, 20);

            var results = await _db.Products
                .Include(p => p.Brand)
                .Where(p => p.IsPublished &&
                    (EF.Functions.Like(p.Name, $"%{q}%") ||
                     (p.Brand != null && EF.Functions.Like(p.Brand.Name, $"%{q}%"))))
                .OrderBy(p => p.Name)
                .Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    price = p.Price,
                    image = p.ImageUrl,
                    brand = p.Brand != null ? p.Brand.Name : ""
                })
                .Take(take)
                .AsNoTracking()
                .ToListAsync(ct);

            return Ok(results);
        }

        // ==========================================
        // ACTION: ĐIỀU HƯỚNG NHANH
        // ==========================================
        [HttpGet("brand/{id:int}")]
        public IActionResult Brand(int id) => RedirectToAction("Index", "Products", new { brandFilter = id });

        [HttpGet("category/{id:int}")]
        public IActionResult Category(int id) => RedirectToAction("Index", "Products", new { categoryFilter = id });

        [HttpGet("AiSupport")]
        public IActionResult AiSupport() => RedirectToAction("Index", "Ai");

        // ==========================================
        // ACTION: QUẢN TRỊ (Refresh Cache)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RefreshHomeCache()
        {
            _cache.Remove(CacheKey_Featured);
            _cache.Remove(CacheKey_BestSeller);
            _cache.Remove(CacheKey_Parts);
            _cache.Remove(CacheKey_Brands);
            _cache.Remove(CacheKey_Categories);
            _cache.Remove(CacheKey_Branches);
            _cache.Remove(CacheKey_SoldCounts);
            _cache.Remove(SD.Cache_Brands);
            _cache.Remove(SD.Cache_Categories);

            TempData[SD.Temp_Success] = "Đã làm mới bộ nhớ đệm (Cache) trang chủ.";
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}