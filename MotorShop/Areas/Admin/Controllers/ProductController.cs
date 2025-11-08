using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MotorShop.Data;
using MotorShop.Models;
using MotorShop.Utilities;
using System.Diagnostics;
using Microsoft.AspNetCore.Hosting; // Cần cho upload file
using System.IO; // Cần cho upload file

namespace MotorShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Manager")]
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductController(ApplicationDbContext db, IWebHostEnvironment webHostEnvironment)
        {
            _db = db;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Admin/Product
        // Đã thêm pageSize và sort
        public async Task<IActionResult> Index(string? q, int? categoryId, int? brandId, string? status, string? sort = "new", int page = 1, int pageSize = 15)
        {
            var query = _db.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Images) // Cần để lấy PrimaryImageUrl
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim().ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(q) || (p.SKU != null && p.SKU.ToLower().Contains(q)));
            }
            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId);
            }
            if (brandId.HasValue)
            {
                query = query.Where(p => p.BrandId == brandId);
            }
            if (!string.IsNullOrEmpty(status))
            {
                if (status == "active") query = query.Where(p => p.IsActive && p.StockQuantity > 0);
                else if (status == "outofstock") query = query.Where(p => p.StockQuantity <= 0);
                else if (status == "inactive") query = query.Where(p => !p.IsActive);
            }

            // Xử lý sắp xếp
            switch (sort)
            {
                case "price_asc": query = query.OrderBy(p => p.Price); break;
                case "price_desc": query = query.OrderByDescending(p => p.Price); break;
                case "stock": query = query.OrderByDescending(p => p.StockQuantity); break;
                default: query = query.OrderByDescending(p => p.Id); break;
            }

            var total = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Thống kê nhanh
            ViewBag.TotalProducts = await _db.Products.CountAsync();
            ViewBag.ActiveProducts = await _db.Products.CountAsync(p => p.IsActive && p.StockQuantity > 0);
            ViewBag.OutOfStockProducts = await _db.Products.CountAsync(p => p.IsActive && p.StockQuantity <= 0);
            ViewBag.InactiveProducts = await _db.Products.CountAsync(p => !p.IsActive);
            ViewBag.TotalStockValue = await _db.Products.Where(p => p.IsActive).SumAsync(p => p.Price * p.StockQuantity);

            // Truyền dữ liệu filter ra View
            ViewBag.Q = q;
            ViewBag.Categories = new SelectList(await _db.Categories.AsNoTracking().ToListAsync(), "Id", "Name", categoryId);
            ViewBag.Brands = new SelectList(await _db.Brands.AsNoTracking().ToListAsync(), "Id", "Name", brandId);
            ViewBag.Status = status;
            ViewBag.Sort = sort;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize; // Cần cho dropdown hiển thị
            ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);

            return View(items);
        }

        // GET: Admin/Product/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var product = await _db.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Images) // Cần để hiển thị gallery
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();
            return View(product);
        }

        // GET: Admin/Product/Create
        public IActionResult Create()
        {
            PopulateDropdowns();
            var product = new Product { Name = "", Year = DateTime.Now.Year };
            return View(product);
        }

        // POST: Admin/Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product)
        {
            ModelState.Remove("Name"); // Bỏ qua validation cho Name (vì dùng "" làm mặc định)

            if (ModelState.IsValid)
            {
                try
                {
                    product.CreatedAt = DateTime.UtcNow;
                    product.UpdatedAt = DateTime.UtcNow;

                    _db.Add(product);
                    await _db.SaveChangesAsync();
                    TempData["Success"] = "Thêm sản phẩm thành công. Giờ bạn có thể thêm ảnh.";
                    return RedirectToAction(nameof(Edit), new { id = product.Id }); // Chuyển đến trang Edit
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi khi thêm sản phẩm.");
                }
            }
            PopulateDropdowns(product);
            return View(product);
        }

        // GET: Admin/Product/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _db.Products
                                   .Include(p => p.Images) // Cần để quản lý ảnh
                                   .FirstOrDefaultAsync(p => p.Id == id);
            if (product == null) return NotFound();
            PopulateDropdowns(product);
            return View(product);
        }

        // POST: Admin/Product/Edit/5 (Đã sửa lỗi CSDL)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product)
        {
            if (id != product.Id) return NotFound();

            var productInDb = await _db.Products.FindAsync(id);
            if (productInDb == null) return NotFound();

            ModelState.Remove("Name"); // Bỏ qua validation

            if (ModelState.IsValid)
            {
                try
                {
                    // Áp dụng pattern Read-and-Update an toàn
                    productInDb.Name = product.Name;
                    productInDb.SKU = product.SKU;
                    productInDb.Description = product.Description;
                    productInDb.Price = product.Price;
                    productInDb.StockQuantity = product.StockQuantity;
                    productInDb.Year = product.Year;
                    productInDb.CategoryId = product.CategoryId;
                    productInDb.BrandId = product.BrandId;
                    productInDb.IsActive = product.IsActive;
                    productInDb.UpdatedAt = DateTime.UtcNow;

                    await _db.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật sản phẩm thành công";
                    return RedirectToAction(nameof(Edit), new { id = product.Id }); // Ở lại trang Edit
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi khi cập nhật.");
                }
            }

            // Nếu lỗi, tải lại Images để hiển thị
            await _db.Entry(productInDb).Collection(p => p.Images).LoadAsync();
            product.Images = productInDb.Images;

            PopulateDropdowns(product);
            return View(product);
        }

        // GET: Admin/Product/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _db.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (product == null) return NotFound();
            return View(product);
        }

        // POST: Admin/Product/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _db.Products.FindAsync(id);
            if (product != null)
            {
                try
                {
                    _db.Products.Remove(product);
                    await _db.SaveChangesAsync();
                    TempData["Success"] = "Đã xóa sản phẩm.";
                }
                catch (DbUpdateException) // Bắt lỗi khoá ngoại
                {
                    TempData["Error"] = "Không thể xoá sản phẩm này (có thể do khoá ngoại).";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Đã xảy ra lỗi khi xoá: " + ex.Message;
                }
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Product/AddImage (Đã sửa lỗi SortOrder)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddImage(int ProductId, IFormFile file, string Caption)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Bạn cần chọn một file ảnh.";
                return RedirectToAction(nameof(Edit), new { id = ProductId });
            }

            var product = await _db.Products.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == ProductId);
            if (product == null) return NotFound();

            // 1. Lưu file vật lý
            string wwwRootPath = _webHostEnvironment.WebRootPath;
            string uploadPath = Path.Combine(wwwRootPath, "images", "products");
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            string fileExtension = Path.GetExtension(file.FileName);
            string uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            string physicalPath = Path.Combine(uploadPath, uniqueFileName);

            try
            {
                await using (var fileStream = new FileStream(physicalPath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi lưu file: {ex.Message}";
                return RedirectToAction(nameof(Edit), new { id = ProductId });
            }

            string publicUrl = $"/images/products/{uniqueFileName}";

            // Sửa lỗi SortOrder
            int nextSortOrder = 0;
            if (product.Images.Any())
            {
                nextSortOrder = product.Images.Max(i => i.SortOrder) + 1;
            }

            // 3. Lưu vào CSDL
            var newImage = new ProductImage
            {
                ProductId = ProductId,
                ImageUrl = publicUrl,
                Caption = Caption ?? "",
                IsPrimary = !product.Images.Any(i => i.IsPrimary), // Ảnh đầu tiên là Primary
                SortOrder = nextSortOrder // Gán SortOrder đã tính toán
            };

            _db.ProductImages.Add(newImage);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Thêm ảnh mới thành công.";
            return RedirectToAction(nameof(Edit), new { id = ProductId });
        }

        // POST: Admin/Product/DeleteImage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteImage(int id) // id của ProductImage
        {
            var image = await _db.ProductImages.FindAsync(id);
            if (image == null)
            {
                TempData["Error"] = "Không tìm thấy ảnh để xoá.";
                return RedirectToAction(nameof(Index));
            }

            var productId = image.ProductId;

            // 1. Xoá file vật lý
            try
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                string physicalPath = Path.Combine(wwwRootPath, image.ImageUrl.TrimStart('/'));

                if (System.IO.File.Exists(physicalPath))
                {
                    System.IO.File.Delete(physicalPath);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi khi xoá file vật lý: {ex.Message}");
            }

            // 2. Xoá khỏi CSDL
            _db.ProductImages.Remove(image);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Đã xoá ảnh.";
            return RedirectToAction(nameof(Edit), new { id = productId });
        }


        // Hàm private để tải Dropdowns
        private void PopulateDropdowns(Product? product = null)
        {
            ViewBag.BrandId = new SelectList(_db.Brands.AsNoTracking(), "Id", "Name", product?.BrandId);
            ViewBag.CategoryId = new SelectList(_db.Categories.AsNoTracking(), "Id", "Name", product?.CategoryId);
        }
    }
}