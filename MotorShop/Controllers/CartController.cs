using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MotorShop.Data;
using MotorShop.Models; // Namespace chứa CartItem và Product
using MotorShop.Services;
using System.ComponentModel.DataAnnotations; // Cần cho AddToCartRequest

namespace MotorShop.Controllers;

public class CartController(CartService cartService, ApplicationDbContext context, ILogger<CartController> logger) : Controller
{
    // GET: /Cart (Hiển thị trang giỏ hàng)
    public IActionResult Index()
    {
        var cartItems = cartService.GetCartItems();
        // Lấy thông tin Product đầy đủ để hiển thị (tùy chọn, có thể bỏ qua nếu CartItem đã đủ thông tin)
        // var productIds = cartItems.Select(ci => ci.ProductId).ToList();
        // var products = context.Products.Where(p => productIds.Contains(p.Id)).AsNoTracking().ToList();
        // // Gán lại Product cho cartItems nếu cần (ví dụ: lấy Brand.Name)

        return View(cartItems);
    }

    // --- API ENDPOINTS CHO AJAX ---

    [HttpPost]
    // [ValidateAntiForgeryToken] // Cần thêm cơ chế gửi token từ JS nếu bật
    public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ." });
        }

        try
        {
            var product = await context.Products.FindAsync(request.ProductId);
            if (product == null)
            {
                return NotFound(new { success = false, message = "Sản phẩm không tồn tại." });
            }

            // Kiểm tra tồn kho trước khi thêm
            var currentQuantityInCart = cartService.GetItemQuantity(request.ProductId);
            if (product.StockQuantity < currentQuantityInCart + request.Quantity)
            {
                return BadRequest(new { success = false, message = $"Số lượng tồn kho không đủ! Chỉ còn {product.StockQuantity} sản phẩm." });
            }

            cartService.AddToCart(product, request.Quantity);

            var totalQuantity = cartService.GetCartItems().Sum(i => i.Quantity);
            return Ok(new
            {
                success = true,
                message = $"Đã thêm '{product.Name}' vào giỏ!",
                cartCount = totalQuantity
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi khi thêm vào giỏ hàng (API)");
            return StatusCode(500, new { success = false, message = "Lỗi máy chủ khi thêm vào giỏ." });
        }
    }

    [HttpPost]
    // [ValidateAntiForgeryToken]
    public IActionResult UpdateQuantity([FromBody] UpdateQuantityRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Số lượng không hợp lệ." });
        }

        try
        {
            var product = context.Products.AsNoTracking().FirstOrDefault(p => p.Id == request.ProductId);
            if (product == null)
            {
                return NotFound(new { success = false, message = "Sản phẩm không tồn tại." });
            }

            // Kiểm tra tồn kho
            if (product.StockQuantity < request.Quantity)
            {
                return BadRequest(new { success = false, message = $"Số lượng tồn kho không đủ (còn {product.StockQuantity}).", currentStock = product.StockQuantity });
            }

            bool updated = cartService.UpdateQuantity(request.ProductId, request.Quantity);

            if (updated)
            {
                var cartItems = cartService.GetCartItems();
                var totalQuantity = cartItems.Sum(i => i.Quantity);
                // Tính lại tổng tiền cho toàn bộ giỏ hàng (nếu cần trả về)
                var newTotalAmount = cartItems.Sum(i => i.Subtotal);

                return Ok(new
                {
                    success = true,
                    message = "Cập nhật số lượng thành công!",
                    cartCount = totalQuantity,
                    newTotalAmount = newTotalAmount // Gửi về tổng tiền mới
                });
            }
            else
            {
                return NotFound(new { success = false, message = "Không tìm thấy sản phẩm trong giỏ." });
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi khi cập nhật số lượng (API)");
            return StatusCode(500, new { success = false, message = "Lỗi máy chủ khi cập nhật số lượng." });
        }
    }

    [HttpPost]
    // [ValidateAntiForgeryToken]
    public IActionResult RemoveItem([FromBody] RemoveItemRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "ID sản phẩm không hợp lệ." });
        }

        try
        {
            cartService.RemoveFromCart(request.ProductId);
            var cartItems = cartService.GetCartItems();
            var totalQuantity = cartItems.Sum(i => i.Quantity);
            var newTotalAmount = cartItems.Sum(i => i.Subtotal);

            return Ok(new
            {
                success = true,
                message = "Đã xóa sản phẩm khỏi giỏ!",
                cartCount = totalQuantity,
                newTotalAmount = newTotalAmount
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi khi xóa sản phẩm khỏi giỏ (API)");
            return StatusCode(500, new { success = false, message = "Lỗi máy chủ khi xóa sản phẩm." });
        }
    }

    // --- Action Xóa (submit form từ trang Cart Index) ---
    // Giữ lại action này nếu bạn vẫn muốn có nút xóa submit form truyền thống
    // Hoặc xóa đi nếu chỉ dùng AJAX
    [HttpPost]
    // [ValidateAntiForgeryToken]
    public IActionResult Remove(int productId)
    {
        cartService.RemoveFromCart(productId);
        TempData["success"] = "Đã xóa sản phẩm khỏi giỏ hàng.";
        return RedirectToAction("Index");
    }
}

// --- Request Models cho API ---
public class AddToCartRequest
{
    [Required]
    public int ProductId { get; set; }

    [Range(1, 100, ErrorMessage = "Số lượng phải từ 1 đến 100.")]
    public int Quantity { get; set; } = 1;
}

public class UpdateQuantityRequest
{
    [Required]
    public int ProductId { get; set; }

    [Required]
    [Range(0, 100, ErrorMessage = "Số lượng phải từ 0 đến 100.")] // Cho phép quantity = 0 để xóa
    public int Quantity { get; set; }
}

public class RemoveItemRequest
{
    [Required]
    public int ProductId { get; set; }
}