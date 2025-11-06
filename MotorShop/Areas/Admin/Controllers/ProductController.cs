using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MotorShop.Data;
using MotorShop.Models;
using MotorShop.Utilities;

namespace MotorShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Manager")]
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _db;
        public ProductController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index(string? q, int? categoryId, string? status, int page = 1)
        {
            int pageSize = 10;
            var query = _db.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Images)
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
            if (!string.IsNullOrEmpty(status))
            {
                if (status == "active") query = query.Where(p => p.IsActive && p.StockQuantity > 0);
                else if (status == "outofstock") query = query.Where(p => p.StockQuantity <= 0);
                else if (status == "inactive") query = query.Where(p => !p.IsActive);
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(p => p.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Q = q;
            ViewBag.CategoryId = new SelectList(await _db.Categories.AsNoTracking().ToListAsync(), "Id", "Name", categoryId);
            ViewBag.Status = status;
            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);

            return View(items);
        }

        public IActionResult Create()
        {
            PopulateDropdowns();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product)
        {
            if (ModelState.IsValid)
            {
                _db.Add(product);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Thêm sản phẩm thành công";
                return RedirectToAction(nameof(Index));
            }
            PopulateDropdowns(product);
            return View(product);
        }

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
            if (ModelState.IsValid)
            {
                _db.Update(product);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Cập nhật sản phẩm thành công";
                return RedirectToAction(nameof(Index));
            }
            PopulateDropdowns(product);
            return View(product);
        }

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
                TempData["Success"] = "Đã xóa sản phẩm.";
            }
            return RedirectToAction(nameof(Index));
        }

        private void PopulateDropdowns(Product? product = null)
        {
            ViewBag.BrandId = new SelectList(_db.Brands.AsNoTracking(), "Id", "Name", product?.BrandId);
            ViewBag.CategoryId = new SelectList(_db.Categories.AsNoTracking(), "Id", "Name", product?.CategoryId);
        }
    }
}