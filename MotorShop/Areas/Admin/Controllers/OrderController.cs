using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MotorShop.Data;
using MotorShop.Models.Enums;
using MotorShop.Utilities;

namespace MotorShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Manager")]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _db;
        public OrderController(ApplicationDbContext db) => _db = db;

        // GET: Admin/Order
        public async Task<IActionResult> Index(string? q, OrderStatus? status, DateTime? from, DateTime? to, int page = 1)
        {
            int pageSize = 15;
            var query = _db.Orders.AsNoTracking().Include(o => o.User).AsQueryable();

            // 1. Thống kê cho thanh quy trình
            var statusCounts = await _db.Orders
                .AsNoTracking()
                .GroupBy(o => o.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count);
            ViewBag.StatusCounts = statusCounts;

            // 2. Lọc dữ liệu
            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim().ToLower();
                query = query.Where(o => o.Id.ToString().Contains(q) ||
                                         (o.ReceiverName != null && o.ReceiverName.ToLower().Contains(q)) ||
                                         (o.ReceiverPhone != null && o.ReceiverPhone.Contains(q)));
            }
            if (status.HasValue)
            {
                query = query.Where(o => o.Status == status.Value);
            }
            if (from.HasValue)
            {
                query = query.Where(o => o.OrderDate >= from.Value.ToUniversalTime());
            }
            if (to.HasValue)
            {
                query = query.Where(o => o.OrderDate <= to.Value.AddDays(1).ToUniversalTime());
            }

            // 3. Phân trang
            var totalItems = await query.CountAsync();
            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            ViewBag.Q = q;
            ViewBag.Status = status;
            ViewBag.From = from?.ToString("yyyy-MM-dd");
            ViewBag.To = to?.ToString("yyyy-MM-dd");

            return View(orders);
        }

        // GET: Admin/Order/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var order = await _db.Orders
                .AsNoTracking()
                .Include(o => o.User)
                .Include(o => o.PickupBranch)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                        .ThenInclude(p => p.Images)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();
            return View(order);
        }

        // POST: Admin/Order/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, OrderStatus status, string? returnUrl = null)
        {
            var order = await _db.Orders.FindAsync(id);
            if (order == null) return NotFound();

            // Logic tự động cập nhật thanh toán cho đơn COD khi giao thành công hoặc hoàn tất
            if ((status == OrderStatus.Delivered || status == OrderStatus.Completed)
                && order.PaymentStatus == PaymentStatus.Pending
                && order.PaymentMethod == PaymentMethod.CashOnDelivery)
            {
                order.PaymentStatus = PaymentStatus.Paid;
            }

            // Logic hoàn kho khi hủy đơn (nếu cần)
            if (status == OrderStatus.Cancelled && order.Status != OrderStatus.Cancelled)
            {
                // Thêm logic hoàn kho ở đây nếu muốn
            }

            order.Status = status;
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Đã cập nhật đơn hàng #{id} sang trạng thái {status}.";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}