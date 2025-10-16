using Microsoft.AspNetCore.Mvc;
using MotorShop.Data;
using MotorShop.Models;
using MotorShop.Services;

namespace MotorShop.Controllers
{
    public class CartController : Controller
    {
        private readonly CartService _cartService;
        private readonly ApplicationDbContext _context;

        public CartController(CartService cartService, ApplicationDbContext context)
        {
            _cartService = cartService;
            _context = context;
        }

        // GET: /Cart (Standard cart page)
        public IActionResult Index()
        {
            var cartItems = _cartService.GetCartItems();
            // This logic is a simplified version. A real app would re-fetch product details here.
            return View(cartItems);
        }

        // POST API: /Cart/AddToCartApi
        [HttpPost]
        public async Task<IActionResult> AddToCartApi([FromBody] AddToCartRequest request)
        {
            var product = await _context.Products.FindAsync(request.ProductId);
            if (product == null)
            {
                return NotFound(new { success = false, message = "Sản phẩm không tồn tại." });
            }

            _cartService.AddToCart(product);

            var cartItemCount = _cartService.GetCartItems().Sum(i => i.Quantity);
            return Ok(new { success = true, message = $"Đã thêm {product.Name} vào giỏ!", cartCount = cartItemCount });
        }

        // POST: /Cart/Remove/5 (For the main cart page)
        [HttpPost]
        public IActionResult Remove(int productId)
        {
            _cartService.RemoveFromCart(productId);
            return RedirectToAction("Index");
        }
    }

    public class AddToCartRequest
    {
        public int ProductId { get; set; }
    }
}