using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MotorShop.Data;
using MotorShop.Models;
using MotorShop.Models.Enums;
using MotorShop.Utilities;
using MotorShop.ViewModels;
using MotorShop.ViewModels.Home;
using System.Diagnostics;
namespace MotorShop.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<HomeController> _logger;
        private readonly IMemoryCache _cache;
        private readonly IEmailSender _emailSender;
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

        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger, IMemoryCache cache, IEmailSender emailSender)
        {
            _db = context;
            _logger = logger;
            _cache = cache;
            _emailSender = emailSender;
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendRequest(ContactRequestViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Cập nhật tiêu đề thư: Thêm Icon chuông + Tên + SĐT để Admin dễ nhận biết
                    string subject = $"🔔 Yêu cầu tư vấn";

                    string body = $@"
        <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
            <h2 style='color: #1d4ed8;'>Yêu cầu tìm xe từ Website</h2>
            <p>Xin chào Admin,</p>
            <p>Hệ thống vừa nhận được yêu cầu tư vấn mới với thông tin chi tiết như sau:</p>
            
            <table style='width: 100%; border-collapse: collapse; margin: 20px 0;'>
                <tr>
                    <td style='padding: 8px; border: 1px solid #ddd; background: #f9f9f9; width: 150px; font-weight: bold;'>Họ tên:</td>
                    <td style='padding: 8px; border: 1px solid #ddd;'>{model.FullName}</td>
                </tr>
                <tr>
                    <td style='padding: 8px; border: 1px solid #ddd; background: #f9f9f9; font-weight: bold;'>Số điện thoại:</td>
                    <td style='padding: 8px; border: 1px solid #ddd;'><a href='tel:{model.Phone}' style='color: #1d4ed8; font-weight: bold;'>{model.Phone}</a></td>
                </tr>
                <tr>
                    <td style='padding: 8px; border: 1px solid #ddd; background: #f9f9f9; font-weight: bold;'>Email:</td>
                    <td style='padding: 8px; border: 1px solid #ddd;'><a href='mailto:{model.Email}'>{model.Email}</a></td>
                </tr>
                <tr>
                    <td style='padding: 8px; border: 1px solid #ddd; background: #f9f9f9; font-weight: bold;'>Nhu cầu chi tiết:</td>
                    <td style='padding: 8px; border: 1px solid #ddd; background: #fff8e1; color: #b45309;'>{model.RequestContent}</td>
                </tr>
            </table>

            <p><em>Vui lòng liên hệ lại khách hàng trong thời gian sớm nhất.</em></p>
            <hr style='border: 0; border-top: 1px solid #eee; margin: 20px 0;'>
            <small style='color: #888;'>Email này được gửi tự động từ hệ thống MotorShop.</small>
        </div>
    ";

                    // Gửi email
                    await _emailSender.SendEmailAsync("danghieu7bthcsnh@gmail.com", subject, body);

                    TempData[SD.Temp_Success] = "Đã gửi yêu cầu thành công! Chúng tôi sẽ liên hệ lại sớm.";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi gửi email liên hệ");
                    TempData[SD.Temp_Error] = "Có lỗi xảy ra khi gửi yêu cầu. Vui lòng thử lại.";
                }
            }
            else
            {
                TempData[SD.Temp_Error] = "Vui lòng điền đầy đủ thông tin.";
            }

            // Quay lại trang chủ và neo xuống phần form
            return RedirectToAction(nameof(Index), new { fragment = "contact-form" });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Subscribe(string email)
        {
            // 1. Validate Email
            if (string.IsNullOrWhiteSpace(email) || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email))
            {
                TempData[SD.Temp_Error] = "Vui lòng nhập địa chỉ Email hợp lệ.";
                return RedirectToAction(nameof(Index), new { fragment = "footer" });
            }

            try
            {
                // ========================================================================
                // GỬI EMAIL 1: THÔNG BÁO CHO ADMIN (Để admin nắm thông tin)
                // ========================================================================
                string adminSubject = $"📬Khách hàng mới đăng ký nhận ưu đãi";
                string adminBody = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; border: 1px solid #e2e8f0; padding: 20px; border-radius: 10px;'>
                <h3 style='color: #2563eb; margin-top: 0;'>🔔 Thông báo Subscriber mới</h3>
                <p>Xin chào Admin,</p>
                <p>Website vừa ghi nhận một khách hàng đăng ký nhận bản tin (Newsletter):</p>
                <div style='background: #f8fafc; padding: 15px; border-radius: 8px; border-left: 4px solid #2563eb;'>
                    <p style='margin: 0;'><strong>Email:</strong> <a href='mailto:{email}' style='color: #0f172a; text-decoration: none;'>{email}</a></p>
                    <p style='margin: 5px 0 0;'><strong>Thời gian:</strong> {DateTime.Now:dd/MM/yyyy HH:mm}</p>
                </div>
                <p style='color: #64748b; font-size: 12px; margin-top: 20px;'>Hệ thống MotorShop tự động gửi.</p>
            </div>
        ";

                // Gửi cho Admin (Email của bạn)
                await _emailSender.SendEmailAsync("danghieu7bthcsnh@gmail.com", adminSubject, adminBody);


                // ========================================================================
                // GỬI EMAIL 2: GỬI ƯU ĐÃI CHO KHÁCH HÀNG (Quan trọng)
                // ========================================================================
                string clientSubject = "🎉 Chào mừng bạn đến với MotorShop - Nhận ngay ưu đãi đặc biệt!";
                string clientBody = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; color: #333; line-height: 1.6;'>
                <div style='background: #0f172a; padding: 20px; text-align: center; border-radius: 10px 10px 0 0;'>
                    <h2 style='color: #fff; margin: 0;'>MOTORSHOP VIETNAM</h2>
                </div>

                <div style='border: 1px solid #e2e8f0; border-top: none; padding: 30px; border-radius: 0 0 10px 10px;'>
                    <h2 style='color: #2563eb; margin-top: 0;'>Cảm ơn bạn đã đăng ký!</h2>
                    <p>Xin chào,</p>
                    <p>Chúng tôi rất vui khi bạn đã quan tâm đến các mẫu xe và dịch vụ tại <strong>MotorShop</strong>.</p>
                    
                    <p>Để tri ân sự quan tâm này, MotorShop xin gửi tặng bạn voucher giảm giá cho lần bảo dưỡng hoặc mua phụ kiện đầu tiên:</p>
                    
                    <div style='text-align: center; margin: 30px 0;'>
                        <span style='background: #fee2e2; color: #ef4444; font-size: 24px; font-weight: bold; padding: 15px 30px; border-radius: 8px; border: 2px dashed #ef4444; display: inline-block;'>
                            WELCOME2025
                        </span>
                        <p style='color: #64748b; font-size: 13px; margin-top: 10px;'>Giảm <strong>10%</strong> tối đa 200k (Hạn dùng: 30 ngày)</p>
                    </div>

                    <p>Từ nay, bạn sẽ là người đầu tiên nhận được thông tin về:</p>
                    <ul style='color: #475569;'>
                        <li>Các mẫu xe mới về (SH 2025, Exciter, PKL...)</li>
                        <li>Chương trình khuyến mãi & trả góp 0%</li>
                        <li>Kinh nghiệm chăm sóc xe hữu ích</li>
                    </ul>

                    <div style='text-align: center; margin-top: 40px;'>
                        <a href='https://localhost:7198/products' style='background: #2563eb; color: #fff; text-decoration: none; padding: 12px 25px; border-radius: 30px; font-weight: bold;'>Xem xe ngay</a>
                    </div>
                </div>

                <div style='text-align: center; padding: 20px; color: #94a3b8; font-size: 12px;'>
                    <p>© {DateTime.Now.Year} MotorShop Vietnam. All rights reserved.</p>
                    <p>123 ABC, Hoàn Kiếm, Hà Nội | Hotline: 1900 1234</p>
                </div>
            </div>
        ";

                // Gửi cho Khách hàng (Email họ vừa nhập)
                await _emailSender.SendEmailAsync(email, clientSubject, clientBody);

                // Thông báo ra màn hình
                TempData[SD.Temp_Success] = "Đăng ký thành công! Hãy kiểm tra Email để nhận mã ưu đãi.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi gửi mail subscribe");
                TempData[SD.Temp_Error] = "Hệ thống đang bận, vui lòng thử lại sau.";
            }

            return RedirectToAction(nameof(Index));
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