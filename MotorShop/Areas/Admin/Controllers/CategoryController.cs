using ClosedXML.Excel;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MotorShop.Data;
using MotorShop.Models;
using MotorShop.Utilities;
using System.Text;

// Alias cho iText để tránh nhầm với các thư viện khác
using PdfDocument = iTextSharp.text.Document;
using PdfPageSize = iTextSharp.text.PageSize;
using ItFont = iTextSharp.text.Font;
using ItBaseColor = iTextSharp.text.BaseColor;

namespace MotorShop.Areas.Admin.Controllers
{
    [Area(SD.AdminAreaName)]
    [Authorize(Roles = SD.Role_Admin)]
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;

        public CategoryController(ApplicationDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        // ViewModel cho Index
        public class CategoryListItemVM
        {
            public int Id { get; set; }
            public string Name { get; set; } = default!;
            public string? Slug { get; set; }
            public string? Description { get; set; }
            public int ProductCount { get; set; }
        }

        // ================== HELPER: QUERY CÓ LỌC ==================
        private IQueryable<Category> BuildFilteredQuery(string? q)
        {
            var query = _db.Categories
                .Include(c => c.Products)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim().ToLower();
                query = query.Where(c =>
                    c.Name.ToLower().Contains(q) ||
                    (c.Slug != null && c.Slug.ToLower().Contains(q)) ||
                    (c.Description != null && c.Description.ToLower().Contains(q)));
            }

            return query;
        }

        private static string GenerateSlug(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "";
            var slug = name.Trim().ToLower();

            // thay khoảng trắng bằng -
            slug = System.Text.RegularExpressions.Regex.Replace(slug, @"\s+", "-");

            // bỏ ký tự không hợp lệ
            slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\-]", "");

            return slug;
        }

        // ================== 1. INDEX ==================
        public async Task<IActionResult> Index(string? q)
        {
            var query = BuildFilteredQuery(q)
                .OrderBy(c => c.Name);

            var list = await query
                .Select(c => new CategoryListItemVM
                {
                    Id = c.Id,
                    Name = c.Name,
                    Slug = c.Slug,
                    Description = c.Description,
                    ProductCount = c.Products.Count
                })
                .ToListAsync();

            ViewBag.Q = q;
            ViewBag.Total = list.Count;
            ViewBag.TotalProducts = await _db.Products.CountAsync();

            return View(list);
        }

        // ================== 2. CREATE ==================
        // ================== 2. CREATE ==================
        public IActionResult Create()
        {
            var model = new Category
            {
                Name = string.Empty,       // ✅ thỏa required Name
                Slug = string.Empty,       // (nếu Slug KHÔNG required thì dòng này cũng không sao)
                Description = string.Empty // (nếu có)
            };
            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category)
        {
            if (!ModelState.IsValid)
                return View(category);

            // Check trùng tên
            var dup = await _db.Categories
                .AnyAsync(c => c.Name == category.Name);
            if (dup)
            {
                ModelState.AddModelError(nameof(category.Name), "Tên danh mục đã tồn tại.");
                return View(category);
            }

            // Tự sinh Slug nếu chưa nhập
            if (string.IsNullOrWhiteSpace(category.Slug))
                category.Slug = GenerateSlug(category.Name);

            _db.Categories.Add(category);
            await _db.SaveChangesAsync();

            TempData[SD.Temp_Success] = "Thêm danh mục thành công.";
            return RedirectToAction(nameof(Index));
        }

        // ================== 3. EDIT ==================
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _db.Categories.FindAsync(id);
            if (category == null) return NotFound();
            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Category input)
        {
            if (id != input.Id) return NotFound();

            if (!ModelState.IsValid)
                return View(input);

            var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id);
            if (category == null) return NotFound();

            // Check trùng tên (trừ chính nó)
            var dup = await _db.Categories
                .AnyAsync(c => c.Id != id && c.Name == input.Name);
            if (dup)
            {
                ModelState.AddModelError(nameof(input.Name), "Tên danh mục đã tồn tại.");
                return View(input);
            }

            category.Name = input.Name;
            category.Description = input.Description;

            // Nếu slug rỗng hoặc khác nhiều, có thể regenerate
            if (string.IsNullOrWhiteSpace(input.Slug))
                category.Slug = GenerateSlug(input.Name);
            else
                category.Slug = input.Slug;

            await _db.SaveChangesAsync();
            TempData[SD.Temp_Success] = "Cập nhật danh mục thành công.";
            return RedirectToAction(nameof(Index));
        }

        // ================== 4. DELETE ==================
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _db.Categories
                .AsNoTracking()
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null) return NotFound();
            return View(category);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _db.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
                return RedirectToAction(nameof(Index));

            if (category.Products.Any())
            {
                TempData[SD.Temp_Error] =
                    $"Danh mục \"{category.Name}\" đang có {category.Products.Count} sản phẩm. " +
                    "Vui lòng chuyển sản phẩm sang danh mục khác trước khi xóa.";
                return RedirectToAction(nameof(Index));
            }

            _db.Categories.Remove(category);
            await _db.SaveChangesAsync();

            TempData[SD.Temp_Success] = "Đã xóa danh mục.";
            return RedirectToAction(nameof(Index));
        }

        // ================== 5. EXPORT EXCEL ==================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportExcel(string? q)
        {
            var list = await BuildFilteredQuery(q)
                .OrderBy(c => c.Name)
                .Include(c => c.Products)
                .ToListAsync();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Categories");

            ws.Cell(1, 1).Value = "BÁO CÁO DANH MỤC SẢN PHẨM";
            ws.Range(1, 1, 1, 5).Merge()
                .Style.Font.SetBold()
                .Font.SetFontSize(16)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(2, 1).Value = $"Tổng danh mục: {list.Count}";
            ws.Range(2, 1, 2, 5).Merge()
                .Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            int row = 4;

            ws.Cell(row, 1).Value = "ID";
            ws.Cell(row, 2).Value = "Tên danh mục";
            ws.Cell(row, 3).Value = "Slug";
            ws.Cell(row, 4).Value = "Mô tả";
            ws.Cell(row, 5).Value = "Số sản phẩm";
            ws.Row(row).Style.Font.SetBold();
            row++;

            foreach (var c in list)
            {
                ws.Cell(row, 1).Value = c.Id;
                ws.Cell(row, 2).Value = c.Name;
                ws.Cell(row, 3).Value = c.Slug;
                ws.Cell(row, 4).Value = c.Description ?? "";
                ws.Cell(row, 5).Value = c.Products.Count;
                row++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            var content = stream.ToArray();

            return File(
                content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Categories_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
        }

        // ================== 6. EXPORT PDF ==================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportPdf(string? q)
        {
            var list = await BuildFilteredQuery(q)
                .OrderBy(c => c.Name)
                .Include(c => c.Products)
                .ToListAsync();

            using var ms = new MemoryStream();
            var doc = new PdfDocument(PdfPageSize.A4.Rotate(), 25, 25, 30, 30);
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

            BaseFont bf = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
            var colorHeaderBg = new ItBaseColor(30, 64, 175);
            var colorHeaderText = new ItBaseColor(255, 255, 255);
            var colorNormal = new ItBaseColor(15, 23, 42);
            var colorGray = new ItBaseColor(107, 114, 128);

            var fontTitle = new ItFont(bf, 16, ItFont.BOLD, colorHeaderBg);
            var fontHeader = new ItFont(bf, 11, ItFont.BOLD, colorHeaderText);
            var fontNormal = new ItFont(bf, 10, ItFont.NORMAL, colorNormal);
            var fontSmall = new ItFont(bf, 9, ItFont.ITALIC, colorGray);

            // Title
            var title = new Paragraph("BÁO CÁO DANH MỤC SẢN PHẨM", fontTitle)
            {
                Alignment = Element.ALIGN_CENTER,
                SpacingAfter = 8f
            };
            doc.Add(title);

            var sub = new Paragraph(
                $"Tổng danh mục: {list.Count} – Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}",
                fontSmall)
            {
                Alignment = Element.ALIGN_CENTER,
                SpacingAfter = 12f
            };
            doc.Add(sub);

            // Table
            var table = new PdfPTable(5)
            {
                WidthPercentage = 100
            };
            table.SetWidths(new float[] { 1f, 3f, 3f, 5f, 2f });

            void AddHeader(string text)
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

            AddHeader("ID");
            AddHeader("Tên danh mục");
            AddHeader("Slug");
            AddHeader("Mô tả");
            AddHeader("Số sản phẩm");

            foreach (var c in list)
            {
                table.AddCell(new PdfPCell(new Phrase(c.Id.ToString(), fontNormal)) { Padding = 4 });
                table.AddCell(new PdfPCell(new Phrase(c.Name ?? "", fontNormal)) { Padding = 4 });
                table.AddCell(new PdfPCell(new Phrase(c.Slug ?? "", fontNormal)) { Padding = 4 });
                table.AddCell(new PdfPCell(new Phrase(c.Description ?? "", fontNormal)) { Padding = 4 });
                table.AddCell(new PdfPCell(new Phrase(c.Products.Count.ToString(), fontNormal))
                {
                    Padding = 4,
                    HorizontalAlignment = Element.ALIGN_RIGHT
                });
            }

            doc.Add(table);

            var footer = new Paragraph(
                $"\nNgười xuất: {(User.Identity?.Name ?? "Admin")}  –  MotorShop Admin",
                fontSmall)
            {
                Alignment = Element.ALIGN_RIGHT
            };
            doc.Add(footer);

            doc.Close();

            return File(ms.ToArray(), "application/pdf", $"Categories_{DateTime.Now:yyyyMMdd}.pdf");
        }
    }
}
