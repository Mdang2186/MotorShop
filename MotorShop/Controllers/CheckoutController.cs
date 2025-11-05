// File: Controllers/CheckoutController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MotorShop.Data;
using MotorShop.Models;
using MotorShop.Models.Enums;
using MotorShop.Services;
using MotorShop.Utilities;
using MotorShop.ViewModels;
using System.Text;
using System.Text.Encodings.Web;

namespace MotorShop.Controllers
{
    [Authorize]
    [AutoValidateAntiforgeryToken]
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly CartService _cart;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<CheckoutController> _logger;

        public CheckoutController(
            ApplicationDbContext db,
            CartService cart,
            UserManager<ApplicationUser> userManager,
            IEmailSender emailSender,
            ILogger<CheckoutController> logger)
        {
            _db = db;
            _cart = cart;
            _userManager = userManager;
            _emailSender = emailSender;
            _logger = logger;
        }

        // ===============================
        // GET: /Checkout?selected=1,3,9
        // ===============================
        [HttpGet]
        public async Task<IActionResult> Index(string? selected, CancellationToken ct)
        {
            var items = _cart.GetCartItems();

            // Nếu có danh sách được chọn -> lọc
            int[]? selectedIds = null;
            if (!string.IsNullOrWhiteSpace(selected))
            {
                selectedIds = selected
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(s => int.TryParse(s, out var x) ? x : (int?)null)
                    .Where(x => x.HasValue)
                    .Select(x => x!.Value)
                    .Distinct()
                    .ToArray();

                if (selectedIds.Length > 0)
                    items = items.Where(i => selectedIds.Contains(i.ProductId)).ToList();
            }

            if (items.Count == 0)
            {
                TempData[SD.Temp_Info] = "Giỏ hàng của bạn đang trống.";
                return RedirectToAction("Index", "Products");
            }

            var me = await _userManager.GetUserAsync(User);

            var branches = await _db.Branches
                .Where(b => b.IsActive)
                .OrderBy(b => b.Name)
                .AsNoTracking()
                .ToListAsync(ct);

            var banks = await _db.Banks
                .Where(b => b.IsActive)
                .OrderBy(b => b.SortOrder)
                .AsNoTracking()
                .ToListAsync(ct);

            var ss = _cart.GetCheckoutSession() ?? new CartService.CheckoutSession
            {
                DeliveryMethod = DeliveryMethod.HomeDelivery,
                PaymentMethod = PaymentMethod.Card,
                ShippingAddress = me?.Address,
                ReceiverName = me?.FullName ?? me?.Email,
                ReceiverPhone = me?.PhoneNumber,
                ReceiverEmail = me?.Email,
                SelectedBankCode = banks.FirstOrDefault()?.Code
            };

            var vm = new CheckoutViewModel
            {
                Items = items,
                Subtotal = items.Sum(i => i.Subtotal),
                ShippingFee = CalculateShipping(ss.DeliveryMethod),
                DiscountAmount = 0,
                DeliveryMethod = ss.DeliveryMethod,
                PaymentMethod = ss.PaymentMethod,
                ReceiverName = ss.ReceiverName,
                ReceiverPhone = ss.ReceiverPhone,
                ReceiverEmail = ss.ReceiverEmail,
                ShippingAddress = ss.ShippingAddress,
                PickupBranchId = ss.PickupBranchId,
                SelectedBankCode = string.IsNullOrWhiteSpace(ss.SelectedBankCode)
                    ? banks.FirstOrDefault()?.Code
                    : ss.SelectedBankCode,
                CardHolder = ss.CardHolder,
                CardNumber = "",
                CardExpiry = ss.CardExpiry,
                Banks = banks,
                Branches = branches,
                SelectedProductIds = selectedIds ?? items.Select(i => i.ProductId).ToArray()
            };
            vm.Total = vm.Subtotal + vm.ShippingFee - vm.DiscountAmount;

            return View(vm);
        }

        // ============================================
        // POST: /Checkout (đặt hàng + thanh toán mô phỏng)
        // ============================================
        [HttpPost]
        public async Task<IActionResult> Index(CheckoutViewModel vm, CancellationToken ct)
        {
            // Lấy lại items hiện tại (không tin vào client)
            var items = _cart.GetCartItems();

            // Nếu người dùng chỉ chọn subset -> lọc lại theo SelectedProductIds
            if (vm.SelectedProductIds is { Length: > 0 })
            {
                var idSet = vm.SelectedProductIds.Distinct().ToHashSet();
                items = items.Where(i => idSet.Contains(i.ProductId)).ToList();
            }

            if (items.Count == 0)
            {
                TempData[SD.Temp_Info] = "Giỏ hàng trống. Vui lòng thêm sản phẩm trước khi thanh toán.";
                return RedirectToAction("Index", "Products");
            }

            // Lưu session checkout từ input user
            var ss = _cart.GetCheckoutSession() ?? new CartService.CheckoutSession();
            ss.DeliveryMethod = vm.DeliveryMethod;
            ss.PaymentMethod = vm.PaymentMethod;
            ss.ReceiverName = vm.ReceiverName?.Trim();
            ss.ReceiverPhone = vm.ReceiverPhone?.Trim();
            ss.ReceiverEmail = vm.ReceiverEmail?.Trim();
            ss.ShippingAddress = vm.ShippingAddress?.Trim();
            ss.PickupBranchId = vm.PickupBranchId;
            ss.SelectedBankCode = vm.SelectedBankCode;
            ss.CardHolder = vm.CardHolder?.Trim();
            ss.CardExpiry = vm.CardExpiry?.Trim();
            _cart.SaveCheckoutSession(ss);

            // Tính lại tổng tiền server-side theo subset
            vm.Items = items;
            vm.Subtotal = items.Sum(i => i.Subtotal);
            vm.ShippingFee = CalculateShipping(vm.DeliveryMethod);
            vm.DiscountAmount = 0;
            vm.Total = vm.Subtotal + vm.ShippingFee - vm.DiscountAmount;

            // ======================
            // VALIDATIONS phía server
            // ======================

            // Thông tin người nhận
            if (string.IsNullOrWhiteSpace(vm.ReceiverName))
                ModelState.AddModelError(nameof(vm.ReceiverName), "Vui lòng nhập tên người nhận.");
            if (string.IsNullOrWhiteSpace(vm.ReceiverPhone))
                ModelState.AddModelError(nameof(vm.ReceiverPhone), "Vui lòng nhập số điện thoại.");
            if (string.IsNullOrWhiteSpace(vm.ReceiverEmail))
                ModelState.AddModelError(nameof(vm.ReceiverEmail), "Vui lòng nhập email.");

            // Giao nhận: địa chỉ/chi nhánh tuỳ phương thức
            if (vm.DeliveryMethod == DeliveryMethod.HomeDelivery)
            {
                if (string.IsNullOrWhiteSpace(vm.ShippingAddress))
                    ModelState.AddModelError(nameof(vm.ShippingAddress), "Vui lòng nhập địa chỉ giao hàng.");
            }
            else // PickupAtStore
            {
                if (!vm.PickupBranchId.HasValue)
                    ModelState.AddModelError(nameof(vm.PickupBranchId), "Vui lòng chọn chi nhánh nhận xe.");
                else
                {
                    var branchOk = await _db.Branches.AnyAsync(b => b.IsActive && b.Id == vm.PickupBranchId.Value, ct);
                    if (!branchOk) ModelState.AddModelError(nameof(vm.PickupBranchId), "Chi nhánh không hợp lệ.");
                }
            }

            // Thanh toán
            string? last4 = null;
            if (vm.PaymentMethod == PaymentMethod.Card)
            {
                var digits = new string((vm.CardNumber ?? "").Where(char.IsDigit).ToArray());
                if (digits.Length < 12 || !LuhnValid(digits))
                    ModelState.AddModelError(nameof(vm.CardNumber), "Số thẻ không hợp lệ.");
                if (!CardExpiryValid(vm.CardExpiry))
                    ModelState.AddModelError(nameof(vm.CardExpiry), "Hạn thẻ không hợp lệ (MM/YY).");
                if (!string.IsNullOrEmpty(digits) && digits.Length >= 4)
                    last4 = digits[^4..];
            }

            if (vm.PaymentMethod is PaymentMethod.Card or PaymentMethod.BankTransfer)
            {
                var bankExists = await _db.Banks.AnyAsync(b => b.IsActive && b.Code == vm.SelectedBankCode, ct);
                if (!bankExists)
                    ModelState.AddModelError(nameof(vm.SelectedBankCode), "Vui lòng chọn ngân hàng hợp lệ.");
            }

            if (!ModelState.IsValid)
            {
                await HydrateListsAsync(vm, ct);
                return View(vm);
            }

            // ======================
            // Tạo đơn: transaction + concurrency
            // ======================
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                var ids = items.Select(i => i.ProductId).Distinct().ToArray();
                var products = await _db.Products
                    .Where(p => ids.Contains(p.Id) && p.IsPublished)
                    .ToListAsync(ct);

                var map = products.ToDictionary(p => p.Id);

                // Kiểm kho lần cuối
                foreach (var ci in items)
                {
                    if (!map.TryGetValue(ci.ProductId, out var prod))
                    {
                        ModelState.AddModelError(string.Empty, $"Sản phẩm ID {ci.ProductId} không còn bán.");
                        await HydrateListsAsync(vm, ct);
                        return View(vm);
                    }
                    if (prod.StockQuantity < ci.Quantity)
                    {
                        ModelState.AddModelError(string.Empty, $"'{prod.Name}' chỉ còn {prod.StockQuantity} chiếc.");
                        await HydrateListsAsync(vm, ct);
                        return View(vm);
                    }
                }

                var me = await _userManager.GetUserAsync(User);
                var order = new Order
                {
                    UserId = me!.Id,
                    OrderDate = DateTime.UtcNow,
                    Status = OrderStatus.Processing,
                    TotalAmount = vm.Total,
                    DeliveryMethod = vm.DeliveryMethod,
                    PickupBranchId = vm.DeliveryMethod == DeliveryMethod.PickupAtStore ? vm.PickupBranchId : null,
                    ShippingAddress = vm.DeliveryMethod == DeliveryMethod.HomeDelivery ? vm.ShippingAddress : null,
                    ReceiverName = vm.ReceiverName,
                    ReceiverPhone = vm.ReceiverPhone,
                    ReceiverEmail = vm.ReceiverEmail,
                    ShippingFee = vm.ShippingFee,
                    DiscountAmount = vm.DiscountAmount,
                    PaymentMethod = vm.PaymentMethod,
                    PaymentStatus = PaymentStatus.Pending
                };

                foreach (var ci in items)
                {
                    var prod = map[ci.ProductId];

                    order.OrderItems.Add(new OrderItem
                    {
                        ProductId = prod.Id,
                        Quantity = ci.Quantity,
                        UnitPrice = prod.Price
                    });

                    // Trừ kho + chạm RowVersion (để concurrency check)
                    prod.StockQuantity -= ci.Quantity;
                    prod.UpdatedAt = DateTime.UtcNow;
                    _db.Products.Update(prod);
                }

                // “Thanh toán mô phỏng”
                if (vm.PaymentMethod is PaymentMethod.Card or PaymentMethod.BankTransfer)
                {
                    order.PaymentStatus = PaymentStatus.Paid;
                    order.PaymentRef = $"MS{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";
                    if (last4 != null) order.CardLast4 = last4;
                }
                else
                {
                    order.PaymentStatus = PaymentStatus.Pending;
                }

                _db.Orders.Add(order);
                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                // ===== Email xác nhận (best-effort) =====
                try
                {
                    var bankName = await _db.Banks
                        .Where(b => b.Code == vm.SelectedBankCode)
                        .Select(b => b.Name)
                        .FirstOrDefaultAsync(ct);

                    string? branchName = null;
                    if (order.PickupBranchId.HasValue)
                        branchName = await _db.Branches
                            .Where(b => b.Id == order.PickupBranchId.Value)
                            .Select(b => b.Name)
                            .FirstOrDefaultAsync(ct);

                    var emailHtml = BuildPaymentEmailHtml(order, items, bankName, branchName);
                    var to = order.ReceiverEmail ?? me.Email ?? "";
                    if (!string.IsNullOrWhiteSpace(to))
                        await _emailSender.SendEmailAsync(to, SD.EmailSubject_PaymentConfirmation, emailHtml);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Gửi email xác nhận thanh toán thất bại (OrderId={OrderId})", /*OrderId*/ 0);
                }

                // Clear session/giỏ
                _cart.ClearCheckoutSession();
                _cart.RemovePurchasedItems(items.Select(i => i.ProductId));

                TempData[SD.Temp_Success] = "Đặt hàng thành công. Chúng tôi đã gửi email xác nhận.";
                return RedirectToAction("History", "Order");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await tx.RollbackAsync(ct);
                _logger.LogError(ex, "Lỗi cạnh tranh dữ liệu khi đặt hàng.");
                ModelState.AddModelError(string.Empty, "Một số sản phẩm vừa được cập nhật tồn kho. Vui lòng kiểm tra lại giỏ hàng.");
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(ct);
                _logger.LogError(ex, "Lỗi khi đặt hàng/thanh toán.");
                ModelState.AddModelError(string.Empty, "Không thể hoàn tất thanh toán. Vui lòng thử lại.");
            }

            await HydrateListsAsync(vm, ct);
            return View(vm);
        }

        // (Tùy chọn cho UI động) — báo giá nhanh khi đổi phương thức/chi nhánh
        // GET: /Checkout/Quote?method=HomeDelivery&selected=1,2,3
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

        private static decimal CalculateShipping(DeliveryMethod method)
            => method == DeliveryMethod.HomeDelivery ? 30000m : 0m;

        private async Task HydrateListsAsync(CheckoutViewModel vm, CancellationToken ct)
        {
            vm.Branches = await _db.Branches
                .Where(b => b.IsActive)
                .OrderBy(b => b.Name)
                .AsNoTracking()
                .ToListAsync(ct);

            vm.Banks = await _db.Banks
                .Where(b => b.IsActive)
                .OrderBy(b => b.SortOrder)
                .AsNoTracking()
                .ToListAsync(ct);

            if (string.IsNullOrWhiteSpace(vm.SelectedBankCode))
                vm.SelectedBankCode = vm.Banks.FirstOrDefault()?.Code;
        }

        private static bool LuhnValid(string digits)
        {
            var sum = 0; var alt = false;
            for (int i = digits.Length - 1; i >= 0; i--)
            {
                var n = digits[i] - '0';
                if (alt)
                {
                    n *= 2;
                    if (n > 9) n -= 9;
                }
                sum += n;
                alt = !alt;
            }
            return sum % 10 == 0;
        }

        private static bool CardExpiryValid(string? expiry)
        {
            if (string.IsNullOrWhiteSpace(expiry)) return false;
            // Hỗ trợ "MM/YY" hoặc "MM/YYYY"
            var parts = expiry.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length != 2) return false;
            if (!int.TryParse(parts[0], out var mm) || mm < 1 || mm > 12) return false;
            if (!int.TryParse(parts[1], out var yy)) return false;
            if (parts[1].Length == 2) yy += 2000;
            try
            {
                var lastDay = DateTime.DaysInMonth(yy, mm);
                var end = new DateTime(yy, mm, lastDay, 23, 59, 59, DateTimeKind.Utc);
                return end >= DateTime.UtcNow.AddMinutes(-1);
            }
            catch { return false; }
        }

        private static string BuildPaymentEmailHtml(Order order, List<CartItem> items, string? bankName, string? branchName)
        {
            var sb = new StringBuilder();
            sb.Append($@"
<table role='presentation' width='100%' cellpadding='0' cellspacing='0' style='background:#f5f7fb;padding:24px 0'>
  <tr>
    <td align='center'>
      <table role='presentation' width='640' cellpadding='0' cellspacing='0' style='background:#ffffff;border-radius:16px;box-shadow:0 10px 25px rgba(0,0,0,.06);overflow:hidden;font-family:system-ui,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#111827'>
        <tr>
          <td style='padding:28px 32px'>
            <div style='display:flex;align-items:center;gap:10px'>
              <div style='width:36px;height:36px;border-radius:10px;background:#2563eb;display:grid;place-items:center'>
                <span style='font-size:18px;color:#fff'>M</span>
              </div>
              <div style='font-weight:700;font-size:18px;color:#111827'>MotorShop</div>
            </div>
            <h1 style='margin:18px 0 8px;font-size:22px;line-height:28px'>Xác nhận đơn hàng #{order.Id}</h1>
            <p style='margin:0 0 16px;color:#374151'>Cảm ơn bạn đã đặt hàng. Thông tin đơn ở bên dưới.</p>
            <table cellpadding='0' cellspacing='0' width='100%' style='border-collapse:collapse'>
              <thead>
                <tr>
                  <th align='left'  style='padding:10px;border-bottom:1px solid #e5e7eb;font-size:13px;color:#6b7280'>Sản phẩm</th>
                  <th align='right' style='padding:10px;border-bottom:1px solid #e5e7eb;font-size:13px;color:#6b7280'>SL</th>
                  <th align='right' style='padding:10px;border-bottom:1px solid #e5e7eb;font-size:13px;color:#6b7280'>Đơn giá</th>
                  <th align='right' style='padding:10px;border-bottom:1px solid #e5e7eb;font-size:13px;color:#6b7280'>Tạm tính</th>
                </tr>
              </thead>
              <tbody>");

            foreach (var it in items)
            {
                var line = it.Subtotal.ToString("#,0");
                var unit = it.Price.ToString("#,0");
                sb.Append($@"
                <tr>
                  <td style='padding:10px 10px 6px 10px'>{HtmlEncoder.Default.Encode(it.ProductName)}</td>
                  <td align='right' style='padding:10px 10px 6px 10px'>{it.Quantity}</td>
                  <td align='right' style='padding:10px 10px 6px 10px'>{unit} ₫</td>
                  <td align='right' style='padding:10px 10px 6px 10px'>{line} ₫</td>
                </tr>");
            }

            sb.Append($@"
              </tbody>
            </table>

            <div style='margin:16px 0;height:1px;background:#e5e7eb'></div>

            <table width='100%' cellpadding='0' cellspacing='0' style='font-size:14px'>
              <tr>
                <td align='left' style='padding:6px 0;color:#6b7280'>Tạm tính</td>
                <td align='right' style='padding:6px 0;font-weight:600'>{items.Sum(i => i.Subtotal).ToString("#,0")} ₫</td>
              </tr>
              <tr>
                <td align='left' style='padding:6px 0;color:#6b7280'>Phí vận chuyển</td>
                <td align='right' style='padding:6px 0;font-weight:600'>{order.ShippingFee.ToString("#,0")} ₫</td>
              </tr>
              <tr>
                <td align='left' style='padding:6px 0;color:#6b7280'>Giảm giá</td>
                <td align='right' style='padding:6px 0;font-weight:600'>-{order.DiscountAmount.ToString("#,0")} ₫</td>
              </tr>
              <tr>
                <td align='left' style='padding:10px 0;font-size:16px;font-weight:700'>Tổng thanh toán</td>
                <td align='right' style='padding:10px 0;font-size:16px;font-weight:800;color:#111827'>{order.TotalAmount.ToString("#,0")} ₫</td>
              </tr>
            </table>

            <div style='margin:16px 0;height:1px;background:#e5e7eb'></div>

            <p style='margin:6px 0;color:#374151'><b>Phương thức giao nhận:</b> {(order.DeliveryMethod == DeliveryMethod.HomeDelivery ? "Giao tận nơi" : "Nhận tại cửa hàng")}</p>");

            if (order.DeliveryMethod == DeliveryMethod.HomeDelivery)
            {
                sb.Append($@"<p style='margin:4px 0;color:#374151'><b>Địa chỉ giao:</b> {HtmlEncoder.Default.Encode(order.ShippingAddress ?? "")}</p>");
            }
            else
            {
                var bn = string.IsNullOrWhiteSpace(branchName) ? $"#{order.PickupBranchId}" : branchName;
                sb.Append($@"<p style='margin:4px 0;color:#374151'><b>Chi nhánh nhận:</b> {HtmlEncoder.Default.Encode(bn)}</p>");
            }

            sb.Append($@"<p style='margin:4px 0;color:#374151'><b>Người nhận:</b> {HtmlEncoder.Default.Encode(order.ReceiverName ?? "")} — {HtmlEncoder.Default.Encode(order.ReceiverPhone ?? "")}</p>
            <p style='margin:4px 0 12px;color:#374151'><b>Thanh toán:</b> {order.PaymentMethod} — {(order.PaymentStatus == PaymentStatus.Paid ? "Đã thanh toán" : "Chờ thanh toán")}</p>");

            if (!string.IsNullOrWhiteSpace(order.PaymentRef))
                sb.Append($@"<p style='margin:0;color:#6b7280'>Mã giao dịch: <b>{HtmlEncoder.Default.Encode(order.PaymentRef)}</b></p>");
            if (!string.IsNullOrWhiteSpace(order.CardLast4))
                sb.Append($@"<p style='margin:0;color:#6b7280'>Thẻ: **** **** **** <b>{HtmlEncoder.Default.Encode(order.CardLast4)}</b></p>");
            if (!string.IsNullOrWhiteSpace(bankName))
                sb.Append($@"<p style='margin:0;color:#6b7280'>Ngân hàng: <b>{HtmlEncoder.Default.Encode(bankName)}</b></p>");

            sb.Append($@"
            <div style='margin:24px 0 4px;height:1px;background:#e5e7eb'></div>
            <p style='margin:0 0 4px;font-size:12px;color:#9ca3af'>&copy; {DateTime.UtcNow:yyyy} MotorShop.</p>
          </td>
        </tr>
      </table>
    </td>
  </tr>
</table>");
            return sb.ToString();
        }
    }
}
