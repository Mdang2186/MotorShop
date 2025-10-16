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

        public async Task<IActionResult> Index(string searchString, int? brandFilter, int? categoryFilter, string sortBy, int pageIndex = 1)
        {
            var productsQuery = _context.Products
                                        .Include(p => p.Brand)
                                        .Include(p => p.Category)
                                        .AsNoTracking() // Performance optimization
                                        .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                productsQuery = productsQuery.Where(p => p.Name.Contains(searchString) || p.Brand.Name.Contains(searchString));
            }
            if (brandFilter.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.BrandId == brandFilter);
            }
            if (categoryFilter.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.CategoryId == categoryFilter);
            }

            productsQuery = sortBy switch
            {
                "price-low" => productsQuery.OrderBy(p => p.Price),
                "price-high" => productsQuery.OrderByDescending(p => p.Price),
                _ => productsQuery.OrderByDescending(p => p.Id),
            };

            int pageSize = 9;
            var totalItems = await productsQuery.CountAsync();
            var products = await productsQuery.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();

            var viewModel = new ProductIndexViewModel
            {
                Products = products,
                Brands = new SelectList(await _context.Brands.AsNoTracking().ToListAsync(), "Id", "Name", brandFilter),
                Categories = new SelectList(await _context.Categories.AsNoTracking().ToListAsync(), "Id", "Name", categoryFilter),
                SearchString = searchString,
                BrandFilter = brandFilter,
                CategoryFilter = categoryFilter,
                SortBy = sortBy,
                PageIndex = pageIndex,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
            };

            // If the request is from AJAX, return only the partial view for the product grid
            if (HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_ProductGridPartial", viewModel);
            }

            return View(viewModel);
        }

        // Action to show product details in a modal
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                                        .Include(p => p.Brand)
                                        .Include(p => p.Category)
                                        .AsNoTracking()
                                        .FirstOrDefaultAsync(p => p.Id == id);
            if (product == null) return NotFound();

            return PartialView("_ProductDetailPartial", product);
        }
    }
}