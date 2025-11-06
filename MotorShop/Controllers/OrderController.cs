// File: Controllers/OrderController.cs
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MotorShop.Data;
using MotorShop.Models;
using MotorShop.Models.Enums;
using MotorShop.Services;
using MotorShop.Utilities;

namespace MotorShop.Controllers
{
    [Authorize]
    [AutoValidateAntiforgeryToken]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly CartService _cart;
        private readonly ILogger<OrderController> _logger;
        private readonly IMemoryCache _cache;

        public OrderController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            IEmailSender emailSender,
            CartService cart,
            ILogger<OrderController> logger,
            IMemoryCache cache)
        {
            _db = db;
            _userManager = userManager;
            _emailSender = emailSender;
            _cart = cart;
            _logger = logger;
            _cache = cache;
        }

        // =====================================================
        // GET: /Order/History?status=&page=1&pageSize=10
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> History(
            OrderStatus? status = null,
            int page = 1,
            int pageSize = 10,
            CancellationToken ct = default)
        {
            pageSize = Math.Clamp(pageSize, 1, 50);
            var userId = _userManager.GetUserId(User);

            var q = _db.Orders
                .AsNoTracking()
                .Include(o => o.PickupBranch)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                .Where(o => o.UserId == userId);

            if (status.HasValue) q = q.Where(o => o.Status == status.Value);

            var total = await q.CountAsync(ct);
            var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
            page = Math.Clamp(page, 1, totalPages);

            var orders = await q
                .OrderByDescending(o => o.Id) // hoặc OrderDate tuỳ chuẩn
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            ViewBag.Status = status;
            ViewBag.Page = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;

            return View(orders);
        }

        // =====================================================
        // GET: /Order/Details/5
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Details(int id, CancellationToken ct = default)
        {
            var order = await _db.Orders
                .Include(o => o.PickupBranch)
                .Include(o => o.Shipper)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id, ct);

            if (order == null) return NotFound();
            if (!IsOwnerOrAdmin(order)) return Forbid();

            return View(order);
        }

        // =====================================================
        // GET: /Order/OrderConfirmation/5  (trang cảm ơn)
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> OrderConfirmation(int id, CancellationToken ct = default)
        {
            var order = await _db.Orders
                .AsNoTracking()
                .Include(o => o.PickupBranch)
                .Include(o => o.OrderItems).ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == id, ct);

            if (order == null) return NotFound();
            if (!IsOwnerOrAdmin(order)) return Forbid();

            return View(order);
        }

        // =====================================================
        // GET: /Order/Invoice/5 (HTML in-place để in)
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Invoice(int id, CancellationToken ct = default)
        {
            var order = await _db.Orders
                .AsNoTracking()
                .Include(o => o.PickupBranch)
                .Include(o => o.OrderItems).ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == id, ct);

            if (order == null) return NotFound();
            if (!IsOwnerOrAdmin(order)) return Forbid();

            var html = BuildInvoiceHtml(order);
            return Content(html, "text/html; charset=utf-8");
        }

        // =====================================================
        // GET: /Order/InvoiceDownload/5 (tải file .html)
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> InvoiceDownload(int id, CancellationToken ct = default)
        {
            var order = await _db.Orders
                .AsNoTracking()
                .Include(o => o.OrderItems).ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == id, ct);

            if (order == null) return NotFound();
            if (!IsOwnerOrAdmin(order)) return Forbid();

            var html = BuildInvoiceHtml(order);
            Response.Headers["Content-Disposition"] = $"attachment; filename=invoice-{order.Id}.html";
            return Content(html, "text/html; charset=utf-8");
        }

        // =====================================================
        // POST: /Order/Cancel/5
        // cho phép khi Pending/Confirmed/Processing (chưa xuất kho)
        // =====================================================
        [HttpPost]
        public async Task<IActionResult> Cancel(int id, CancellationToken ct = default)
        {
            var order = await _db.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == id, ct);

            if (order == null) return NotFound();
            if (!IsOwnerOrAdmin(order)) return Forbid();

            if (order.Status is not (OrderStatus.Pending or OrderStatus.Confirmed or OrderStatus.Processing))
            {
                TempData[SD.Temp_Warning] = "Đơn hàng không thể hủy ở trạng thái hiện tại.";
                return RedirectToAction(nameof(Details), new { id });
            }

            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                // Hoàn kho
                var productIds = order.OrderItems.Select(oi => oi.ProductId).Distinct().ToArray();
                var products = await _db.Products.Where(p => productIds.Contains(p.Id)).ToListAsync(ct);
                var map = products.ToDictionary(p => p.Id);

                foreach (var item in order.OrderItems)
                {
                    if (map.TryGetValue(item.ProductId, out var prod))
                    {
                        prod.StockQuantity += item.Quantity;
                        prod.UpdatedAt = DateTime.UtcNow;
                        _db.Products.Update(prod);
                    }
                }

                // Hoàn tiền mô phỏng (nếu enum có Refunded)
                if (order.PaymentStatus == PaymentStatus.Paid)
                {
                    try { order.PaymentStatus = PaymentStatus.Refunded; } catch { /* enum có thể không có Refunded */ }
                }

                order.Status = OrderStatus.Cancelled;

                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                TempData[SD.Temp_Success] = "Đơn hàng đã được hủy.";
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await tx.RollbackAsync(ct);
                _logger.LogError(ex, "Cancel order concurrency error: {OrderId}", id);
                TempData[SD.Temp_Error] = "Đơn hàng thay đổi trong lúc xử lý. Vui lòng thử lại.";
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(ct);
                _logger.LogError(ex, "Cancel order failed: {OrderId}", id);
                TempData[SD.Temp_Error] = "Có lỗi khi hủy đơn hàng. Vui lòng thử lại.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // =====================================================
        // POST: /Order/ConfirmReceived/5  (user xác nhận đã nhận)
        // =====================================================
        [HttpPost]
        public async Task<IActionResult> ConfirmReceived(int id, CancellationToken ct = default)
        {
            var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == id, ct);
            if (order == null) return NotFound();
            if (!IsOwnerOrAdmin(order)) return Forbid();

            if (order.Status == OrderStatus.Cancelled)
            {
                TempData[SD.Temp_Warning] = "Đơn đã hủy không thể xác nhận nhận hàng.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Tuỳ chuẩn nghiệp vụ của bạn:
            // Nếu muốn siết chặt hơn:
            // if (order.Status is not (OrderStatus.Shipping or OrderStatus.Delivered))
            // {
            //     TempData[SD.Temp_Warning] = "Đơn chưa ở trạng thái có thể xác nhận nhận hàng.";
            //     return RedirectToAction(nameof(Details), new { id });
            // }

            order.Status = OrderStatus.Completed;
            if (order.PaymentStatus == PaymentStatus.Pending)
                order.PaymentStatus = PaymentStatus.Paid; // COD -> coi như đã thu

            await _db.SaveChangesAsync(ct);
            TempData[SD.Temp_Success] = "Cảm ơn bạn! Đơn đã được xác nhận hoàn tất.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // =====================================================
        // POST: /Order/ResendEmail/5  (chống spam 60s)
        // =====================================================
        [HttpPost]
        public async Task<IActionResult> ResendEmail(int id, CancellationToken ct = default)
        {
            var order = await _db.Orders
                .AsNoTracking()
                .Include(o => o.OrderItems).ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == id, ct);

            if (order == null) return NotFound();
            if (!IsOwnerOrAdmin(order)) return Forbid();

            var userId = _userManager.GetUserId(User) ?? "";
            var cacheKey = $"resend:{userId}:{id}";
            if (_cache.TryGetValue(cacheKey, out _))
            {
                TempData[SD.Temp_Warning] = "Bạn vừa yêu cầu gửi lại email. Vui lòng thử lại sau ít phút.";
                return RedirectToAction(nameof(Details), new { id });
            }

            try
            {
                var html = BuildEmailHtml(order);
                var to = order.ReceiverEmail ?? (await _userManager.GetUserAsync(User))?.Email ?? "";
                if (!string.IsNullOrWhiteSpace(to))
                    await _emailSender.SendEmailAsync(to, SD.EmailSubject_PaymentConfirmation, html);

                _cache.Set(cacheKey, true, TimeSpan.FromMinutes(1)); // cooldown 60s
                TempData[SD.Temp_Success] = "Đã gửi lại email xác nhận.";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Resend email failed: {OrderId}", id);
                TempData[SD.Temp_Error] = "Không thể gửi email. Vui lòng thử lại sau.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // =====================================================
        // POST: /Order/Reorder/5  — Mua lại toàn bộ
        // =====================================================
        [HttpPost]
        public async Task<IActionResult> Reorder(int id, CancellationToken ct = default)
        {
            var order = await _db.Orders
                .Include(o => o.OrderItems).ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == id, ct);

            if (order == null) return NotFound();
            if (!IsOwnerOrAdmin(order)) return Forbid();

            var candidates = order.OrderItems
                .Where(oi => oi.Product != null && oi.Product.IsPublished && oi.Product.StockQuantity > 0)
                .ToList();

            if (candidates.Count == 0)
            {
                TempData[SD.Temp_Warning] = "Không có sản phẩm còn hàng để thêm vào giỏ.";
                return RedirectToAction(nameof(Details), new { id });
            }

            int lines = 0, units = 0;
            foreach (var oi in candidates)
            {
                var p = oi.Product!;
                var inCart = _cart.GetItemQuantity(p.Id);
                var maxAdd = Math.Max(0, p.StockQuantity - inCart);
                var qty = Math.Min(Math.Max(1, oi.Quantity), maxAdd);
                if (qty <= 0) continue;

                _cart.Add(p, qty);
                lines++;
                units += qty;
            }

            TempData[SD.Temp_Success] = lines > 0
                ? $"Đã thêm {lines} dòng / {units} sản phẩm vào giỏ."
                : "Tồn kho hiện tại không đủ để thêm các sản phẩm vào giỏ.";

            return RedirectToAction("Index", "Cart");
        }

        // =====================================================
        // POST: /Order/ReorderItem/5?productId=123&qty=2  — mua lại 1 món
        // =====================================================
        [HttpPost]
        public async Task<IActionResult> ReorderItem(int id, int productId, int qty = 1, CancellationToken ct = default)
        {
            qty = Math.Max(1, qty);

            var order = await _db.Orders
                .Include(o => o.OrderItems).ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == id, ct);

            if (order == null) return NotFound();
            if (!IsOwnerOrAdmin(order)) return Forbid();

            var item = order.OrderItems.FirstOrDefault(i => i.ProductId == productId);
            if (item?.Product == null || !item.Product.IsPublished || item.Product.StockQuantity <= 0)
            {
                TempData[SD.Temp_Warning] = "Sản phẩm không còn bán hoặc đã hết hàng.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var p = item.Product;
            var inCart = _cart.GetItemQuantity(p.Id);
            var canAdd = Math.Max(0, p.StockQuantity - inCart);
            var addQty = Math.Min(qty, canAdd);

            if (addQty <= 0)
            {
                TempData[SD.Temp_Warning] = "Sản phẩm đã đạt giới hạn tồn kho trong giỏ.";
                return RedirectToAction(nameof(Details), new { id });
            }

            _cart.Add(p, addQty);
            TempData[SD.Temp_Success] = "Đã thêm lại sản phẩm vào giỏ.";
            return RedirectToAction("Index", "Cart");
        }

        // =====================================================
        // GET: /Order/Track/123  (JSON tracking đơn giản)
        // =====================================================
        [HttpGet]
        [Produces("application/json")]
        public async Task<IActionResult> Track(int id, CancellationToken ct = default)
        {
            var me = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole(SD.Role_Admin);

            var order = await _db.Orders
                .AsNoTracking()
                .Include(o => o.Shipper)
                .FirstOrDefaultAsync(o => o.Id == id, ct);

            if (order == null) return NotFound();
            if (!isAdmin && order.UserId != me!.Id) return Forbid();

            return Ok(new
            {
                success = true,
                status = order.Status.ToString(),
                shipper = order.Shipper?.Name,
                tracking = order.TrackingCode
            });
        }

        // =====================================================
        // GET: /Order/Quote?method=HomeDelivery&selected=1,2,3 (JSON báo giá nhanh)
        // =====================================================
        [HttpGet]
        [Produces("application/json")]
        public IActionResult Quote(DeliveryMethod method, string? selected)
        {
            var items = _cart.GetCartItems();

            if (!string.IsNullOrWhiteSpace(selected))
            {
                var idSet = selected.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                    .Select(s => int.TryParse(s, out var x) ? x : (int?)null)
                                    .Where(x => x.HasValue)
                                    .Select(x => x!.Value)
                                    .ToHashSet();
                items = items.Where(i => idSet.Contains(i.ProductId)).ToList();
            }

            var subtotal = items.Sum(i => i.Subtotal);
            var shipping = CalculateShipping(method);
            var total = subtotal + shipping;

            return Ok(new { success = true, subtotal, shipping, total });
        }

        // =====================================================
        // POST: /Order/UpdateDelivery (đổi phương thức giao nhận)
        // =====================================================
        [HttpPost]
        public async Task<IActionResult> UpdateDelivery(UpdateDeliveryRequest model, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                TempData[SD.Temp_Error] = "Dữ liệu không hợp lệ.";
                return RedirectToAction(nameof(Details), new { id = model.OrderId });
            }

            var me = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole(SD.Role_Admin);

            var order = await _db.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == model.OrderId, ct);

            if (order == null) return NotFound();
            if (!isAdmin && order.UserId != me!.Id) return Forbid();

            if (order.Status is not (OrderStatus.Pending or OrderStatus.Confirmed or OrderStatus.Processing))
            {
                TempData[SD.Temp_Warning] = "Đơn đã ở trạng thái không thể đổi giao nhận.";
                return RedirectToAction(nameof(Details), new { id = model.OrderId });
            }

            if (model.DeliveryMethod == DeliveryMethod.HomeDelivery)
            {
                if (string.IsNullOrWhiteSpace(model.ShippingAddress))
                {
                    TempData[SD.Temp_Error] = "Vui lòng nhập địa chỉ giao hàng.";
                    return RedirectToAction(nameof(Details), new { id = model.OrderId });
                }
                order.ShippingAddress = model.ShippingAddress!.Trim();
                order.PickupBranchId = null;
            }
            else
            {
                if (!model.PickupBranchId.HasValue ||
                    !await _db.Branches.AnyAsync(b => b.IsActive && b.Id == model.PickupBranchId.Value, ct))
                {
                    TempData[SD.Temp_Error] = "Chi nhánh nhận không hợp lệ.";
                    return RedirectToAction(nameof(Details), new { id = model.OrderId });
                }
                order.PickupBranchId = model.PickupBranchId;
                order.ShippingAddress = null;
            }

            order.DeliveryMethod = model.DeliveryMethod;
            order.ShippingFee = CalculateShipping(order.DeliveryMethod);
            var subtotal = order.OrderItems.Sum(oi => oi.UnitPrice * oi.Quantity);
            order.TotalAmount = subtotal + order.ShippingFee - order.DiscountAmount;

            await _db.SaveChangesAsync(ct);

            TempData[SD.Temp_Success] = "Đã cập nhật phương thức giao nhận.";
            return RedirectToAction(nameof(Details), new { id = model.OrderId });
        }

        // ======================= Helpers =======================
        private bool IsOwnerOrAdmin(Order order)
            => order.UserId == _userManager.GetUserId(User) || User.IsInRole(SD.Role_Admin);

        private static decimal CalculateShipping(DeliveryMethod method)
            => method == DeliveryMethod.HomeDelivery ? 30000m : 0m;

        private static string BuildInvoiceHtml(Order order)
        {
            var enc = HtmlEncoder.Default;
            var sb = new StringBuilder();
            sb.Append($@"
<!doctype html><html lang='vi'><meta charset='utf-8'>
<title>Hóa đơn #{order.Id} - MotorShop</title>
<style>
body{{font-family:system-ui,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#111}}
.wrap{{max-width:780px;margin:24px auto;padding:24px;border:1px solid #e5e7eb;border-radius:12px;background:#fff}}
h1{{margin:0 0 12px}}
table{{width:100%;border-collapse:collapse}}
th,td{{padding:8px}}
th{{text-align:left;border-bottom:1px solid #e5e7eb;color:#6b7280}}
td:last-child,th:last-child{{text-align:right}}
.muted{{color:#6b7280}}
.total td{{font-weight:700;border-top:1px solid #e5e7eb}}
@media print{{.noprint{{display:none}} body{{background:#fff}}}}
</style>
<div class='wrap'>
  <h1>Hóa đơn #{order.Id}</h1>
  <p class='muted'>Ngày: {order.OrderDate:dd/MM/yyyy HH:mm}</p>
  <p><b>Người nhận:</b> {enc.Encode(order.ReceiverName ?? "")} — {enc.Encode(order.ReceiverPhone ?? "")}<br/>
     <b>Email:</b> {enc.Encode(order.ReceiverEmail ?? "")}<br/>" +
     (order.DeliveryMethod == DeliveryMethod.HomeDelivery
      ? $"<b>Địa chỉ giao:</b> {enc.Encode(order.ShippingAddress ?? "")}"
      : $"<b>Nhận tại chi nhánh:</b> #{order.PickupBranchId}") + @"</p>

  <table>
    <thead><tr><th>Sản phẩm</th><th>SL</th><th>Đơn giá</th><th>Tạm tính</th></tr></thead>
    <tbody>");
            foreach (var it in order.OrderItems)
            {
                var line = (it.Quantity * it.UnitPrice).ToString("#,0");
                sb.Append($@"<tr>
<td>{enc.Encode(it.Product?.Name ?? $"#{it.ProductId}")}</td>
<td style='text-align:right'>{it.Quantity}</td>
<td style='text-align:right'>{it.UnitPrice:#,0} ₫</td>
<td style='text-align:right'>{line} ₫</td>
</tr>");
            }
            sb.Append($@"</tbody>
    <tfoot>
      <tr><td colspan='3' class='muted'>Phí vận chuyển</td><td>{order.ShippingFee:#,0} ₫</td></tr>
      <tr><td colspan='3' class='muted'>Giảm giá</td><td>-{order.DiscountAmount:#,0} ₫</td></tr>
      <tr class='total'><td colspan='3'>Tổng cộng</td><td>{order.TotalAmount:#,0} ₫</td></tr>
      <tr><td colspan='3' class='muted'>Thanh toán</td><td>{order.PaymentMethod} — {(order.PaymentStatus == PaymentStatus.Paid ? "Đã thanh toán" : "Chờ thanh toán")}</td></tr>
    </tfoot>
  </table>

  <p class='muted'>© {DateTime.UtcNow:yyyy} MotorShop</p>
  <button class='noprint' onclick='print()'>In hóa đơn</button>
</div></html>");
            return sb.ToString();
        }

        private static string BuildEmailHtml(Order order)
        {
            var enc = HtmlEncoder.Default;
            var sb = new StringBuilder();
            sb.Append($@"<h2>Xác nhận đơn hàng #{order.Id}</h2>
<p>Xin chào {enc.Encode(order.ReceiverName ?? "")}, cảm ơn bạn đã đặt hàng tại MotorShop.</p>
<table style='width:100%;border-collapse:collapse'>
<thead><tr><th align='left'>Sản phẩm</th><th align='right'>SL</th><th align='right'>Đơn giá</th><th align='right'>Tạm tính</th></tr></thead><tbody>");
            foreach (var it in order.OrderItems)
            {
                var line = (it.Quantity * it.UnitPrice).ToString("#,0");
                sb.Append($@"<tr>
<td>{enc.Encode(it.Product?.Name ?? $"#{it.ProductId}")}</td>
<td align='right'>{it.Quantity}</td>
<td align='right'>{it.UnitPrice:#,0} ₫</td>
<td align='right'>{line} ₫</td>
</tr>");
            }
            sb.Append($@"</tbody></table>
<p><b>Tổng thanh toán:</b> {order.TotalAmount:#,0} ₫</p>
<p>Trạng thái thanh toán: {(order.PaymentStatus == PaymentStatus.Paid ? "Đã thanh toán" : "Chờ thanh toán")}</p>");
            return sb.ToString();
        }
    }

    // ======= DTOs =======
    public class UpdateDeliveryRequest
    {
        [Required]
        public int OrderId { get; set; }

        [Required]
        public DeliveryMethod DeliveryMethod { get; set; }

        // HomeDelivery
        [StringLength(255)]
        public string? ShippingAddress { get; set; }

        // PickupAtStore
        public int? PickupBranchId { get; set; }
    }
}
