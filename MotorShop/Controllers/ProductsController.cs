// File: Controllers/ProductsController.cs
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MotorShop.Data;
using MotorShop.Models;
using MotorShop.Services;
using MotorShop.Utilities;
using MotorShop.ViewModels;

namespace MotorShop.Controllers
{
    [AutoValidateAntiforgeryToken]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IMemoryCache _cache;
        private readonly CartService _cart;
        private readonly ILogger<ProductsController> _logger;

        private const string RecentlyViewedCookie = "rv";
        private const int RecentlyViewedMax = 10;

        public ProductsController(
            ApplicationDbContext db,
            IMemoryCache cache,
            CartService cart,
            ILogger<ProductsController> logger)
        {
            _db = db;
            _cache = cache;
            _cart = cart;
            _logger = logger;
        }

        // =========================================================
        // GET: /Products
        // Hỗ trợ cả 'q' (từ navbar) và 'searchString' (form)
        // Lọc brand/category/năm/giá/tồn kho, sắp xếp, phân trang.
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Index(
            [FromQuery(Name = "q")] string? q,
            string? searchString,
            int? brandFilter,
            int? categoryFilter,
            string? sortBy,
            decimal? minPrice,
            decimal? maxPrice,
            int? year,
            bool inStockOnly = false,
            int page = 1,
            int pageSize = SD.DefaultPageSize,
            CancellationToken ct = default)
        {
            pageSize = SD.PageSizeOptions.Contains(pageSize) ? pageSize : SD.DefaultPageSize;

            // Gom keyword
            var keyword = (string.IsNullOrWhiteSpace(searchString) ? q : searchString)?.Trim();

            // Base query (listing chỉ cần Brand; Category chỉ để lọc)
            IQueryable<Product> productsQuery = _db.Products
                .AsNoTracking()
                .Include(p => p.Brand)
                .Where(p => p.IsPublished);

            // 1) Từ khoá trên tên/brand
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var like = $"%{keyword}%";
                productsQuery = productsQuery.Where(p =>
                    EF.Functions.Like(p.Name, like) ||
                    (p.Brand != null && EF.Functions.Like(p.Brand.Name, like)));
            }

            // 2) Lọc Brand/Category
            if (brandFilter.HasValue) productsQuery = productsQuery.Where(p => p.BrandId == brandFilter.Value);
            if (categoryFilter.HasValue) productsQuery = productsQuery.Where(p => p.CategoryId == categoryFilter.Value);

            // 3) Lọc năm
            if (year.HasValue) productsQuery = productsQuery.Where(p => p.Year == year.Value);

            // 4) Lọc giá
            if (minPrice.HasValue) productsQuery = productsQuery.Where(p => p.Price >= minPrice.Value);
            if (maxPrice.HasValue) productsQuery = productsQuery.Where(p => p.Price <= maxPrice.Value);

            // 5) Chỉ hàng còn tồn
            if (inStockOnly) productsQuery = productsQuery.Where(p => p.StockQuantity > 0);

            // 6) Sort
            productsQuery = sortBy switch
            {
                "price-low" => productsQuery.OrderBy(p => p.Price).ThenByDescending(p => p.CreatedAt),
                "price-high" => productsQuery.OrderByDescending(p => p.Price).ThenByDescending(p => p.CreatedAt),
                "name-asc" => productsQuery.OrderBy(p => p.Name),
                "name-desc" => productsQuery.OrderByDescending(p => p.Name),
                _ => productsQuery.OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id) // mặc định mới nhất
            };

            // 7) Phân trang
            var totalItems = await productsQuery.CountAsync(ct);
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            page = Math.Max(1, page);
            if (totalPages > 0 && page > totalPages) page = totalPages; // kẹp trang vượt trần

            var products = await productsQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            // 8) Brands/Categories (cache)
            var brands = await _cache.GetOrCreateAsync(SD.Cache_Brands, async e =>
            {
                e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                return await _db.Brands.AsNoTracking().OrderBy(b => b.Name).ToListAsync(ct);
            });
            var categories = await _cache.GetOrCreateAsync(SD.Cache_Categories, async e =>
            {
                e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                return await _db.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync(ct);
            });

            var vm = new ProductIndexViewModel
            {
                Products = products,
                Brands = new SelectList(brands!, "Id", "Name", brandFilter),
                Categories = new SelectList(categories!, "Id", "Name", categoryFilter),

                SearchString = keyword,
                BrandFilter = brandFilter,
                CategoryFilter = categoryFilter,
                SortBy = sortBy,
                MinPrice = minPrice,
                MaxPrice = maxPrice,

                PageIndex = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                TotalProductCount = totalItems
            };

            // Lưu ý: JS hiện tại fetch toàn bộ HTML rồi thay #resultsMeta/#productsContainer,
            // nên KHÔNG trả partial ở đây. (Giữ fallback nếu bạn dùng nơi khác)
            var xrw = Request.Headers["X-Requested-With"].ToString();
            if (xrw.Equals("XMLHttpRequest", StringComparison.OrdinalIgnoreCase) || Request.Query.ContainsKey("partial"))
            {
                return PartialView("_ProductGridPartial", vm.Products);
            }

            return View(vm);
        }

        // =========================================================
        // SEO route: /products/123/honda-vision-2024
        // =========================================================
        [HttpGet("/products/{id:int}/{slug?}")]
        public async Task<IActionResult> Details(int id, string? slug, CancellationToken ct = default)
        {
            var product = await _db.Products
                .AsNoTracking()
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Specifications)
                .FirstOrDefaultAsync(p => p.Id == id && p.IsPublished, ct);

            if (product == null) return NotFound();

            // Canonical slug
            var expected = ToSlug(product.Name);
            if (!string.IsNullOrWhiteSpace(slug) &&
                !string.Equals(slug, expected, StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToActionPermanent(nameof(Details), new { id, slug = expected });
            }

            // Liên quan: cùng Category
            var related = await _db.Products
                .AsNoTracking()
                .Include(p => p.Brand)
                .Where(p => p.CategoryId == product.CategoryId && p.Id != id && p.IsPublished)
                .OrderByDescending(p => p.CreatedAt)
                .Take(4)
                .ToListAsync(ct);

            TrackRecentlyViewed(id);

            var vm = new ProductDetailViewModel
            {
                Product = product,
                RelatedProducts = related
            };
            return View(vm);
        }

        // =========================================================
        // POST: /products/{id}/add   (thêm vào giỏ từ trang chi tiết/form)
        // =========================================================
        [HttpPost("/products/{id:int}/add")]
        public async Task<IActionResult> AddToCart(int id, int qty = 1, CancellationToken ct = default)
        {
            qty = Math.Max(1, qty);

            var p = await _db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.IsPublished, ct);
            if (p == null)
            {
                TempData[SD.Temp_Error] = "Sản phẩm không tồn tại hoặc tạm ngừng bán.";
                return RedirectToAction(nameof(Index));
            }

            if (p.StockQuantity <= 0)
            {
                TempData[SD.Temp_Warning] = "Sản phẩm đã hết hàng.";
                return RedirectToAction(nameof(Details), new { id, slug = ToSlug(p.Name) });
            }

            // Clamp theo tồn kho (cộng lượng đã có trong giỏ)
            var inCart = _cart.GetItemQuantity(p.Id);
            var canAdd = Math.Max(0, p.StockQuantity - inCart);
            var addQty = Math.Min(qty, canAdd);

            if (addQty <= 0)
            {
                TempData[SD.Temp_Warning] = "Bạn đã thêm tối đa theo tồn kho hiện có.";
                return RedirectToAction("Index", "Cart");
            }

            _cart.AddToCart(p, addQty);
            TempData[SD.Temp_Success] = $"Đã thêm '{p.Name}' vào giỏ ({addQty}).";
            return RedirectToAction("Index", "Cart");
        }

        // =========================================================
        // AJAX: /Products/QuickAdd (body JSON: { productId, quantity })
        // =========================================================
        public record QuickAddDto(int ProductId, int Quantity);

        [HttpPost]
        public async Task<IActionResult> QuickAdd([FromBody] QuickAddDto dto, CancellationToken ct = default)
        {
            if (dto is null || dto.ProductId <= 0 || dto.Quantity <= 0)
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ." });

            var p = await _db.Products.AsNoTracking()
                                      .FirstOrDefaultAsync(x => x.Id == dto.ProductId && x.IsPublished, ct);
            if (p == null)
                return NotFound(new { success = false, message = "Sản phẩm không tồn tại." });

            if (p.StockQuantity <= 0)
                return BadRequest(new { success = false, message = "Sản phẩm đã hết hàng." });

            var inCart = _cart.GetItemQuantity(p.Id);
            var canAdd = Math.Max(0, p.StockQuantity - inCart);
            var addQty = Math.Min(dto.Quantity, canAdd);

            if (addQty <= 0)
                return BadRequest(new { success = false, message = "Đã đạt giới hạn tồn kho trong giỏ." });

            _cart.AddToCart(p, addQty);

            return Ok(new
            {
                success = true,
                message = $"Đã thêm '{p.Name}' ({addQty})",
                cartCount = _cart.GetTotalQuantity(),
                newSubtotal = _cart.GetSubtotal()
            });
        }

        // =========================================================
        // AJAX: /Products/Suggest?term=ho
        // (HomeController đã có alias /products/suggest để đồng bộ JS)
        // =========================================================
        [HttpGet]
        [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "term" })]
        public async Task<IActionResult> Suggest(string term, CancellationToken ct = default)
        {
            term = (term ?? "").Trim();
            if (term.Length < 2) return Ok(Array.Empty<object>());

            var like = $"%{term}%";
            var names = await _db.Products.AsNoTracking()
                .Where(p => p.IsPublished &&
                            (EF.Functions.Like(p.Name, like) ||
                             (p.Brand != null && EF.Functions.Like(p.Brand.Name, like))))
                .OrderBy(p => p.Name)
                .Select(p => new { id = p.Id, name = p.Name })
                .Take(10)
                .ToListAsync(ct);

            return Ok(names);
        }

        // =========================================================
        // AJAX: /Products/PriceRange?brandId=&categoryId=
        // Trả về min/max giá trong tập lọc (hiệu quả hơn .ToList().Min/Max)
        // =========================================================
        [HttpGet]
        [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "brandId", "categoryId" })]
        public async Task<IActionResult> PriceRange(int? brandId, int? categoryId, CancellationToken ct = default)
        {
            var q = _db.Products.AsNoTracking().Where(p => p.IsPublished);
            if (brandId.HasValue) q = q.Where(p => p.BrandId == brandId.Value);
            if (categoryId.HasValue) q = q.Where(p => p.CategoryId == categoryId.Value);

            var stats = await q
                .GroupBy(_ => 1)
                .Select(g => new { min = g.Min(p => p.Price), max = g.Max(p => p.Price) })
                .FirstOrDefaultAsync(ct);

            return Ok(new { min = stats?.min ?? 0m, max = stats?.max ?? 0m });
        }

        // =========================================================
        // QUICK VIEW (partial)
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> QuickView(int id, CancellationToken ct = default)
        {
            var p = await _db.Products
                .AsNoTracking()
                .Include(x => x.Brand)
                .Include(x => x.Images)
                .FirstOrDefaultAsync(x => x.Id == id && x.IsPublished, ct);

            if (p == null) return NotFound();
            return PartialView("_QuickView", p);
        }

        // ======================= Helpers =======================
        private static string ToSlug(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";
            var s = input.Trim().ToLowerInvariant();
            s = Regex.Replace(s, @"\s+", "-");
            s = Regex.Replace(s, @"[^a-z0-9\-]+", "");
            s = Regex.Replace(s, @"-+", "-").Trim('-');
            return s;
        }

        private void TrackRecentlyViewed(int productId)
        {
            try
            {
                var csv = Request.Cookies[RecentlyViewedCookie] ?? "";
                var ids = csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                             .Select(x => int.TryParse(x, out var v) ? v : 0)
                             .Where(v => v > 0)
                             .ToList();

                ids.Remove(productId);
                ids.Insert(0, productId);
                if (ids.Count > RecentlyViewedMax) ids = ids.Take(RecentlyViewedMax).ToList();

                // Không cần JS đọc cookie này → HttpOnly=true; bật Secure theo HTTPS; SameSite=Lax.
                Response.Cookies.Append(
                    RecentlyViewedCookie,
                    string.Join(',', ids),
                    new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = Request.IsHttps,
                        SameSite = SameSiteMode.Lax,
                        Expires = DateTimeOffset.UtcNow.AddDays(7)
                    });
            }
            catch { /* best-effort */ }
        }
    }
}
