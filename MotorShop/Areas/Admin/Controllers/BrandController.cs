using System.Text;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MotorShop.Data;
using MotorShop.Models;
using MotorShop.Utilities;

// alias cho iText
using PdfDocument = iTextSharp.text.Document;
using PdfPageSize = iTextSharp.text.PageSize;
using ItFont = iTextSharp.text.Font;
using ItBaseColor = iTextSharp.text.BaseColor;

namespace MotorShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Manager")]
    public class BrandController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;

        public BrandController(ApplicationDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        // ========== VIEW MODEL ==========

        public class BrandListItemVM
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public string? Slug { get; set; }
            public string? LogoUrl { get; set; }
            public bool IsActive { get; set; }
            public int ProductCount { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        public class BrandIndexVM
        {
            public string? Search { get; set; }
            public bool? OnlyActive { get; set; }
            public int Page { get; set; }
            public int TotalPages { get; set; }
            public int TotalBrands { get; set; }
            public int ActiveCount { get; set; }
            public int InactiveCount { get; set; }

            public List<BrandListItemVM> Items { get; set; } = new();
        }

        // ========= HELPER CHUNG ==========

        private static string ToSlug(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";
            var s = input.Trim().ToLowerInvariant();
            s = Regex.Replace(s, @"\s+", "-");
            s = Regex.Replace(s, @"[^a-z0-9\-]+", "");
            s = Regex.Replace(s, @"-+", "-").Trim('-');
            return s;
        }

        private string BrandLogoFolder
            => Path.Combine(_env.WebRootPath, "images", "brands");

        private string? SaveLogo(IFormFile? file, string? oldPath = null)
        {
            if (file == null || file.Length == 0) return oldPath;

            Directory.CreateDirectory(BrandLogoFolder);

            var ext = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(ext)) ext = ".png";

            // tên file random, tránh trùng
            var fileName = $"brand_{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(BrandLogoFolder, fileName);

            using (var fs = new FileStream(fullPath, FileMode.Create))
            {
                file.CopyTo(fs);
            }

            // Xoá file cũ nếu có
            if (!string.IsNullOrWhiteSpace(oldPath))
            {
                try
                {
                    var oldPhysical = oldPath.TrimStart('~', '/')
                                             .Replace("/", Path.DirectorySeparatorChar.ToString());
                    var oldFull = Path.Combine(_env.WebRootPath, oldPhysical);
                    if (System.IO.File.Exists(oldFull))
                        System.IO.File.Delete(oldFull);
                }
                catch { /* best effort */ }
            }

            // Trả về URL tương đối dùng trong img src
            return $"/images/brands/{fileName}";
        }

        // ========== 1. INDEX (LIST + FILTER + PAGING) ==========

        public async Task<IActionResult> Index(string? q, bool? onlyActive, int page = 1)
        {
            const int pageSize = 12;
            page = Math.Max(1, page);

            var query = _db.Brands
                .Include(b => b.Products)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var kw = q.Trim();
                query = query.Where(b =>
                    EF.Functions.Like(b.Name, $"%{kw}%") ||
                    (b.Slug != null && EF.Functions.Like(b.Slug, $"%{kw}%")));
            }

            if (onlyActive.HasValue)
            {
                query = query.Where(b => b.IsActive == onlyActive.Value);
            }

            var totalBrands = await query.CountAsync();

            var items = await query
                .OrderByDescending(b => b.CreatedAt)
                .ThenBy(b => b.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var vm = new BrandIndexVM
            {
                Search = q,
                OnlyActive = onlyActive,
                Page = page,
                TotalPages = (int)Math.Ceiling(totalBrands / (double)pageSize),
                TotalBrands = totalBrands,
                ActiveCount = await _db.Brands.CountAsync(b => b.IsActive),
                InactiveCount = await _db.Brands.CountAsync(b => !b.IsActive),
                Items = items.Select(b => new BrandListItemVM
                {
                    Id = b.Id,
                    Name = b.Name,
                    Slug = b.Slug,
                    LogoUrl = b.LogoUrl,
                    IsActive = b.IsActive,
                    ProductCount = b.Products.Count,
                    CreatedAt = b.CreatedAt
                }).ToList()
            };

            return View(vm); // View: Areas/Admin/Views/Brand/Index.cshtml
        }

        // ========== 2. CREATE ==========

        public IActionResult Create()
        {
            return View(new Brand());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Brand brand, IFormFile? logoFile)
        {
            if (!ModelState.IsValid)
            {
                return View(brand);
            }

            // Slug tự sinh nếu trống
            brand.Slug = string.IsNullOrWhiteSpace(brand.Slug)
                ? ToSlug(brand.Name)
                : ToSlug(brand.Slug!);

            // Lưu logo nếu có
            if (logoFile != null && logoFile.Length > 0)
            {
                brand.LogoUrl = SaveLogo(logoFile);
            }

            brand.CreatedAt = DateTime.UtcNow;
            brand.UpdatedAt = DateTime.UtcNow;

            _db.Brands.Add(brand);
            await _db.SaveChangesAsync();

            TempData[SD.Temp_Success] = "Đã thêm thương hiệu mới.";
            return RedirectToAction(nameof(Index));
        }

        // ========== 3. EDIT ==========

        public async Task<IActionResult> Edit(int id)
        {
            var brand = await _db.Brands.FindAsync(id);
            if (brand == null) return NotFound();
            return View(brand);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Brand input, IFormFile? logoFile, bool removeLogo = false)
        {
            if (id != input.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                return View(input);
            }

            var brand = await _db.Brands.FirstOrDefaultAsync(b => b.Id == id);
            if (brand == null) return NotFound();

            brand.Name = input.Name;
            brand.Description = input.Description;
            brand.IsActive = input.IsActive;

            brand.Slug = string.IsNullOrWhiteSpace(input.Slug)
                ? ToSlug(input.Name)
                : ToSlug(input.Slug!);

            // Xử lý logo
            if (removeLogo)
            {
                brand.LogoUrl = SaveLogo(null, brand.LogoUrl); // xoá file cũ nếu có
                brand.LogoUrl = null;
            }
            else if (logoFile != null && logoFile.Length > 0)
            {
                brand.LogoUrl = SaveLogo(logoFile, brand.LogoUrl);
            }

            brand.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            TempData[SD.Temp_Success] = "Đã cập nhật thương hiệu.";
            return RedirectToAction(nameof(Index));
        }

        // ========== 4. DELETE ==========

        public async Task<IActionResult> Delete(int id)
        {
            var brand = await _db.Brands
                .Include(b => b.Products)
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == id);

            if (brand == null) return NotFound();
            return View(brand);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var brand = await _db.Brands
                .Include(b => b.Products)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (brand == null) return NotFound();

            if (brand.Products.Any())
            {
                TempData[SD.Temp_Error] = "Không thể xoá: còn sản phẩm thuộc thương hiệu này.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            // Xoá logo vật lý nếu có
            if (!string.IsNullOrWhiteSpace(brand.LogoUrl))
            {
                SaveLogo(null, brand.LogoUrl);
            }

            _db.Brands.Remove(brand);
            await _db.SaveChangesAsync();

            TempData[SD.Temp_Success] = "Đã xoá thương hiệu.";
            return RedirectToAction(nameof(Index));
        }

        // ========== 5. TOGGLE ACTIVE (BẬT/TẮT NHANH) ==========

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id, string? returnUrl = null)
        {
            var brand = await _db.Brands.FindAsync(id);
            if (brand == null) return NotFound();

            brand.IsActive = !brand.IsActive;
            brand.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            TempData[SD.Temp_Success] = brand.IsActive
                ? "Thương hiệu đã được kích hoạt."
                : "Thương hiệu đã được tạm ẩn.";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction(nameof(Index));
        }

        // ========== 6. EXPORT EXCEL ==========

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportExcel(string? q, bool? onlyActive)
        {
            var query = _db.Brands
                .Include(b => b.Products)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var kw = q.Trim();
                query = query.Where(b =>
                    EF.Functions.Like(b.Name, $"%{kw}%") ||
                    (b.Slug != null && EF.Functions.Like(b.Slug, $"%{kw}%")));
            }

            if (onlyActive.HasValue)
            {
                query = query.Where(b => b.IsActive == onlyActive.Value);
            }

            var list = await query
                .OrderBy(b => b.Name)
                .ToListAsync();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Brands");

            ws.Cell(1, 1).Value = "DANH SÁCH THƯƠNG HIỆU";
            ws.Range(1, 1, 1, 6).Merge()
                .Style.Font.SetBold()
                .Font.SetFontSize(16)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(2, 1).Value = $"Tổng cộng: {list.Count} thương hiệu";
            ws.Range(2, 1, 2, 6).Merge()
                .Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            int row = 4;
            ws.Cell(row, 1).Value = "ID";
            ws.Cell(row, 2).Value = "Tên";
            ws.Cell(row, 3).Value = "Slug";
            ws.Cell(row, 4).Value = "Mô tả";
            ws.Cell(row, 5).Value = "Số sản phẩm";
            ws.Cell(row, 6).Value = "Trạng thái";
            ws.Row(row).Style.Font.SetBold();
            row++;

            foreach (var b in list)
            {
                ws.Cell(row, 1).Value = b.Id;
                ws.Cell(row, 2).Value = b.Name;
                ws.Cell(row, 3).Value = b.Slug;
                ws.Cell(row, 4).Value = b.Description;
                ws.Cell(row, 5).Value = b.Products.Count;
                ws.Cell(row, 6).Value = b.IsActive ? "Đang dùng" : "Tạm ẩn";
                row++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            var content = stream.ToArray();

            return File(
                content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Brands_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
        }

        // ========== 7. EXPORT PDF ==========

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportPdf(string? q, bool? onlyActive)
        {
            var query = _db.Brands
                .Include(b => b.Products)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var kw = q.Trim();
                query = query.Where(b =>
                    EF.Functions.Like(b.Name, $"%{kw}%") ||
                    (b.Slug != null && EF.Functions.Like(b.Slug, $"%{kw}%")));
            }

            if (onlyActive.HasValue)
            {
                query = query.Where(b => b.IsActive == onlyActive.Value);
            }

            var list = await query
                .OrderBy(b => b.Name)
                .ToListAsync();

            using var ms = new MemoryStream();
            var doc = new PdfDocument(PdfPageSize.A4.Rotate(), 25, 25, 30, 30);
            PdfWriter.GetInstance(doc, ms);
            doc.Open();

            // Font có hỗ trợ Unicode
            string fontPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Fonts),
                "arial.ttf");

            var bf = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
            var colorHeaderBg = new ItBaseColor(30, 64, 175);
            var colorHeaderText = new ItBaseColor(255, 255, 255);
            var colorNormal = new ItBaseColor(15, 23, 42);
            var colorGray = new ItBaseColor(107, 114, 128);

            var fontTitle = new ItFont(bf, 16, ItFont.BOLD, colorHeaderBg);
            var fontHeader = new ItFont(bf, 11, ItFont.BOLD, colorHeaderText);
            var fontNormal = new ItFont(bf, 10, ItFont.NORMAL, colorNormal);
            var fontSmall = new ItFont(bf, 9, ItFont.ITALIC, colorGray);

            var title = new Paragraph("DANH SÁCH THƯƠNG HIỆU", fontTitle)
            {
                Alignment = Element.ALIGN_CENTER,
                SpacingAfter = 8f
            };
            doc.Add(title);

            var sub = new Paragraph(
                $"Tổng cộng: {list.Count} thương hiệu — Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}",
                fontSmall)
            {
                Alignment = Element.ALIGN_CENTER,
                SpacingAfter = 12f
            };
            doc.Add(sub);

            var table = new PdfPTable(6)
            {
                WidthPercentage = 100
            };
            table.SetWidths(new float[] { 1f, 3f, 3f, 4f, 2f, 2f });

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
            AddHeader("Tên");
            AddHeader("Slug");
            AddHeader("Mô tả");
            AddHeader("Sản phẩm");
            AddHeader("Trạng thái");

            foreach (var b in list)
            {
                table.AddCell(new PdfPCell(new Phrase(b.Id.ToString(), fontNormal)) { Padding = 4 });
                table.AddCell(new PdfPCell(new Phrase(b.Name ?? "", fontNormal)) { Padding = 4 });
                table.AddCell(new PdfPCell(new Phrase(b.Slug ?? "", fontNormal)) { Padding = 4 });
                table.AddCell(new PdfPCell(new Phrase(b.Description ?? "", fontNormal)) { Padding = 4 });

                table.AddCell(new PdfPCell(new Phrase(b.Products.Count.ToString(), fontNormal))
                {
                    Padding = 4,
                    HorizontalAlignment = Element.ALIGN_RIGHT
                });

                var status = b.IsActive ? "Đang dùng" : "Tạm ẩn";
                table.AddCell(new PdfPCell(new Phrase(status, fontNormal))
                {
                    Padding = 4,
                    HorizontalAlignment = Element.ALIGN_CENTER
                });
            }

            doc.Add(table);

            var footer = new Paragraph(
                $"\nNgười xuất: {(User.Identity?.Name ?? "Admin")} – MotorShop Admin",
                fontSmall)
            {
                Alignment = Element.ALIGN_RIGHT
            };
            doc.Add(footer);

            doc.Close();

            return File(ms.ToArray(), "application/pdf", $"Brands_{DateTime.Now:yyyyMMdd}.pdf");
        }
        // ========== 4.5 DETAILS (XEM CHI TIẾT & SẢN PHẨM LIÊN QUAN) ==========
        public async Task<IActionResult> Details(int id)
        {
            var brand = await _db.Brands
                .Include(b => b.Products)
                    .ThenInclude(p => p.Category) // Load thêm danh mục của sản phẩm
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == id);

            if (brand == null) return NotFound();
            return View(brand);
        }
    }

}
