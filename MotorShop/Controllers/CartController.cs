using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        // GET: /Cart (Trang giỏ hàng chính)
        public IActionResult Index()
        {
            var cartItems = _cartService.GetCartItems();
            return View(cartItems);
        }

        // ✨ ENDPOINT API MỚI DÙNG CHO AJAX ✨
        // POST: /Cart/AddToCart
        [HttpPost]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
        {
            if (request == null || request.ProductId <= 0)
            {
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ." });
            }

            var product = await _context.Products.FindAsync(request.ProductId);
            if (product == null)
            {
                return NotFound(new { success = false, message = "Sản phẩm không tồn tại." });
            }

            _cartService.AddToCart(product);

            var cartItemCount = _cartService.GetCartItems().Sum(i => i.Quantity);
            return Ok(new
            {
                success = true,
                message = $"Đã thêm {product.Name} vào giỏ!",
                cartCount = cartItemCount
            });
        }

        // Action xóa sản phẩm khỏi giỏ hàng (trên trang Cart chính)
        [HttpPost]
        public IActionResult Remove(int productId)
        {
            _cartService.RemoveFromCart(productId);
            return RedirectToAction("Index");
        }
    }

    // Class để nhận dữ liệu từ AJAX
    public class AddToCartRequest
    {
        public int ProductId { get; set; }
    }
}