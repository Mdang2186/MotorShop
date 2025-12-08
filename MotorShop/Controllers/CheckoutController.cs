// Controllers/CheckoutController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
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
        private readonly PaymentSettings _payCfg;

        // % đặt cọc khi nhận tại cửa hàng
        private const decimal DepositPercentPickup = 0.5m;

        public CheckoutController(
            ApplicationDbContext db,
            CartService cart,
            UserManager<ApplicationUser> userManager,
            IEmailSender emailSender,
            ILogger<CheckoutController> logger,
            IOptions<PaymentSettings> paymentOptions)
        {
            _db = db;
            _cart = cart;
            _userManager = userManager;
            _emailSender = emailSender;
            _logger = logger;
            _payCfg = paymentOptions.Value;
        }

        // =========================================
        // GET: /Checkout?selected=1,3,9
        // =========================================
        [HttpGet]
        public async Task<IActionResult> Index(string? selected, CancellationToken ct)
        {
            var items = _cart.GetCartItems();

            // Lọc theo danh sách sản phẩm được chọn (nếu có)
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

            // Lấy trạng thái checkout từ Session (giữ lại giữa GET / POST)
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

            // Tính đặt cọc & QR cho màn hình GET
            ApplyPaymentMeta(vm);

            return View(vm);
        }

        // =========================================
        // POST: /Checkout (đặt hàng)
        // =========================================
        [HttpPost]
        public async Task<IActionResult> Index(CheckoutViewModel vm, CancellationToken ct)
        {
            // 1. Lấy lại giỏ từ Session (không tin client gửi lên)
            var items = _cart.GetCartItems();

            // Nếu chỉ thanh toán một phần giỏ -> lọc lại
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

            // 2. Lưu session checkout (giữ dữ liệu nếu validate lỗi)
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

            // 3. Tính tiền từ server
            var calculatedSubtotal = items.Sum(i => i.Subtotal);
            var calculatedShippingFee = CalculateShipping(vm.DeliveryMethod);
            var calculatedDiscount = 0m;
            var calculatedTotal = calculatedSubtotal + calculatedShippingFee - calculatedDiscount;

            vm.Items = items;
            vm.Subtotal = calculatedSubtotal;
            vm.ShippingFee = calculatedShippingFee;
            vm.DiscountAmount = calculatedDiscount;
            vm.Total = calculatedTotal;

            // Tính đặt cọc + QR (trong trường hợp validate lỗi cũng cần hiển thị đúng)
            ApplyPaymentMeta(vm);

            // 4. Validate thêm phía server (bổ sung cho IValidatableObject)
            if (string.IsNullOrWhiteSpace(vm.ReceiverName))
                ModelState.AddModelError(nameof(vm.ReceiverName), "Vui lòng nhập tên người nhận.");
            if (string.IsNullOrWhiteSpace(vm.ReceiverPhone))
                ModelState.AddModelError(nameof(vm.ReceiverPhone), "Vui lòng nhập số điện thoại.");
            if (string.IsNullOrWhiteSpace(vm.ReceiverEmail))
                ModelState.AddModelError(nameof(vm.ReceiverEmail), "Vui lòng nhập email.");

            // Kiểm tra chi nhánh có tồn tại nếu chọn PickupAtStore
            if (vm.DeliveryMethod == DeliveryMethod.PickupAtStore && vm.PickupBranchId.HasValue)
            {
                var branchOk = await _db.Branches
                    .AnyAsync(b => b.IsActive && b.Id == vm.PickupBranchId.Value, ct);
                if (!branchOk)
                    ModelState.AddModelError(nameof(vm.PickupBranchId), "Chi nhánh không hợp lệ.");
            }

            // Thanh toán thẻ: kiểm tra số thẻ & hạn
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

            // ===========================
            // Chuyển khoản / QR / Đặt cọc
            // ===========================
            if (vm.PaymentMethod == PaymentMethod.BankTransfer ||
                vm.PaymentMethod == PaymentMethod.PayAtStore)
            {
                // Lấy danh sách bank đang active
                var activeBanksQuery = _db.Banks.Where(b => b.IsActive);
                var hasAnyBank = await activeBanksQuery.AnyAsync(ct);

                if (hasAnyBank)
                {
                    // Nếu chưa chọn code → auto set bank đầu tiên để không bị lỗi khó chịu
                    if (string.IsNullOrWhiteSpace(vm.SelectedBankCode))
                    {
                        var firstBank = await activeBanksQuery
                            .OrderBy(b => b.SortOrder)
                            .FirstOrDefaultAsync(ct);

                        if (firstBank != null)
                        {
                            vm.SelectedBankCode = firstBank.Code;
                            ss.SelectedBankCode = firstBank.Code;
                            _cart.SaveCheckoutSession(ss);
                        }
                        else
                        {
                            ModelState.AddModelError(nameof(vm.SelectedBankCode),
                                "Vui lòng chọn ngân hàng để chuyển khoản / nhận cọc.");
                        }
                    }
                    else
                    {
                        var bankExists = await activeBanksQuery
                            .AnyAsync(b => b.Code == vm.SelectedBankCode, ct);

                        if (!bankExists)
                        {
                            ModelState.AddModelError(nameof(vm.SelectedBankCode),
                                "Ngân hàng không hợp lệ.");
                        }
                    }
                }
                // Nếu KHÔNG có bản ghi bank nào trong DB:
                // => dùng QR tĩnh từ PaymentSettings, không chặn ModelState.
            }

            if (!ModelState.IsValid)
            {
                await HydrateListsAsync(vm, ct);
                return View(vm);
            }

            // 5. Tạo đơn hàng trong transaction
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                var ids = items.Select(i => i.ProductId).Distinct().ToArray();
                var products = await _db.Products
                    .Where(p => ids.Contains(p.Id) && p.IsPublished)
                    .ToListAsync(ct);

                var map = products.ToDictionary(p => p.Id);

                // Kiểm tra tồn kho
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

                    ShippingFee = calculatedShippingFee,
                    TotalAmount = calculatedTotal,
                    DiscountAmount = calculatedDiscount,

                    DeliveryMethod = vm.DeliveryMethod,
                    PickupBranchId = vm.DeliveryMethod == DeliveryMethod.PickupAtStore
                        ? vm.PickupBranchId
                        : null,
                    ShippingAddress = vm.DeliveryMethod == DeliveryMethod.HomeDelivery
                        ? vm.ShippingAddress
                        : null,

                    ReceiverName = vm.ReceiverName,
                    ReceiverPhone = vm.ReceiverPhone,
                    ReceiverEmail = vm.ReceiverEmail,
                    CustomerNote = vm.CustomerNote,

                    PaymentMethod = vm.PaymentMethod,
                    PaymentStatus = PaymentStatus.Pending,

                    DepositAmount = vm.DepositAmount,
                    DepositNote = vm.DepositNote,
                    SelectedBankCode = vm.SelectedBankCode
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

                    // Trừ kho
                    prod.StockQuantity -= ci.Quantity;
                    prod.UpdatedAt = DateTime.UtcNow;
                    _db.Products.Update(prod);
                }

                // Xử lý trạng thái thanh toán theo phương thức
                switch (vm.PaymentMethod)
                {
                    case PaymentMethod.Card:
                        // Giả lập đã thanh toán 100% qua cổng thẻ
                        order.PaymentStatus = PaymentStatus.Paid;
                        order.PaymentRef = BuildPaymentRef("CARD");
                        order.CardLast4 = last4;
                        break;

                    case PaymentMethod.BankTransfer:
                        // Chuyển khoản toàn bộ: chờ khách chuyển => Pending
                        order.PaymentStatus = PaymentStatus.Pending;
                        break;

                    case PaymentMethod.PayAtStore:
                        // Đặt cọc (50%) bằng chuyển khoản + thanh toán phần còn lại tại cửa hàng
                        order.PaymentStatus = PaymentStatus.Pending;
                        break;

                    case PaymentMethod.CashOnDelivery:
                        // Thanh toán khi nhận hàng
                        order.PaymentStatus = PaymentStatus.Pending;
                        break;
                }

                _db.Orders.Add(order);
                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                // 6. Gửi email xác nhận + QR nếu có
                try
                {
                    string? bankName = null;
                    if (!string.IsNullOrWhiteSpace(vm.SelectedBankCode))
                    {
                        bankName = await _db.Banks
                            .Where(b => b.Code == vm.SelectedBankCode)
                            .Select(b => b.Name)
                            .FirstOrDefaultAsync(ct);
                    }

                    string? branchName = null;
                    if (order.PickupBranchId.HasValue)
                    {
                        branchName = await _db.Branches
                            .Where(b => b.Id == order.PickupBranchId.Value)
                            .Select(b => b.Name)
                            .FirstOrDefaultAsync(ct);
                    }

                    // Tạo QR cho email nếu phương thức dùng QR
                    string? qrUrlForEmail = null;
                    if (PaymentUsesQr(order.PaymentMethod))
                    {
                        var amount = order.DepositAmount ?? order.TotalAmount;
                        qrUrlForEmail = BuildQrImageUrl(
                            amount,
                            order.ReceiverName ?? string.Empty,
                            order.Id);
                    }

                    var emailHtml = BuildPaymentEmailHtml(
                        order,
                        items,
                        bankName,
                        branchName,
                        qrUrlForEmail);

                    var to = order.ReceiverEmail ?? me.Email ?? "";
                    if (!string.IsNullOrWhiteSpace(to))
                        await _emailSender.SendEmailAsync(to, SD.EmailSubject_PaymentConfirmation, emailHtml);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Gửi email xác nhận thanh toán thất bại (OrderId={OrderId})", order.Id);
                }

                // 7. Clear giỏ
                _cart.ClearCheckoutSession();
                _cart.RemovePurchasedItems(items.Select(i => i.ProductId));

                TempData[SD.Temp_Success] = "Đặt hàng thành công. Chúng tôi đã gửi email xác nhận cho bạn.";
                return RedirectToAction(nameof(Success), new { id = order.Id });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await tx.RollbackAsync(ct);
                _logger.LogError(ex, "Lỗi cạnh tranh dữ liệu khi đặt hàng.");
                ModelState.AddModelError(string.Empty,
                    "Một số sản phẩm vừa được cập nhật tồn kho. Vui lòng kiểm tra lại giỏ hàng.");
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

        // =========================================
        // TRANG THÔNG BÁO ĐẶT HÀNG THÀNH CÔNG
        // GET: /Checkout/Success/5
        // =========================================
        [HttpGet]
        public async Task<IActionResult> Success(int id, CancellationToken ct)
        {
            var userId = _userManager.GetUserId(User);

            var order = await _db.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id, ct);

            if (order == null || order.UserId != userId)
            {
                return RedirectToAction("History", "Order");
            }

            var lines = order.OrderItems.Select(oi => new CheckoutLineVm
            {
                Name = oi.Product?.Name ?? "Sản phẩm",
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice,
                ImageUrl = null
            }).ToList();

            var subtotal = lines.Sum(l => l.Subtotal);

            var requiresDeposit = order.DepositAmount.HasValue && order.DepositAmount.Value > 0;
            var depositAmount = order.DepositAmount ?? 0m;
            var remaining = order.TotalAmount - depositAmount;

            string? branchName = null;
            if (order.PickupBranchId.HasValue)
            {
                branchName = await _db.Branches
                    .Where(b => b.Id == order.PickupBranchId.Value)
                    .Select(b => b.Name)
                    .FirstOrDefaultAsync(ct);
            }

            // ===== QR THANH TOÁN =====
            string? paymentQrUrl = null;
            string? paymentQrDesc = null;
            if (PaymentUsesQr(order.PaymentMethod))
            {
                var amount = requiresDeposit ? depositAmount : order.TotalAmount;
                paymentQrUrl = BuildQrImageUrl(
                    amount,
                    order.ReceiverName ?? string.Empty,
                    order.Id);

                paymentQrDesc = requiresDeposit
                    ? $"Quét mã QR để thanh toán tiền đặt cọc khoảng {depositAmount:#,0} ₫."
                    : $"Quét mã QR để thanh toán toàn bộ đơn hàng khoảng {order.TotalAmount:#,0} ₫.";
            }

            // ===== QR ĐƠN HÀNG (link tra cứu / chi tiết) =====
            // URL tuyệt đối tới trang xem chi tiết hoặc tra cứu đơn
            var orderUrl = Url.Action("Details", "Order",
                new { id = order.Id },
                protocol: Request.Scheme);

            var orderQrDesc =
                $"Quét mã để mở nhanh thông tin đơn hàng #{order.Id} trên MotorShop.";

            var vm = new OrderSuccessViewModel
            {
                OrderId = order.Id,
                OrderDate = order.OrderDate,

                ReceiverName = order.ReceiverName,
                ReceiverPhone = order.ReceiverPhone,
                ReceiverEmail = order.ReceiverEmail,

                DeliveryMethod = order.DeliveryMethod,
                ShippingAddress = order.ShippingAddress,
                BranchName = branchName,

                Subtotal = subtotal,
                ShippingFee = order.ShippingFee,
                DiscountAmount = order.DiscountAmount,
                Total = order.TotalAmount,

                RequiresDeposit = requiresDeposit,
                DepositAmount = depositAmount,
                RemainingAmount = remaining,

                PaymentMethod = order.PaymentMethod,
                PaymentStatus = order.PaymentStatus,
                PaymentMethodLabel = GetPaymentMethodLabel(order.PaymentMethod, order.DeliveryMethod),

                // QR THANH TOÁN
                QrPayload = paymentQrUrl,
                QrDescription = paymentQrDesc,

                // QR ĐƠN HÀNG
                OrderQrPayload = orderUrl,
                OrderQrDescription = orderQrDesc,

                Items = lines
            };

            return View(vm);
        }

        // =========================================
        // POST: /Checkout/ConfirmPayment/5
        // =========================================
        [HttpPost]
        public async Task<IActionResult> ConfirmPayment(int id, CancellationToken ct)
        {
            var userId = _userManager.GetUserId(User);

            var order = await _db.Orders
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId, ct);

            if (order == null)
            {
                TempData[SD.Temp_Error] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("History", "Order");
            }

            if (order.PaymentStatus != PaymentStatus.Paid)
            {
                order.PaymentStatus = PaymentStatus.Paid;

                if (string.IsNullOrWhiteSpace(order.PaymentRef))
                {
                    order.PaymentRef = BuildPaymentRef("QR");
                }

                await _db.SaveChangesAsync(ct);
            }

            TempData[SD.Temp_Success] = "Thanh toán thành công. Cảm ơn bạn!";
            return RedirectToAction(nameof(Success), new { id = order.Id });
        }

        // =========================================
        // API báo giá nhanh (đổi phương thức / chi nhánh)
        // =========================================
        [HttpGet]
        [Produces("application/json")]
        public IActionResult Quote(DeliveryMethod method, string? selected)
        {
            var items = _cart.GetCartItems();

            if (!string.IsNullOrWhiteSpace(selected))
            {
                var idSet = selected
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
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

        // ================== HELPERS ==================

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

        private void ApplyPaymentMeta(CheckoutViewModel vm)
        {
            vm.RequiresDeposit = vm.DeliveryMethod == DeliveryMethod.PickupAtStore;

            vm.DepositAmount = null;
            vm.DepositNote = null;
            vm.TransferQrUrl = null;

            if (vm.RequiresDeposit)
            {
                vm.DepositAmount = Math.Round(vm.Total * DepositPercentPickup, 0);
                vm.DepositNote =
                    $"Nhận xe tại cửa hàng: vui lòng đặt cọc {DepositPercentPickup:P0} đơn hàng " +
                    $"(khoảng {vm.DepositAmount.Value:#,0} ₫). Phần còn lại thanh toán tại quầy khi nhận xe.";
            }

            if (PaymentUsesQr(vm.PaymentMethod) && vm.Total > 0)
            {
                var amount = vm.DepositAmount ?? vm.Total;
                vm.TransferQrUrl = BuildQrImageUrl(
                    amount,
                    vm.ReceiverName ?? string.Empty,
                    null);
            }
        }

        private bool PaymentUsesQr(PaymentMethod method)
            => method == PaymentMethod.BankTransfer
               || method == PaymentMethod.PayAtStore;

        private string BuildPaymentRef(string prefix)
            => $"{prefix}{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";

        private string BuildQrImageUrl(decimal amount, string customerName, int? orderId)
        {
            var info = orderId.HasValue
                ? $"Thanh toan don MS#{orderId} {customerName}"
                : $"Thanh toan MotorShop {customerName}";

            var encodedInfo = Uri.EscapeDataString(info);
            var encodedName = Uri.EscapeDataString(_payCfg.AccountName ?? string.Empty);
            var intAmount = (int)Math.Round(amount, 0);

            return $"https://img.vietqr.io/image/{_payCfg.BankCode}-{_payCfg.AccountNumber}-compact2.png" +
                   $"?amount={intAmount}&addInfo={encodedInfo}&accountName={encodedName}";
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

        private string GetPaymentMethodLabel(PaymentMethod method, DeliveryMethod delivery)
        {
            return method switch
            {
                PaymentMethod.Card => "Thẻ ngân hàng (Visa/MasterCard/ATM)",
                PaymentMethod.BankTransfer => "Chuyển khoản / QR ngân hàng",
                PaymentMethod.CashOnDelivery => "Thanh toán khi nhận hàng (COD)",
                PaymentMethod.PayAtStore when delivery == DeliveryMethod.PickupAtStore
                    => "Đặt cọc & thanh toán tại cửa hàng",
                PaymentMethod.PayAtStore => "Thanh toán tại cửa hàng",
                _ => method.ToString()
            };
        }

        // ============ EMAIL TEMPLATE ============
        private static string BuildPaymentEmailHtml(
            Order order,
            List<CartItem> items,
            string? bankName,
            string? branchName,
            string? qrUrl)
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
              </tr>");

            if (order.DepositAmount.HasValue && order.DepositAmount.Value > 0)
            {
                var remain = order.TotalAmount - order.DepositAmount.Value;
                sb.Append($@"
              <tr>
                <td align='left' style='padding:6px 0;color:#6b7280'>Tiền đặt cọc</td>
                <td align='right' style='padding:6px 0;font-weight:600'>{order.DepositAmount.Value.ToString("#,0")} ₫</td>
              </tr>
              <tr>
                <td align='left' style='padding:6px 0;color:#6b7280'>Còn lại (dự kiến)</td>
                <td align='right' style='padding:6px 0;font-weight:600'>{remain.ToString("#,0")} ₫</td>
              </tr>");
            }

            sb.Append($@"
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
                sb.Append($@"<p style='margin:0 0 6px;color:#6b7280'>Ngân hàng: <b>{HtmlEncoder.Default.Encode(bankName)}</b></p>");

            if (!string.IsNullOrWhiteSpace(order.DepositNote))
                sb.Append($@"<p style='margin:4px 0 10px;color:#4b5563;font-size:13px'>{HtmlEncoder.Default.Encode(order.DepositNote)}</p>");

            if (!string.IsNullOrWhiteSpace(qrUrl))
            {
                var payNow = (order.DepositAmount ?? order.TotalAmount).ToString("#,0");
                sb.Append($@"
            <div style='margin:18px 0 4px;padding:14px;border-radius:12px;background:#eff6ff;border:1px solid #bfdbfe'>
              <p style='margin:0 0 8px;font-weight:600;color:#1d4ed8'>Mã QR thanh toán đơn hàng</p>
              <p style='margin:0 0 10px;font-size:13px;color:#4b5563'>
                Số tiền: <b>{payNow} ₫</b><br/>
                Vui lòng kiểm tra đúng tên chủ tài khoản, số tiền và nội dung chuyển khoản.
              </p>
              <div style='text-align:center'>
                <img src='{HtmlEncoder.Default.Encode(qrUrl)}'
                     alt='QR chuyển khoản'
                     style='max-width:220px;width:100%;border-radius:10px;border:1px solid #d1d5db' />
              </div>
            </div>");
            }

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
