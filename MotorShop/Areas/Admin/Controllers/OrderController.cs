using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MotorShop.Data;
using MotorShop.Models;
using MotorShop.Models.Enums;
using MotorShop.Utilities;
using System.Text.Json;
using System.Xml.Linq;
using ItBaseColor = iTextSharp.text.BaseColor;
using ItFont = iTextSharp.text.Font;
using PdfDocument = iTextSharp.text.Document;
using PdfPageSize = iTextSharp.text.PageSize;

namespace MotorShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Manager")]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;

        public OrderController(ApplicationDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        //======================================================================
        //  CONSTANTS: Risk / SLA / Saved Filter
        //======================================================================

        private const decimal RiskAmountThreshold = 50_000_000m; // 50 triệu
        private const int RiskPendingThreshold = 3;
        private static readonly TimeSpan RiskPendingWindow = TimeSpan.FromHours(24);

        private static readonly TimeSpan SlaPendingTimeout = TimeSpan.FromHours(24);
        private static readonly TimeSpan SlaProcessingTimeout = TimeSpan.FromHours(48);

        private const string SavedFilterCookiePrefix = "ms_order_filter_";

        public record OrderRiskInfo(bool IsRisky, string? Reason, int PendingCount24h);
        public record OrderSlaInfo(bool IsOverdue, string Level, double AgeHours);
        public record SavedOrderFilter(
            string Key,
            string Name,
            string? Keyword,
            OrderStatus? Status,
            DateTime? From,
            DateTime? To,
            bool OnlyCod);

        //======================================================================
        //  STATE MACHINE: QUY TẮC CHUYỂN TRẠNG THÁI
        //======================================================================

        private static readonly Dictionary<OrderStatus, OrderStatus[]> AllowedTransitions =
            new()
            {
                [OrderStatus.Pending] = new[]
                {
                    OrderStatus.Confirmed,
                    OrderStatus.Cancelled
                },
                [OrderStatus.Confirmed] = new[]
                {
                    OrderStatus.Processing,
                    OrderStatus.Cancelled
                },
                [OrderStatus.Processing] = new[]
                {
                    OrderStatus.Shipping,
                    OrderStatus.Cancelled
                },
                [OrderStatus.Shipping] = new[]
                {
                    OrderStatus.Delivered,
                    OrderStatus.Cancelled
                },
                [OrderStatus.Delivered] = new[]
                {
                    OrderStatus.Completed,
                    OrderStatus.Cancelled
                },
                [OrderStatus.Completed] = Array.Empty<OrderStatus>(),
                [OrderStatus.Cancelled] = Array.Empty<OrderStatus>()
            };

        private static bool CanChangeStatus(OrderStatus from, OrderStatus to)
        {
            if (from == to) return false;
            return AllowedTransitions.TryGetValue(from, out var allowed)
                   && allowed.Contains(to);
        }

        //======================================================================
        //  INDEX: LIST + FILTER + PAGING + RISK + SLA + SAVED FILTERS
        //======================================================================

        public async Task<IActionResult> Index(
            string? q,
            OrderStatus? status,
            DateTime? from,
            DateTime? to,
            bool onlyCod = false,
            int page = 1)
        {
            const int pageSize = 15;

            var query = _db.Orders
                .AsNoTracking()
                .Include(o => o.User)
                .Include(o => o.PickupBranch)
                .AsQueryable();

            // Thống kê status pipeline
            var statusCounts = await _db.Orders
                .AsNoTracking()
                .GroupBy(o => o.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count);
            ViewBag.StatusCounts = statusCounts;

            // Lọc dữ liệu
            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim().ToLower();
                query = query.Where(o =>
                    o.Id.ToString().Contains(q) ||
                    (o.ReceiverName != null && o.ReceiverName.ToLower().Contains(q)) ||
                    (o.ReceiverPhone != null && o.ReceiverPhone.Contains(q)));
            }

            if (status.HasValue)
            {
                query = query.Where(o => o.Status == status.Value);
            }

            if (from.HasValue)
            {
                var fUtc = from.Value.ToUniversalTime();
                query = query.Where(o => o.OrderDate >= fUtc);
            }

            if (to.HasValue)
            {
                var tUtc = to.Value.AddDays(1).ToUniversalTime();
                query = query.Where(o => o.OrderDate < tUtc);
            }

            if (onlyCod)
            {
                query = query.Where(o => o.PaymentMethod == PaymentMethod.CashOnDelivery);
            }

            // Phân trang
            var totalItems = await query.CountAsync();
            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .ThenByDescending(o => o.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            ViewBag.Q = q;
            ViewBag.Status = status;
            ViewBag.From = from?.ToString("yyyy-MM-dd");
            ViewBag.To = to?.ToString("yyyy-MM-dd");
            ViewBag.OnlyCod = onlyCod;

            // Risk & SLA cho từng đơn
            var riskDict = new Dictionary<int, OrderRiskInfo>();
            var slaDict = new Dictionary<int, OrderSlaInfo>();
            foreach (var order in orders)
            {
                riskDict[order.Id] = await ComputeRiskInfoAsync(order);
                slaDict[order.Id] = ComputeSlaInfo(order);
            }
            ViewBag.RiskDict = riskDict;
            ViewBag.SlaDict = slaDict;

            // Các bộ lọc đã lưu (cookie)
            ViewBag.SavedFilters = GetSavedFiltersFromCookies();

            return View(orders);
        }

        //======================================================================
        //  DETAILS: ĐƠN + THỐNG KÊ LỊCH SỬ KHÁCH + RISK + SLA + LIST SHIPPER
        //======================================================================

        public async Task<IActionResult> Details(int id)
        {
            var order = await _db.Orders
                .AsNoTracking()
                .Include(o => o.User)
                .Include(o => o.PickupBranch)
                .Include(o => o.Shipper)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                        .ThenInclude(p => p.Images)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            ViewBag.RiskInfo = await ComputeRiskInfoAsync(order);
            ViewBag.SlaInfo = ComputeSlaInfo(order);
            ViewBag.CustomerStats = await ComputeCustomerHistoryAsync(order);

            // ➜ danh sách đơn vị giao hàng để render dropdown
            ViewBag.Shippers = await _db.Shippers
                .AsNoTracking()
                .OrderBy(s => s.Name)
                .ToListAsync();

            return View(order);
        }

        //======================================================================
        //  UPDATE STATUS: STATE MACHINE + PAYMENT LOGIC + SHIPPER REQUIRED
        //======================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, OrderStatus status, int? shipperId, string? returnUrl = null)
        {
            var order = await _db.Orders
                .Include(o => o.Shipper)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                TempData[SD.Temp_Error] = "Đơn hàng không tồn tại.";
                return RedirectToAction(nameof(Index));
            }

            var oldStatus = order.Status;

            // Kiểm tra workflow
            if (!CanChangeStatus(oldStatus, status))
            {
                TempData[SD.Temp_Error] =
                    $"Không thể chuyển trạng thái từ {oldStatus} sang {status}. " +
                    "Vui lòng đi đúng quy trình (Pending → Confirmed → Processing → Shipping → Delivered → Completed).";

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                return RedirectToAction(nameof(Details), new { id });
            }

            // ===== BẮT BUỘC CHỌN ĐƠN VỊ SHIPPER CHO ĐƠN GIAO TẬN NƠI =====
            // Quy ước:
            //  - Chỉ bắt buộc khi: giao tận nơi (HomeDelivery) và trạng thái mới >= Shipping
            //  - PickupAtStore thì không cần Shipper
            var requiresShipper =
                order.DeliveryMethod == DeliveryMethod.HomeDelivery &&
                (status == OrderStatus.Shipping
                 || status == OrderStatus.Delivered
                 || status == OrderStatus.Completed);

            if (requiresShipper)
            {
                if (!shipperId.HasValue || shipperId.Value <= 0)
                {
                    TempData[SD.Temp_Error] = "Vui lòng chọn đơn vị giao hàng (shipper) trước khi chuyển sang trạng thái giao hàng.";
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        return Redirect(returnUrl);

                    return RedirectToAction(nameof(Details), new { id });
                }

                // Kiểm tra shipper tồn tại
                var shipperExists = await _db.Shippers
                    .AsNoTracking()
                    .AnyAsync(s => s.Id == shipperId.Value);

                if (!shipperExists)
                {
                    TempData[SD.Temp_Error] = "Đơn vị giao hàng không hợp lệ.";
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        return Redirect(returnUrl);

                    return RedirectToAction(nameof(Details), new { id });
                }

                // Gán vào đơn
                order.ShipperId = shipperId.Value;
            }
            else
            {
                // Nếu admin chuyển ngược về Processing/Pending → có thể bỏ shipper đi
                if (status == OrderStatus.Pending || status == OrderStatus.Confirmed || status == OrderStatus.Processing)
                {
                    order.ShipperId = null;
                }
            }

            // COD: khi Delivered/Completed thì auto Paid nếu đang Pending
            if ((status == OrderStatus.Delivered || status == OrderStatus.Completed)
                && order.PaymentStatus == PaymentStatus.Pending
                && order.PaymentMethod == PaymentMethod.CashOnDelivery)
            {
                order.PaymentStatus = PaymentStatus.Paid;
            }

            // Hủy đơn → nếu muốn hoàn kho thì TODO ở đây
            if (status == OrderStatus.Cancelled && oldStatus != OrderStatus.Cancelled)
            {
                // TODO: hoàn kho từ OrderItems nếu cần
            }

            order.Status = status;
            await _db.SaveChangesAsync();

            TempData[SD.Temp_Success] = $"Đã cập nhật đơn hàng #{id} sang trạng thái {status}.";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction(nameof(Details), new { id });
        }

        //======================================================================
        //  PRINT INVOICE: HOÁ ĐƠN / PHIẾU GIAO XE (PDF)
        //======================================================================

        [HttpGet]
        public async Task<IActionResult> PrintInvoice(int id)
        {
            var order = await _db.Orders
                .AsNoTracking()
                .Include(o => o.User)
                .Include(o => o.PickupBranch)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            using var ms = new MemoryStream();
            var doc = new PdfDocument(PdfPageSize.A4, 36, 36, 60, 40);
            PdfWriter.GetInstance(doc, ms);
            doc.Open();

            // Font tiếng Việt
            string fontPath = Path.Combine(_env.WebRootPath, "fonts", "arial.ttf");
            if (!System.IO.File.Exists(fontPath))
            {
                fontPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Fonts),
                    "arial.ttf");
            }

            var bf = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);

            var colorDark = new ItBaseColor(15, 23, 42);
            var colorGray = new ItBaseColor(100, 116, 139);
            var colorPrimary = new ItBaseColor(37, 99, 235);

            var fontTitle = new ItFont(bf, 18, ItFont.BOLD, colorPrimary);
            var fontHeader = new ItFont(bf, 11, ItFont.BOLD, colorDark);
            var fontNormal = new ItFont(bf, 10, ItFont.NORMAL, colorDark);
            var fontMuted = new ItFont(bf, 9, ItFont.NORMAL, colorGray);

            // HEADER: logo + info
            var headerTable = new PdfPTable(2) { WidthPercentage = 100 };
            headerTable.SetWidths(new float[] { 1.5f, 2.5f });

            var shopNamePara = new Paragraph("MOTORSHOP FUTURE", fontTitle)
            {
                SpacingAfter = 2f
            };
            var shopSub = new Paragraph("Showroom xe máy - phụ tùng cao cấp", fontMuted);

            var cellLeft = new PdfPCell
            {
                Border = PdfPCell.NO_BORDER,
                Padding = 0
            };
            cellLeft.AddElement(shopNamePara);
            cellLeft.AddElement(shopSub);

            var branchName = order.PickupBranch?.Name ?? "Chi nhánh mặc định";
            var branchAddr = order.PickupBranch?.Address ?? "Đang cập nhật địa chỉ";

            var info = new Paragraph(
                $"Chi nhánh: {branchName}\n" +
                $"Địa chỉ: {branchAddr}\n" +
                $"Điện thoại: 1900 1234\n" +
                $"Website: motorshop.local",
                fontMuted);

            var cellRight = new PdfPCell
            {
                Border = PdfPCell.NO_BORDER,
                HorizontalAlignment = Element.ALIGN_RIGHT,
                Padding = 0
            };
            cellRight.AddElement(info);

            headerTable.AddCell(cellLeft);
            headerTable.AddCell(cellRight);
            doc.Add(headerTable);

            doc.Add(new Paragraph("\n"));

            // TIÊU ĐỀ HOÁ ĐƠN
            var title = new Paragraph("HÓA ĐƠN BÁN HÀNG / PHIẾU GIAO XE", fontTitle)
            {
                Alignment = Element.ALIGN_CENTER,
                SpacingAfter = 10f
            };
            doc.Add(title);

            // THÔNG TIN ĐƠN + KHÁCH
            var infoTable = new PdfPTable(2) { WidthPercentage = 100 };
            infoTable.SetWidths(new float[] { 1.4f, 1.6f });

            void AddInfoRow(string label, string value, bool right = false)
            {
                var phrase = new Phrase($"{label}: {value}", fontNormal);
                var c = new PdfPCell(phrase)
                {
                    Border = PdfPCell.NO_BORDER,
                    PaddingBottom = 3f,
                    HorizontalAlignment = right ? Element.ALIGN_RIGHT : Element.ALIGN_LEFT
                };
                infoTable.AddCell(c);
            }

            var orderDateLocal = NormalizeToUtc(order.OrderDate).ToLocalTime();

            AddInfoRow($"Mã đơn #{order.Id}", $"Ngày: {orderDateLocal:HH:mm dd/MM/yyyy}");
            AddInfoRow("PT thanh toán", $"{order.PaymentMethod} / {order.PaymentStatus}", true);

            var customerName = order.ReceiverName ?? order.User?.FullName ?? "Khách lẻ";
            var customerPhone = order.ReceiverPhone ?? "Đang cập nhật";
            var customerAddress = "Địa chỉ giao: đang cập nhật";

            AddInfoRow("Khách hàng", customerName);
            AddInfoRow("Số điện thoại", customerPhone, true);
            AddInfoRow("Địa chỉ giao", customerAddress);
            AddInfoRow("Trạng thái", order.Status.ToString(), true);

            doc.Add(infoTable);
            doc.Add(new Paragraph("\n"));

            // BẢNG SẢN PHẨM
            var itemsTable = new PdfPTable(6) { WidthPercentage = 100 };
            itemsTable.SetWidths(new float[] { 0.8f, 3f, 1.2f, 1.2f, 1.2f, 1.3f });

            void AddHeaderCell(string text)
            {
                var cell = new PdfPCell(new Phrase(text, fontHeader))
                {
                    BackgroundColor = new ItBaseColor(226, 232, 240),
                    Padding = 5,
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    VerticalAlignment = Element.ALIGN_MIDDLE
                };
                itemsTable.AddCell(cell);
            }

            AddHeaderCell("STT");
            AddHeaderCell("Sản phẩm");
            AddHeaderCell("Số lượng");
            AddHeaderCell("Đơn giá");
            AddHeaderCell("Giảm");
            AddHeaderCell("Thành tiền");

            int stt = 1;
            decimal subTotal = 0m;

            foreach (var item in order.OrderItems.OrderBy(i => i.Id))
            {
                var name = item.Product?.Name ?? $"SP #{item.ProductId}";
                var qty = item.Quantity;
                var price = item.UnitPrice;
                var discount = 0m;
                var lineTotal = qty * price;

                subTotal += lineTotal;

                itemsTable.AddCell(new PdfPCell(new Phrase(stt.ToString(), fontNormal))
                {
                    Padding = 4,
                    HorizontalAlignment = Element.ALIGN_CENTER
                });
                itemsTable.AddCell(new PdfPCell(new Phrase(name, fontNormal)) { Padding = 4 });
                itemsTable.AddCell(new PdfPCell(new Phrase(qty.ToString(), fontNormal))
                {
                    Padding = 4,
                    HorizontalAlignment = Element.ALIGN_CENTER
                });
                itemsTable.AddCell(new PdfPCell(new Phrase(price.ToString("N0") + " ₫", fontNormal))
                {
                    Padding = 4,
                    HorizontalAlignment = Element.ALIGN_RIGHT
                });
                itemsTable.AddCell(new PdfPCell(new Phrase(discount.ToString("N0") + " ₫", fontNormal))
                {
                    Padding = 4,
                    HorizontalAlignment = Element.ALIGN_RIGHT
                });
                itemsTable.AddCell(new PdfPCell(new Phrase(lineTotal.ToString("N0") + " ₫", fontNormal))
                {
                    Padding = 4,
                    HorizontalAlignment = Element.ALIGN_RIGHT
                });

                stt++;
            }

            doc.Add(itemsTable);
            doc.Add(new Paragraph("\n"));

            // TỔNG TIỀN
            var totalTable = new PdfPTable(2)
            {
                WidthPercentage = 40,
                HorizontalAlignment = Element.ALIGN_RIGHT
            };
            totalTable.SetWidths(new float[] { 1.2f, 1.2f });

            void AddTotalRow(string label, decimal amount, bool bold = false)
            {
                var f = bold ? fontHeader : fontNormal;

                totalTable.AddCell(new PdfPCell(new Phrase(label, f))
                {
                    Border = PdfPCell.NO_BORDER,
                    HorizontalAlignment = Element.ALIGN_RIGHT,
                    Padding = 3
                });
                totalTable.AddCell(new PdfPCell(new Phrase(amount.ToString("N0") + " ₫", f))
                {
                    Border = PdfPCell.NO_BORDER,
                    HorizontalAlignment = Element.ALIGN_RIGHT,
                    Padding = 3
                });
            }

            var shipping = order.ShippingFee;
            var finalTotal = order.TotalAmount;

            AddTotalRow("Tạm tính", subTotal, false);
            AddTotalRow("Phí giao / đăng ký", shipping, false);
            AddTotalRow("Tổng phải thanh toán", finalTotal, true);

            doc.Add(totalTable);

            // CHỮ KÝ
            doc.Add(new Paragraph("\n\n"));

            var signTable = new PdfPTable(2) { WidthPercentage = 100 };
            signTable.SetWidths(new float[] { 1f, 1f });

            signTable.AddCell(new PdfPCell(new Phrase("Khách hàng\n\n\n\n(Ký & ghi rõ họ tên)", fontMuted))
            {
                Border = PdfPCell.NO_BORDER,
                HorizontalAlignment = Element.ALIGN_CENTER
            });
            signTable.AddCell(new PdfPCell(new Phrase("Đại diện MotorShop\n\n\n\n(Ký, đóng dấu)", fontMuted))
            {
                Border = PdfPCell.NO_BORDER,
                HorizontalAlignment = Element.ALIGN_CENTER
            });

            doc.Add(signTable);
            doc.Close();

            var fileName = $"Invoice_{order.Id}_{DateTime.Now:yyyyMMddHHmm}.pdf";
            return File(ms.ToArray(), "application/pdf", fileName);
        }

        //======================================================================
        //  LƯU / ÁP DỤNG BỘ LỌC
        //======================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveFilter(
            string name,
            string? q,
            OrderStatus? status,
            DateTime? from,
            DateTime? to,
            bool onlyCod = false)
        {
            name = (name ?? "").Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData[SD.Temp_Error] = "Tên bộ lọc không được để trống.";
                return RedirectToAction(nameof(Index), new { q, status, from, to, onlyCod });
            }

            var key = Guid.NewGuid().ToString("N");

            var filter = new SavedOrderFilter(
                key,
                name,
                q,
                status,
                from,
                to,
                onlyCod);

            var json = JsonSerializer.Serialize(filter);

            Response.Cookies.Append(
                SavedFilterCookiePrefix + key,
                json,
                new CookieOptions
                {
                    HttpOnly = false,
                    Secure = Request.IsHttps,
                    Expires = DateTimeOffset.UtcNow.AddDays(30),
                    SameSite = SameSiteMode.Lax
                });

            TempData[SD.Temp_Success] = "Đã lưu bộ lọc đơn hàng.";

            return RedirectToAction(nameof(Index), new { q, status, from, to, onlyCod });
        }

        [HttpGet]
        public IActionResult ApplyFilter(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return RedirectToAction(nameof(Index));

            if (!Request.Cookies.TryGetValue(SavedFilterCookiePrefix + key, out var json))
            {
                TempData[SD.Temp_Error] = "Bộ lọc đã hết hạn hoặc không tồn tại.";
                return RedirectToAction(nameof(Index));
            }

            SavedOrderFilter? filter;
            try
            {
                filter = JsonSerializer.Deserialize<SavedOrderFilter>(json);
            }
            catch
            {
                TempData[SD.Temp_Error] = "Không đọc được bộ lọc. Vui lòng lưu lại.";
                return RedirectToAction(nameof(Index));
            }

            if (filter == null)
                return RedirectToAction(nameof(Index));

            return RedirectToAction(nameof(Index), new
            {
                q = filter.Keyword,
                status = filter.Status,
                from = filter.From?.ToString("yyyy-MM-dd"),
                to = filter.To?.ToString("yyyy-MM-dd"),
                onlyCod = filter.OnlyCod
            });
        }

        private List<SavedOrderFilter> GetSavedFiltersFromCookies()
        {
            var list = new List<SavedOrderFilter>();

            foreach (var cookie in Request.Cookies)
            {
                if (!cookie.Key.StartsWith(SavedFilterCookiePrefix, StringComparison.OrdinalIgnoreCase))
                    continue;

                try
                {
                    var filter = JsonSerializer.Deserialize<SavedOrderFilter>(cookie.Value);
                    if (filter != null) list.Add(filter);
                }
                catch
                {
                    // ignore
                }
            }

            return list.OrderBy(f => f.Name).ToList();
        }

        //======================================================================
        //  HELPERS
        //======================================================================

        private static DateTime NormalizeToUtc(DateTime dt)
        {
            return dt.Kind == DateTimeKind.Utc
                ? dt
                : DateTime.SpecifyKind(dt, DateTimeKind.Utc);
        }

        private async Task<OrderRiskInfo> ComputeRiskInfoAsync(Order order)
        {
            var isHighAmount = order.TotalAmount >= RiskAmountThreshold;

            var nowUtc = DateTime.UtcNow;
            var sinceUtc = nowUtc - RiskPendingWindow;

            int pendingCount = 0;

            if (!string.IsNullOrWhiteSpace(order.ReceiverPhone))
            {
                pendingCount = await _db.Orders
                    .AsNoTracking()
                    .Where(o =>
                        o.Id != order.Id &&
                        o.Status == OrderStatus.Pending &&
                        o.OrderDate >= sinceUtc &&
                        o.ReceiverPhone == order.ReceiverPhone)
                    .CountAsync();
            }

            var tooManyPending = pendingCount >= RiskPendingThreshold;
            bool isRisky = isHighAmount || tooManyPending;

            string? reason = null;
            if (isHighAmount) reason = "Tổng tiền lớn";
            if (tooManyPending)
            {
                reason = reason == null
                    ? "Nhiều đơn Pending gần đây"
                    : $"{reason}, nhiều đơn Pending gần đây";
            }

            return new OrderRiskInfo(isRisky, reason, pendingCount);
        }

        private OrderSlaInfo ComputeSlaInfo(Order order)
        {
            var nowUtc = DateTime.UtcNow;
            var orderUtc = NormalizeToUtc(order.OrderDate);
            var age = nowUtc - orderUtc;
            var hours = age.TotalHours;

            bool overdue = false;
            string level = "normal";

            if (order.Status == OrderStatus.Pending && age > SlaPendingTimeout)
            {
                overdue = true;
                level = "pending";
            }
            else if (order.Status == OrderStatus.Processing && age > SlaProcessingTimeout)
            {
                overdue = true;
                level = "processing";
            }

            return new OrderSlaInfo(overdue, level, hours);
        }

        private async Task<object> ComputeCustomerHistoryAsync(Order order)
        {
            if (string.IsNullOrWhiteSpace(order.ReceiverPhone))
            {
                return new
                {
                    TotalOrders = 0,
                    TotalSpent = 0m,
                    LastOrder = (Order?)null
                };
            }

            var phone = order.ReceiverPhone;

            var query = _db.Orders
                .AsNoTracking()
                .Where(o =>
                    o.Id != order.Id &&
                    o.ReceiverPhone == phone);

            var totalOrders = await query.CountAsync();

            var totalSpent = await query
                .Where(o => o.Status == OrderStatus.Completed || o.Status == OrderStatus.Delivered)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

            var lastOrder = await query
                .OrderByDescending(o => o.OrderDate)
                .ThenByDescending(o => o.Id)
                .FirstOrDefaultAsync();

            return new
            {
                TotalOrders = totalOrders,
                TotalSpent = totalSpent,
                LastOrder = lastOrder
            };
        }
    }
}
