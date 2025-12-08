using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MotorShop.Data;
using MotorShop.Models;
using MotorShop.Utilities;

namespace MotorShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Manager")]
    public class ShipperController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ShipperController(ApplicationDbContext db)
        {
            _db = db;
        }

        // ================== ViewModel cho Index ==================
        public class ShipperIndexViewModel
        {
            public string? Search { get; set; }
            public bool? OnlyActive { get; set; }

            public int Page { get; set; }
            public int TotalPages { get; set; }
            public int TotalShippers { get; set; }
            public int ActiveCount { get; set; }
            public int InactiveCount { get; set; }

            public List<Shipper> Items { get; set; } = new();

            /// <summary>
            /// Tổng số đơn gắn với từng Shipper (key = ShipperId)
            /// </summary>
            public Dictionary<int, int> TotalOrdersByShipper { get; set; } = new();

            /// <summary>
            /// Số đơn đang "mở" / chưa hoàn tất của từng Shipper (Pending/Confirmed/Processing/Shipping...)
            /// </summary>
            public Dictionary<int, int> OpenOrdersByShipper { get; set; } = new();
        }

        // ================== 1. INDEX: Danh sách + Lọc + Phân trang ==================
        public async Task<IActionResult> Index(string? q, bool? onlyActive, int page = 1)
        {
            const int pageSize = 10;
            page = Math.Max(1, page);

            var query = _db.Shippers
                .AsNoTracking()
                .AsQueryable();

            // --- Tìm kiếm theo tên / mã / SĐT ---
            if (!string.IsNullOrWhiteSpace(q))
            {
                var kw = q.Trim();
                query = query.Where(s =>
                    EF.Functions.Like(s.Name, $"%{kw}%") ||
                    (!string.IsNullOrEmpty(s.Code) && EF.Functions.Like(s.Code!, $"%{kw}%")) ||
                    (!string.IsNullOrEmpty(s.Phone) && s.Phone!.Contains(kw)));
            }

            // --- Lọc trạng thái hoạt động ---
            if (onlyActive.HasValue)
            {
                query = query.Where(s => s.IsActive == onlyActive.Value);
            }

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(s => s.IsActive)   // Shipper đang hoạt động lên trước
                .ThenBy(s => s.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // ---------- Thống kê tổng/active/inactive ----------
            var activeCount = await _db.Shippers.CountAsync(s => s.IsActive);
            var inactiveCount = await _db.Shippers.CountAsync(s => !s.IsActive);

            // ---------- Thống kê đơn hàng theo Shipper ----------
            // Lấy tất cả order có gán ShipperId
            var ordersByShipper = await _db.Orders
                .AsNoTracking()
                .Where(o => o.ShipperId != null)
                .GroupBy(o => o.ShipperId!.Value)
                .Select(g => new
                {
                    ShipperId = g.Key,
                    TotalOrders = g.Count()
                })
                .ToDictionaryAsync(x => x.ShipperId, x => x.TotalOrders);

            // Đơn "đang mở": tuỳ enum OrderStatus của bạn
            // giả sử: Pending, Confirmed, Processing, Shipping = đơn chưa hoàn tất
            var openOrdersByShipper = await _db.Orders
                .AsNoTracking()
                .Where(o => o.ShipperId != null &&
                            (o.Status == Models.Enums.OrderStatus.Pending ||
                             o.Status == Models.Enums.OrderStatus.Confirmed ||
                             o.Status == Models.Enums.OrderStatus.Processing ||
                             o.Status == Models.Enums.OrderStatus.Shipping))
                .GroupBy(o => o.ShipperId!.Value)
                .Select(g => new
                {
                    ShipperId = g.Key,
                    OpenOrders = g.Count()
                })
                .ToDictionaryAsync(x => x.ShipperId, x => x.OpenOrders);

            var vm = new ShipperIndexViewModel
            {
                Search = q,
                OnlyActive = onlyActive,
                Page = page,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                TotalShippers = total,
                ActiveCount = activeCount,
                InactiveCount = inactiveCount,
                Items = items,
                TotalOrdersByShipper = ordersByShipper,
                OpenOrdersByShipper = openOrdersByShipper
            };

            return View(vm);   // View: Areas/Admin/Views/Shipper/Index.cshtml
        }

        // ================== 2. CREATE ==================
        public IActionResult Create()
        {
            return View(new Shipper());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Shipper shipper)
        {
            if (!ModelState.IsValid)
            {
                return View(shipper);
            }

            // Normalize text
            shipper.Name = shipper.Name.Trim();
            if (!string.IsNullOrWhiteSpace(shipper.Code))
                shipper.Code = shipper.Code.Trim().ToUpperInvariant();
            if (!string.IsNullOrWhiteSpace(shipper.Phone))
                shipper.Phone = shipper.Phone.Trim();
            if (!string.IsNullOrWhiteSpace(shipper.Note))
                shipper.Note = shipper.Note.Trim();

            _db.Shippers.Add(shipper);
            await _db.SaveChangesAsync();

            TempData[SD.Temp_Success] = "Đã thêm đơn vị giao hàng mới.";
            return RedirectToAction(nameof(Index));
        }

        // ================== 3. EDIT ==================
        public async Task<IActionResult> Edit(int id)
        {
            var shipper = await _db.Shippers.FindAsync(id);
            if (shipper == null) return NotFound();

            return View(shipper);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Shipper input)
        {
            if (id != input.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                return View(input);
            }

            var shipper = await _db.Shippers.FirstOrDefaultAsync(s => s.Id == id);
            if (shipper == null) return NotFound();

            shipper.Name = input.Name.Trim();
            shipper.Code = string.IsNullOrWhiteSpace(input.Code)
                ? null
                : input.Code.Trim().ToUpperInvariant();
            shipper.Phone = string.IsNullOrWhiteSpace(input.Phone)
                ? null
                : input.Phone.Trim();
            shipper.Note = string.IsNullOrWhiteSpace(input.Note)
                ? null
                : input.Note.Trim();
            shipper.IsActive = input.IsActive;

            await _db.SaveChangesAsync();

            TempData[SD.Temp_Success] = "Đã cập nhật thông tin đơn vị giao hàng.";
            return RedirectToAction(nameof(Index));
        }

        // ================== 4. DELETE ==================
        public async Task<IActionResult> Delete(int id)
        {
            var shipper = await _db.Shippers
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id);

            if (shipper == null) return NotFound();

            return View(shipper);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var shipper = await _db.Shippers
                .Include(s => s.Orders)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (shipper == null) return NotFound();

            // Nếu đã có đơn gắn với Shipper thì KHÔNG cho xoá, tránh mất lịch sử
            if (shipper.Orders != null && shipper.Orders.Any())
            {
                TempData[SD.Temp_Error] =
                    "Không thể xoá đơn vị giao hàng vì đã có đơn hàng gắn với shipper này. " +
                    "Bạn chỉ có thể tạm ẩn (Inactive).";

                return RedirectToAction(nameof(Delete), new { id });
            }

            _db.Shippers.Remove(shipper);
            await _db.SaveChangesAsync();

            TempData[SD.Temp_Success] = "Đã xoá đơn vị giao hàng.";
            return RedirectToAction(nameof(Index));
        }

        // ================== 5. TOGGLE ACTIVE (Bật/Tắt nhanh) ==================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id, string? returnUrl = null)
        {
            var shipper = await _db.Shippers.FindAsync(id);
            if (shipper == null) return NotFound();

            shipper.IsActive = !shipper.IsActive;
            await _db.SaveChangesAsync();

            TempData[SD.Temp_Success] = shipper.IsActive
                ? "Đã kích hoạt đơn vị giao hàng."
                : "Đã tạm ẩn đơn vị giao hàng.";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction(nameof(Index));
        }
    }
}
