using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MotorShop.Data;
using MotorShop.Models;
using MotorShop.Services;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MotorShop.Controllers
{
    public class CartController : Controller
    {
        private readonly CartService _cartService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CartController> _logger; // Thêm ILogger để ghi log lỗi

        public CartController(CartService cartService, ApplicationDbContext context, ILogger<CartController> logger)
        {
            _cartService = cartService;
            _context = context;
            _logger = logger;
        }

        // GET: /Cart (Hiển thị trang giỏ hàng)
        public IActionResult Index()
        {
            var cartItems = _cartService.GetCartItems();
            return View(cartItems);
        }

        /// <summary>
        /// API Endpoint để thêm sản phẩm vào giỏ hàng bằng AJAX.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken] // Tăng cường bảo mật, chống tấn công CSRF
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ.", errors });
            }

            try
            {
                var product = await _context.Products.FindAsync(request.ProductId);
                if (product == null)
                {
                    return NotFound(new { success = false, message = "Sản phẩm không tồn tại." });
                }

                var currentQuantityInCart = _cartService.GetItemQuantity(request.ProductId);
                if (product.StockQuantity < currentQuantityInCart + request.Quantity)
                {
                    return BadRequest(new { success = false, message = $"Số lượng tồn kho không đủ! Chỉ còn {product.StockQuantity} sản phẩm." });
                }

                _cartService.AddToCart(product, request.Quantity);

                var cartItemCount = _cartService.GetCartItems().Sum(i => i.Quantity);
                return Ok(new
                {
                    success = true,
                    message = $"Đã thêm '{product.Name}' vào giỏ!",
                    cartCount = cartItemCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra khi thêm sản phẩm vào giỏ hàng.");
                return StatusCode(500, new { success = false, message = "Đã có lỗi xảy ra ở máy chủ. Vui lòng thử lại." });
            }
        }

        /// <summary>
        /// Action xóa một sản phẩm khỏi giỏ hàng.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Remove(int productId)
        {
            _cartService.RemoveFromCart(productId);
            TempData["success"] = "Đã xóa sản phẩm khỏi giỏ hàng.";
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Action cập nhật số lượng của một sản phẩm trong giỏ hàng.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Update(int productId, int quantity)
        {
            if (quantity <= 0)
            {
                // Nếu số lượng là 0 hoặc âm, coi như xóa sản phẩm
                return Remove(productId);
            }
            _cartService.UpdateQuantity(productId, quantity);
            return RedirectToAction("Index");
        }
    }

    /// <summary>
    /// DTO (Data Transfer Object) để nhận dữ liệu từ yêu cầu AJAX AddToCart.
    /// </summary>
    public class AddToCartRequest
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        [Range(1, 100, ErrorMessage = "Số lượng phải từ 1 đến 100.")]
        public int Quantity { get; set; } = 1;
    }
}