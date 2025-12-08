using ClosedXML.Excel;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MotorShop.Data;
using MotorShop.Models;
using MotorShop.Models.Enums;
using MotorShop.Utilities;
using MotorShop.ViewModels;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

// ===== ALIAS cho iTextSharp, tránh trùng tên =====
using PdfDocument = iTextSharp.text.Document;
using PdfParagraph = iTextSharp.text.Paragraph;
using ItFont = iTextSharp.text.Font;
using ItBaseColor = iTextSharp.text.BaseColor;

namespace MotorShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Manager")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public DashboardController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            IEmailSender emailSender,
            IWebHostEnvironment webHostEnvironment)
        {
            _db = db;
            _userManager = userManager;
            _emailSender = emailSender;
            _webHostEnvironment = webHostEnvironment;
        }

        // ============= 1. MÀN HÌNH CHÍNH =============
        public async Task<IActionResult> Index(DateTime? from, DateTime? to)
        {
            var vm = await BuildDashboardViewModel(from, to);
            return View(vm);
        }

        // ============= 2. XUẤT EXCEL (NÂNG CAO) =============
        [HttpPost]
        public async Task<IActionResult> ExportSummaryExcel(DateTime? from, DateTime? to)
        {
            var vm = await BuildDashboardViewModel(from, to);

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("BaoCaoTongQuan");

            // Header
            ws.Cell(1, 1).Value = "BÁO CÁO TỔNG QUAN MOTORSHOP";
            ws.Range(1, 1, 1, 4).Merge()
                .Style.Font.SetBold()
                .Font.SetFontSize(16)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(2, 1).Value = $"Thời gian: {FormatRange(vm.From, vm.To)}";
            ws.Range(2, 1, 2, 4).Merge()
                .Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            int row = 4;

            // Bảng chỉ số
            var headerStyle = wb.Style;
            headerStyle.Font.Bold = true;
            headerStyle.Fill.BackgroundColor = XLColor.LightGray;
            headerStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(row, 1).Value = "CHỈ SỐ TỔNG HỢP";
            ws.Range(row, 1, row, 2).Merge().Style = headerStyle;
            row++;

            ws.Cell(row, 1).Value = "Tiêu chí";
            ws.Cell(row, 2).Value = "Giá trị";
            ws.Range(row, 1, row, 2).Style
                .Font.SetBold()
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            row++;

            AddExcelRow(ws, ref row, "Doanh thu", vm.TotalRevenueInRange, "#,##0 ₫");
            AddExcelRow(ws, ref row, "Tổng đơn hàng", vm.TotalOrdersInRange);
            AddExcelRow(ws, ref row, "Đơn thành công", vm.SuccessfulOrdersInRange);
            AddExcelRow(ws, ref row, "Đơn hủy", vm.CancelledOrdersInRange);
            AddExcelRow(ws, ref row, "Khách hàng mới", vm.NewCustomersInRange);

            // Top sản phẩm
            row += 2;
            ws.Cell(row, 1).Value = "TOP SẢN PHẨM BÁN CHẠY";
            ws.Range(row, 1, row, 3).Merge().Style = headerStyle;
            row++;

            ws.Cell(row, 1).Value = "Sản phẩm";
            ws.Cell(row, 2).Value = "Số lượng bán";
            ws.Cell(row, 3).Value = "Doanh thu";
            ws.Range(row, 1, row, 3).Style
                .Border.SetBottomBorder(XLBorderStyleValues.Thin)
                .Font.SetBold();
            row++;

            foreach (var p in vm.TopProducts)
            {
                ws.Cell(row, 1).Value = p.ProductName;
                ws.Cell(row, 2).Value = p.Quantity;
                ws.Cell(row, 3).Value = p.Revenue;
                ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0 ₫";
                row++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            var content = stream.ToArray();

            return File(
                content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"BaoCao_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
        }

        private void AddExcelRow(IXLWorksheet ws, ref int row, string label, object value, string format = "")
        {
            ws.Cell(row, 1).Value = label;

            if (value is decimal or double or int or long)
            {
                ws.Cell(row, 2).Value = Convert.ToDouble(value);
            }
            else
            {
                ws.Cell(row, 2).Value = value?.ToString() ?? "";
            }

            if (!string.IsNullOrEmpty(format))
            {
                ws.Cell(row, 2).Style.NumberFormat.Format = format;
            }
            row++;
        }

        // ============= 3. XUẤT PDF (NÂNG CAO, TIẾNG VIỆT) =============
        [HttpPost]
        public async Task<IActionResult> ExportSummaryPdf(DateTime? from, DateTime? to)
        {
            var vm = await BuildDashboardViewModel(from, to);

            using var ms = new MemoryStream();
            var doc = new PdfDocument(PageSize.A4, 25, 25, 30, 30);
            var writer = PdfWriter.GetInstance(doc, ms);
            doc.Open();

            // Font tiếng Việt
            string fontPath = Path.Combine(_webHostEnvironment.WebRootPath, "fonts", "arial.ttf");
            if (!System.IO.File.Exists(fontPath))
            {
                fontPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Fonts),
                    "arial.ttf");
            }

            BaseFont bf = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);

            var colorBlue = new ItBaseColor(0, 102, 204);
            var colorWhite = new ItBaseColor(255, 255, 255);
            var colorBlack = new ItBaseColor(0, 0, 0);
            var colorDarkGray = new ItBaseColor(64, 64, 64);
            var colorGray = new ItBaseColor(128, 128, 128);

            ItFont fontTitle = new ItFont(bf, 16, ItFont.BOLD, colorBlue);
            ItFont fontHeader = new ItFont(bf, 12, ItFont.BOLD, colorWhite);
            ItFont fontNormal = new ItFont(bf, 11, ItFont.NORMAL, colorBlack);
            ItFont fontBold = new ItFont(bf, 11, ItFont.BOLD, colorBlack);

            // Tiêu đề
            var pTitle = new PdfParagraph("BÁO CÁO TỔNG QUAN MOTORSHOP", fontTitle);
            pTitle.Alignment = Element.ALIGN_CENTER;
            doc.Add(pTitle);

            var pDate = new PdfParagraph($"Thời gian: {FormatRange(vm.From, vm.To)}", fontNormal)
            {
                Alignment = Element.ALIGN_CENTER,
                SpacingAfter = 20f
            };
            doc.Add(pDate);

            // Bảng 1: Thống kê chung
            PdfPTable table1 = new PdfPTable(2) { WidthPercentage = 100 };
            table1.SetWidths(new float[] { 3f, 1f });

            AddPdfCell(table1, "Chỉ tiêu", fontHeader, colorDarkGray);
            AddPdfCell(table1, "Giá trị", fontHeader, colorDarkGray);

            AddPdfCell(table1, "Doanh thu", fontNormal);
            AddPdfCell(table1, vm.TotalRevenueInRange.ToString("N0") + " đ",
                fontBold, null, Element.ALIGN_RIGHT);

            AddPdfCell(table1, "Tổng đơn hàng", fontNormal);
            AddPdfCell(table1, vm.TotalOrdersInRange.ToString(),
                fontNormal, null, Element.ALIGN_RIGHT);

            AddPdfCell(table1, "Đơn thành công", fontNormal);
            AddPdfCell(table1, vm.SuccessfulOrdersInRange.ToString(),
                fontNormal, null, Element.ALIGN_RIGHT);

            AddPdfCell(table1, "Đơn hủy", fontNormal);
            AddPdfCell(table1, vm.CancelledOrdersInRange.ToString(),
                fontNormal, null, Element.ALIGN_RIGHT);

            AddPdfCell(table1, "Khách hàng mới", fontNormal);
            AddPdfCell(table1, vm.NewCustomersInRange.ToString(),
                fontNormal, null, Element.ALIGN_RIGHT);

            doc.Add(table1);
            doc.Add(new PdfParagraph("\n", fontNormal));

            // Bảng 2: Top sản phẩm
            doc.Add(new PdfParagraph("Top sản phẩm bán chạy:", fontBold));
            doc.Add(new PdfParagraph(" ", fontNormal));

            PdfPTable table2 = new PdfPTable(3) { WidthPercentage = 100 };
            table2.SetWidths(new float[] { 5f, 1.5f, 2.5f });

            AddPdfCell(table2, "Tên sản phẩm", fontHeader, colorDarkGray);
            AddPdfCell(table2, "Số lượng", fontHeader, colorDarkGray);
            AddPdfCell(table2, "Doanh thu", fontHeader, colorDarkGray);

            foreach (var p in vm.TopProducts)
            {
                AddPdfCell(table2, p.ProductName, fontNormal);
                AddPdfCell(table2, p.Quantity.ToString(), fontNormal, null, Element.ALIGN_CENTER);
                AddPdfCell(table2, p.Revenue.ToString("N0") + " đ", fontNormal, null, Element.ALIGN_RIGHT);
            }

            doc.Add(table2);

            // Footer
            var pFooter = new PdfParagraph(
                $"\nNgười xuất: {_userManager.GetUserName(User)} - Ngày: {DateTime.Now:dd/MM/yyyy HH:mm}",
                new ItFont(bf, 9, ItFont.ITALIC, colorGray))
            {
                Alignment = Element.ALIGN_RIGHT
            };
            doc.Add(pFooter);

            doc.Close();
            writer.Close();

            return File(ms.ToArray(), "application/pdf", $"BaoCao_{DateTime.Now:yyyyMMdd}.pdf");
        }

        private void AddPdfCell(
            PdfPTable table,
            string text,
            ItFont font,
            ItBaseColor bgColor = null,
            int align = Element.ALIGN_LEFT)
        {
            PdfPCell cell = new PdfPCell(new Phrase(text, font))
            {
                Padding = 6,
                HorizontalAlignment = align,
                VerticalAlignment = Element.ALIGN_MIDDLE
            };
            if (bgColor != null) cell.BackgroundColor = bgColor;

            table.AddCell(cell);
        }

        // ============= 4. GỬI EMAIL =============
        [HttpPost]
        public async Task<IActionResult> EmailSummaryReport(string email, DateTime? from, DateTime? to)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                TempData[SD.Temp_Error] = "Vui lòng nhập email.";
                return RedirectToAction(nameof(Index), new { from, to });
            }

            var vm = await BuildDashboardViewModel(from, to);

            var htmlContent = $@"
<div style='font-family: Arial, sans-serif;'>
  <h2 style='color: #007bff;'>Báo cáo nhanh MotorShop</h2>
  <p>Thời gian: <b>{FormatRange(vm.From, vm.To)}</b></p>
  <table border='1' cellpadding='8' cellspacing='0'
         style='border-collapse: collapse; width: 100%; max-width: 600px;'>
    <tr style='background-color: #f2f2f2;'>
      <th>Chỉ tiêu</th><th>Giá trị</th>
    </tr>
    <tr>
      <td>Doanh thu</td>
      <td style='text-align:right; font-weight:bold;'>{vm.TotalRevenueInRange:N0} ₫</td>
    </tr>
    <tr><td>Đơn hàng</td><td style='text-align:right;'>{vm.TotalOrdersInRange}</td></tr>
    <tr><td>Đơn thành công</td><td style='text-align:right;'>{vm.SuccessfulOrdersInRange}</td></tr>
    <tr><td>Đơn hủy</td><td style='text-align:right;'>{vm.CancelledOrdersInRange}</td></tr>
    <tr><td>Khách hàng mới</td><td style='text-align:right;'>{vm.NewCustomersInRange}</td></tr>
  </table>
  <p>Vui lòng truy cập trang quản trị để tải báo cáo chi tiết PDF/Excel.</p>
</div>";

            await _emailSender.SendEmailAsync(
                email,
                $"[Báo cáo] Doanh thu {FormatRange(vm.From, vm.To)}",
                htmlContent);

            TempData[SD.Temp_Success] = "Đã gửi email báo cáo thành công.";
            return RedirectToAction(nameof(Index), new { from, to });
        }

        // ============= 5. LOGIC QUERY =============
        // ============= 5. LOGIC QUERY =============
        // Trong DashboardController.cs
        private async Task<DashboardViewModel> BuildDashboardViewModel(DateTime? from, DateTime? to)
        {
            var todayUtc = DateTime.UtcNow.Date;
            // Mặc định 7 ngày nếu không chọn
            var fromUtc = from?.Date ?? todayUtc.AddDays(-6);
            var toUtc = to?.Date.AddDays(1) ?? todayUtc.AddDays(1); // exclusive

            var vm = new DashboardViewModel
            {
                From = fromUtc,
                To = toUtc.AddDays(-1) // hiển thị đến hết ngày trước toUtc
            };

            // 1. Query đơn hàng trong khoảng
            var ordersInRangeQuery = _db.Orders
                .Include(o => o.User)
                .Where(o => o.OrderDate >= fromUtc && o.OrderDate < toUtc);

            var validOrders = ordersInRangeQuery
                .Where(o => o.Status != OrderStatus.Cancelled);

            // 2. KPI tổng quan
            vm.TotalOrdersInRange = await ordersInRangeQuery.CountAsync();
            vm.TotalRevenueInRange = await validOrders.SumAsync(o => o.TotalAmount);
            vm.SuccessfulOrdersInRange = await validOrders.CountAsync(o => o.Status == OrderStatus.Completed);
            vm.CancelledOrdersInRange = await ordersInRangeQuery.CountAsync(o => o.Status == OrderStatus.Cancelled);
            vm.NewCustomersInRange = await _userManager.Users
                .CountAsync(u => u.CreatedAt >= fromUtc && u.CreatedAt < toUtc);

            // 3. Biểu đồ DOANH THU theo ngày (Line)
            var revenueData = await validOrders
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new { Date = g.Key, Total = g.Sum(x => x.TotalAmount) })
                .ToListAsync();

            // 4. Biểu đồ SỐ ĐƠN theo ngày (Bar)
            var orderCountData = await ordersInRangeQuery
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync();

            // 5. Biểu đồ KHÁCH MỚI theo ngày (Bar)
            var newCustomerData = await _userManager.Users
                .Where(u => u.CreatedAt >= fromUtc && u.CreatedAt < toUtc)
                .GroupBy(u => u.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync();

            // Fill dữ liệu cho từng ngày (kể cả ngày không có đơn/khách)
            for (var day = fromUtc; day < toUtc; day = day.AddDays(1))
            {
                var label = day.ToString("dd/MM");

                vm.RevenueChartLabels.Add(label);
                var rev = revenueData.FirstOrDefault(x => x.Date == day)?.Total ?? 0;
                vm.RevenueChartData.Add(rev);

                vm.OrderCountChartLabels.Add(label);
                var cnt = orderCountData.FirstOrDefault(x => x.Date == day)?.Count ?? 0;
                vm.OrderCountChartData.Add(cnt);

                vm.NewCustomerChartLabels.Add(label);
                var newCnt = newCustomerData.FirstOrDefault(x => x.Date == day)?.Count ?? 0;
                vm.NewCustomerChartData.Add(newCnt);
            }

            // 6. Biểu đồ TRẠNG THÁI đơn hàng (Doughnut)
            var statusData = await ordersInRangeQuery
                .GroupBy(o => o.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            foreach (OrderStatus status in Enum.GetValues(typeof(OrderStatus)))
            {
                vm.OrderStatusLabels.Add(status.ToString());
                vm.OrderStatusCounts.Add(statusData.FirstOrDefault(x => x.Status == status)?.Count ?? 0);
            }

            // 7. Doanh thu theo DANH MỤC (Bar chart)
            var revenueByCategory = await _db.OrderItems
                .Where(oi => oi.Order.OrderDate >= fromUtc
                          && oi.Order.OrderDate < toUtc
                          && oi.Order.Status != OrderStatus.Cancelled)
                .Include(oi => oi.Product)
                .ThenInclude(p => p.Category)
                .GroupBy(oi => oi.Product.Category != null ? oi.Product.Category.Name : "Khác")
                .Select(g => new
                {
                    Category = g.Key,
                    Total = g.Sum(x => x.Quantity * x.UnitPrice)
                })
                .ToListAsync();

            foreach (var cat in revenueByCategory)
            {
                vm.RevenueByCategoryLabels.Add(cat.Category ?? "Khác");
                vm.RevenueByCategoryData.Add(cat.Total);
            }

            // 8. Top sản phẩm bán chạy
            vm.TopProducts = await _db.OrderItems
                .Where(oi => oi.Order.OrderDate >= fromUtc
                          && oi.Order.OrderDate < toUtc
                          && oi.Order.Status != OrderStatus.Cancelled)
                .Include(oi => oi.Product)
                .GroupBy(oi => new { oi.ProductId, oi.Product.Name, oi.Product.ImageUrl })
                .Select(g => new TopProductDto
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.Name,
                    ImageUrl = g.Key.ImageUrl,
                    Quantity = g.Sum(x => x.Quantity),
                    Revenue = g.Sum(x => x.Quantity * x.UnitPrice)
                })
                .OrderByDescending(x => x.Quantity)
                .Take(5)
                .ToListAsync();

            // 9. Đơn hàng gần đây
            vm.RecentOrders = await _db.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .Take(6)
                .AsNoTracking()
                .ToListAsync();

            // 10. Khách hàng VIP (Top chi tiêu trong khoảng from-to)
            // Gom đơn theo UserId để lấy TotalOrders & TotalSpent
            var customerAgg = await validOrders
                .Where(o => o.UserId != null)
                .GroupBy(o => o.UserId!)
                .Select(g => new
                {
                    UserId = g.Key,
                    TotalOrders = g.Count(),
                    TotalSpent = g.Sum(x => x.TotalAmount)
                })
                .ToListAsync();

            var userIds = customerAgg.Select(x => x.UserId).ToList();
            var users = await _userManager.Users
                .Where(u => userIds.Contains(u.Id))
                .ToListAsync();

            vm.VipCustomers = customerAgg
                .OrderByDescending(x => x.TotalSpent)
                .Take(10) // Top 10 VIP
                .Select(x =>
                {
                    var u = users.FirstOrDefault(u => u.Id == x.UserId);
                    return new CustomerSummaryDto
                    {
                        UserId = x.UserId,
                        Email = u?.Email ?? "",
                        FullName = u?.FullName,
                        PhoneNumber = u?.PhoneNumber,
                        CreatedAt = u?.CreatedAt ?? DateTime.UtcNow,
                        TotalOrders = x.TotalOrders,
                        TotalSpent = x.TotalSpent
                    };
                })
                .ToList();

            // 11. Khách hàng đăng ký gần đây (trong khoảng from-to)
            var newUsersList = await _userManager.Users
                .Where(u => u.CreatedAt >= fromUtc && u.CreatedAt < toUtc)
                .OrderByDescending(u => u.CreatedAt)
                .Take(10)
                .ToListAsync();

            var newUserIds = newUsersList.Select(u => u.Id).ToList();

            var newUserOrderAgg = await _db.Orders
                .Where(o => o.UserId != null && newUserIds.Contains(o.UserId)
                            && o.Status != OrderStatus.Cancelled)
                .GroupBy(o => o.UserId!)
                .Select(g => new
                {
                    UserId = g.Key,
                    TotalOrders = g.Count(),
                    TotalSpent = g.Sum(x => x.TotalAmount)
                })
                .ToListAsync();

            var newUserOrderDict = newUserOrderAgg.ToDictionary(x => x.UserId, x => x);

            vm.NewCustomers = newUsersList
                .Select(u =>
                {
                    newUserOrderDict.TryGetValue(u.Id, out var agg);
                    return new CustomerSummaryDto
                    {
                        UserId = u.Id,
                        Email = u.Email ?? "",
                        FullName = u.FullName,
                        PhoneNumber = u.PhoneNumber,
                        CreatedAt = u.CreatedAt,
                        TotalOrders = agg?.TotalOrders ?? 0,
                        TotalSpent = agg?.TotalSpent ?? 0
                    };
                })
                .ToList();

            return vm;
        }


        private string FormatRange(DateTime? from, DateTime? to)
        {
            if (from == null && to == null) return "Toàn thời gian";
            if (from != null && to == null) return $"Từ {from:dd/MM/yyyy}";
            if (from == null && to != null) return $"Đến {to:dd/MM/yyyy}";
            return $"{from:dd/MM/yyyy} - {to:dd/MM/yyyy}";
        }
    }
}
