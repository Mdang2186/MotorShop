using ClosedXML.Excel;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MotorShop.Data;
using MotorShop.Models;
using MotorShop.Utilities;
using System.Text;

// alias cho Document iText để tránh nhầm lẫn nếu sau này dùng OpenXml
using PdfDocument = iTextSharp.text.Document;
using PdfPageSize = iTextSharp.text.PageSize;
using ItFont = iTextSharp.text.Font;
using ItBaseColor = iTextSharp.text.BaseColor;

namespace MotorShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Manager")]
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;

        public ProductController(ApplicationDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        // ================== HELPER BUILD QUERY (lọc dùng chung) ==================
        private IQueryable<Product> BuildFilteredQuery(string? q, int? categoryId, string? status)
        {
            var query = _db.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Images)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim().ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(q) ||
                    (p.SKU != null && p.SKU.ToLower().Contains(q)));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            if (!string.IsNullOrEmpty(status))
            {
                status = status.ToLower();
                if (status == "active")
                    query = query.Where(p => p.IsActive && p.StockQuantity > 0);
                else if (status == "outofstock")
                    query = query.Where(p => p.StockQuantity <= 0);
                else if (status == "inactive")
                    query = query.Where(p => !p.IsActive);
            }

            return query;
        }

        private void PopulateDropdowns(Product? product = null)
        {
            ViewBag.BrandId = new SelectList(
                _db.Brands.AsNoTracking().OrderBy(b => b.Name),
                "Id", "Name", product?.BrandId);

            ViewBag.CategoryId = new SelectList(
                _db.Categories.AsNoTracking().OrderBy(c => c.Name),
                "Id", "Name", product?.CategoryId);
        }

        // ================== 1. INDEX (LIST + FILTER + PAGING) ==================
        public async Task<IActionResult> Index(string? q, int? categoryId, string? status, int page = 1)
        {
            const int pageSize = 10;

            var query = BuildFilteredQuery(q, categoryId, status);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(p => p.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Q = q;
            ViewBag.Status = status;
            ViewBag.CategoryId = new SelectList(
                await _db.Categories.AsNoTracking().ToListAsync(),
                "Id", "Name", categoryId);

            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);
            ViewBag.TotalItems = total;

            return View(items); // List<Product>
        }

        // ================== 2. CREATE ==================
        public IActionResult Create()
        {
            PopulateDropdowns();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product)
        {
            if (!ModelState.IsValid)
            {
                PopulateDropdowns(product);
                return View(product);
            }

            product.CreatedAt = DateTime.UtcNow;
            product.UpdatedAt = DateTime.UtcNow;

            _db.Add(product);
            await _db.SaveChangesAsync();

            TempData[SD.Temp_Success] = "Thêm sản phẩm thành công.";
            return RedirectToAction(nameof(Index));
        }

        // ================== 3. EDIT ==================
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _db.Products.FindAsync(id);
            if (product == null) return NotFound();

            PopulateDropdowns(product);
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product)
        {
            if (id != product.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                PopulateDropdowns(product);
                return View(product);
            }

            var dbProduct = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (dbProduct == null) return NotFound();

            dbProduct.Name = product.Name;
            dbProduct.SKU = product.SKU;
            dbProduct.Description = product.Description;
            dbProduct.Price = product.Price;
            dbProduct.OriginalPrice = product.OriginalPrice;
            dbProduct.Year = product.Year;
            dbProduct.Slug = product.Slug;
            dbProduct.IsActive = product.IsActive;
            dbProduct.IsPublished = product.IsPublished;
            dbProduct.StockQuantity = product.StockQuantity;
            dbProduct.ImageUrl = product.ImageUrl;
            dbProduct.BrandId = product.BrandId;
            dbProduct.CategoryId = product.CategoryId;
            dbProduct.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            TempData[SD.Temp_Success] = "Cập nhật sản phẩm thành công.";
            return RedirectToAction(nameof(Index));
        }

        // ================== 4. DELETE ==================
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _db.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();
            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _db.Products.FindAsync(id);
            if (product != null)
            {
                _db.Products.Remove(product);
                await _db.SaveChangesAsync();
                TempData[SD.Temp_Success] = "Đã xóa sản phẩm.";
            }
            return RedirectToAction(nameof(Index));
        }

        // ================== 4.5 DETAILS (PROFILE SẢN PHẨM) ==================
        public async Task<IActionResult> Details(int id)
        {
            var product = await _db.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Images)
                .Include(p => p.Specifications)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();
            return View(product);
        }

        // ================== 5. TOGGLE ACTIVE / PUBLISH ==================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id, string? returnUrl = null)
        {
            var product = await _db.Products.FindAsync(id);
            if (product == null) return NotFound();

            product.IsActive = !product.IsActive;
            product.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            TempData[SD.Temp_Success] = product.IsActive
                ? "Đã kích hoạt sản phẩm."
                : "Đã ẩn sản phẩm (ngừng kinh doanh).";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePublished(int id, string? returnUrl = null)
        {
            var product = await _db.Products.FindAsync(id);
            if (product == null) return NotFound();

            product.IsPublished = !product.IsPublished;
            product.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            TempData[SD.Temp_Success] = product.IsPublished
                ? "Sản phẩm đã được hiển thị trên website."
                : "Sản phẩm đã tạm ẩn khỏi website.";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction(nameof(Index));
        }

        // ================== 6. EXPORT EXCEL ==================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportExcel(string? q, int? categoryId, string? status)
        {
            var query = BuildFilteredQuery(q, categoryId, status)
                .OrderByDescending(p => p.Id);

            var list = await query.ToListAsync();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Products");

            ws.Cell(1, 1).Value = "BÁO CÁO DANH MỤC SẢN PHẨM";
            ws.Range(1, 1, 1, 8).Merge()
                .Style.Font.SetBold()
                .Font.SetFontSize(16)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(2, 1).Value = $"Tổng cộng: {list.Count} sản phẩm";
            ws.Range(2, 1, 2, 8).Merge()
                .Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            int row = 4;

            ws.Cell(row, 1).Value = "ID";
            ws.Cell(row, 2).Value = "Tên sản phẩm";
            ws.Cell(row, 3).Value = "SKU";
            ws.Cell(row, 4).Value = "Danh mục";
            ws.Cell(row, 5).Value = "Thương hiệu";
            ws.Cell(row, 6).Value = "Giá bán";
            ws.Cell(row, 7).Value = "Tồn kho";
            ws.Cell(row, 8).Value = "Trạng thái";
            ws.Row(row).Style.Font.SetBold();
            row++;

            foreach (var p in list)
            {
                ws.Cell(row, 1).Value = p.Id;
                ws.Cell(row, 2).Value = p.Name;
                ws.Cell(row, 3).Value = p.SKU;
                ws.Cell(row, 4).Value = p.Category?.Name;
                ws.Cell(row, 5).Value = p.Brand?.Name;
                ws.Cell(row, 6).Value = (double)p.Price;
                ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0 ₫";
                ws.Cell(row, 7).Value = p.StockQuantity;
                ws.Cell(row, 8).Value = p.IsActive
                    ? (p.StockQuantity > 0 ? "Đang bán" : "Hết hàng")
                    : "Ngừng kinh doanh";
                row++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            var content = stream.ToArray();

            return File(
                content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Products_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
        }

        // ================== 7. QUẢN LÝ ẢNH ==================
        public async Task<IActionResult> Images(int id)
        {
            var product = await _db.Products
                .Include(p => p.Images)          // KHÔNG OrderBy trong Include
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddImage(int productId, string imageUrl, string? caption, int sortOrder = 0, bool isPrimary = false)
        {
            var product = await _db.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == productId);
            if (product == null) return NotFound();

            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                TempData[SD.Temp_Error] = "Đường dẫn ảnh không được để trống.";
                return RedirectToAction(nameof(Images), new { id = productId });
            }

            if (isPrimary)
            {
                foreach (var img in product.Images)
                    img.IsPrimary = false;
            }

            _db.ProductImages.Add(new ProductImage
            {
                ProductId = productId,
                ImageUrl = imageUrl.Trim(),
                Caption = caption ?? "",
                SortOrder = sortOrder,
                IsPrimary = isPrimary
            });

            await _db.SaveChangesAsync();
            TempData[SD.Temp_Success] = "Đã thêm ảnh sản phẩm.";
            return RedirectToAction(nameof(Images), new { id = productId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteImage(int id)
        {
            var img = await _db.ProductImages.FindAsync(id);
            if (img == null) return NotFound();

            var productId = img.ProductId;
            _db.ProductImages.Remove(img);
            await _db.SaveChangesAsync();

            TempData[SD.Temp_Success] = "Đã xóa ảnh.";
            return RedirectToAction(nameof(Images), new { id = productId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPrimaryImage(int id)
        {
            var img = await _db.ProductImages.FindAsync(id);
            if (img == null) return NotFound();

            var list = await _db.ProductImages
                .Where(i => i.ProductId == img.ProductId)
                .ToListAsync();

            foreach (var item in list)
                item.IsPrimary = (item.Id == id);

            await _db.SaveChangesAsync();

            TempData[SD.Temp_Success] = "Đã đặt ảnh đại diện.";
            return RedirectToAction(nameof(Images), new { id = img.ProductId });
        }

        // ================== 8. QUẢN LÝ THÔNG SỐ KỸ THUẬT ==================
        public async Task<IActionResult> Specs(int id)
        {
            var product = await _db.Products
                .Include(p => p.Specifications)   // KHÔNG OrderBy trong Include
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSpec(int productId, string name, string? value, int sortOrder = 0)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData[SD.Temp_Error] = "Tên thông số không được để trống.";
                return RedirectToAction(nameof(Specs), new { id = productId });
            }

            _db.ProductSpecifications.Add(new ProductSpecification
            {
                ProductId = productId,
                Name = name.Trim(),
                Value = string.IsNullOrWhiteSpace(value) ? null : value.Trim(),
                SortOrder = sortOrder
            });

            await _db.SaveChangesAsync();

            TempData[SD.Temp_Success] = "Đã thêm thông số kỹ thuật.";
            return RedirectToAction(nameof(Specs), new { id = productId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSpec(int id)
        {
            var spec = await _db.ProductSpecifications.FindAsync(id);
            if (spec == null) return NotFound();

            var productId = spec.ProductId;
            _db.ProductSpecifications.Remove(spec);
            await _db.SaveChangesAsync();

            TempData[SD.Temp_Success] = "Đã xóa thông số.";
            return RedirectToAction(nameof(Specs), new { id = productId });
        }

        // ================== 9. EXPORT PDF ==================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportPdf(string? q, int? categoryId, string? status)
        {
            var query = BuildFilteredQuery(q, categoryId, status)
                .OrderByDescending(p => p.Id);

            var list = await query.ToListAsync();

            using var ms = new MemoryStream();
            var doc = new PdfDocument(PdfPageSize.A4.Rotate(), 25, 25, 30, 30);
            PdfWriter.GetInstance(doc, ms);
            doc.Open();

            string fontPath = Path.Combine(_env.WebRootPath, "fonts", "arial.ttf");
            if (!System.IO.File.Exists(fontPath))
            {
                fontPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Fonts),
                    "arial.ttf");
            }

            var bf = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
            var colorHeaderBg = new ItBaseColor(30, 64, 175);
            var colorHeaderText = new ItBaseColor(255, 255, 255);
            var colorNormal = new ItBaseColor(15, 23, 42);
            var colorGray = new ItBaseColor(107, 114, 128);

            var fontTitle = new ItFont(bf, 16, ItFont.BOLD, colorHeaderBg);
            var fontHeader = new ItFont(bf, 11, ItFont.BOLD, colorHeaderText);
            var fontNormal = new ItFont(bf, 10, ItFont.NORMAL, colorNormal);
            var fontSmall = new ItFont(bf, 9, ItFont.ITALIC, colorGray);

            var title = new Paragraph("BÁO CÁO DANH SÁCH SẢN PHẨM", fontTitle)
            {
                Alignment = Element.ALIGN_CENTER,
                SpacingAfter = 8f
            };
            doc.Add(title);

            var sub = new Paragraph(
                $"Tổng cộng: {list.Count} sản phẩm — Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}",
                fontSmall)
            {
                Alignment = Element.ALIGN_CENTER,
                SpacingAfter = 12f
            };
            doc.Add(sub);

            var table = new PdfPTable(7)
            {
                WidthPercentage = 100
            };
            table.SetWidths(new float[] { 1f, 3f, 2f, 2.5f, 2.5f, 2f, 2f });

            void AddHeaderCell(string text)
            {
                var cell = new PdfPCell(new Phrase(text, fontHeader))
                {
                    BackgroundColor = colorHeaderBg,
                    Padding = 5,
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    VerticalAlignment = Element.ALIGN_MIDDLE
                };
                table.AddCell(cell);
            }

            AddHeaderCell("ID");
            AddHeaderCell("Tên sản phẩm");
            AddHeaderCell("SKU");
            AddHeaderCell("Danh mục");
            AddHeaderCell("Thương hiệu");
            AddHeaderCell("Giá");
            AddHeaderCell("Tồn / Trạng thái");

            foreach (var p in list)
            {
                table.AddCell(new PdfPCell(new Phrase(p.Id.ToString(), fontNormal)) { Padding = 4 });
                table.AddCell(new PdfPCell(new Phrase(p.Name ?? "", fontNormal)) { Padding = 4 });
                table.AddCell(new PdfPCell(new Phrase(p.SKU ?? "", fontNormal)) { Padding = 4 });
                table.AddCell(new PdfPCell(new Phrase(p.Category?.Name ?? "", fontNormal)) { Padding = 4 });
                table.AddCell(new PdfPCell(new Phrase(p.Brand?.Name ?? "", fontNormal)) { Padding = 4 });

                table.AddCell(new PdfPCell(new Phrase(p.Price.ToString("N0") + " ₫", fontNormal))
                {
                    Padding = 4,
                    HorizontalAlignment = Element.ALIGN_RIGHT
                });

                var statusText = p.IsActive
                    ? (p.StockQuantity > 0 ? $"Còn {p.StockQuantity}" : "Hết hàng")
                    : "Ngừng kinh doanh";

                table.AddCell(new PdfPCell(new Phrase(statusText, fontNormal))
                {
                    Padding = 4,
                    HorizontalAlignment = Element.ALIGN_RIGHT
                });
            }

            doc.Add(table);

            var footer = new Paragraph(
                $"\nNgười xuất: {(User.Identity?.Name ?? "Admin")}   –   MotorShop Admin",
                fontSmall)
            {
                Alignment = Element.ALIGN_RIGHT
            };
            doc.Add(footer);

            doc.Close();

            return File(ms.ToArray(), "application/pdf", $"Products_{DateTime.Now:yyyyMMdd}.pdf");
        }
    }
}
