using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MotorShop.Data;
using MotorShop.ViewModels;
using MotorShop.Models;

namespace MotorShop.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string searchString, int? brandFilter, int? categoryFilter, string sortBy, decimal? minPrice, decimal? maxPrice, int page = 1)
        {
            var productsQuery = _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .AsNoTracking() // Tối ưu hiệu năng
                .AsQueryable();

            // 1. Lọc theo chuỗi tìm kiếm
            if (!string.IsNullOrEmpty(searchString))
            {
                productsQuery = productsQuery.Where(p => p.Name.Contains(searchString) || p.Brand.Name.Contains(searchString));
            }
            // 2. Lọc theo thương hiệu
            if (brandFilter.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.BrandId == brandFilter.Value);
            }
            // 3. Lọc theo loại xe
            if (categoryFilter.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.CategoryId == categoryFilter.Value);
            }
            if (minPrice.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.Price >= minPrice.Value);
            }
            if (maxPrice.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.Price <= maxPrice.Value);
            }
            // 4. Sắp xếp
            productsQuery = sortBy switch
            {
                "price-low" => productsQuery.OrderBy(p => p.Price),
                "price-high" => productsQuery.OrderByDescending(p => p.Price),
                _ => productsQuery.OrderByDescending(p => p.Id), // Mặc định là mới nhất
            };

            // 5. Phân trang
            int pageSize = 9;
            var totalItems = await productsQuery.CountAsync();
            var products = await productsQuery.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            var viewModel = new ProductIndexViewModel
            {
                Products = products,
                // Tạo SelectList hiệu quả, tự động chọn item đã lọc
                Brands = new SelectList(await _context.Brands.AsNoTracking().ToListAsync(), "Id", "Name", brandFilter),
                Categories = new SelectList(await _context.Categories.AsNoTracking().ToListAsync(), "Id", "Name", categoryFilter),

                // Giữ lại trạng thái để hiển thị trên View
                SearchString = searchString,
                BrandFilter = brandFilter,
                CategoryFilter = categoryFilter,
                SortBy = sortBy,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                // Thông tin phân trang
                PageIndex = page,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
            };

            // Nếu là yêu cầu AJAX, chỉ trả về phần lưới sản phẩm
            if (HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_ProductGridPartial", viewModel.Products); // Chỉ cần truyền danh sách sản phẩm
            }

            return View(viewModel);
        }

        // GET: /Products/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            // Lấy 4 sản phẩm liên quan (cùng loại xe, khác sản phẩm hiện tại)
            var relatedProducts = await _context.Products
                .Include(p => p.Brand)
                .Where(p => p.CategoryId == product.CategoryId && p.Id != id)
                .AsNoTracking()
                .Take(4)
                .ToListAsync();

            var viewModel = new ProductDetailViewModel
            {
                Product = product,
                RelatedProducts = relatedProducts
            };

            return View(viewModel);
        }
        // Thêm action mới này vào trong class ProductsController

        // GET: /Products/Details/5
        
    }
}