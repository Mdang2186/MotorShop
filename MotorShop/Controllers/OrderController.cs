using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MotorShop.Data;
using MotorShop.Models;
using MotorShop.Models.Enums;
using MotorShop.Services;
using MotorShop.ViewModels;
using System.Security.Claims;

namespace MotorShop.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly CartService _cartService;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public OrderController(CartService cartService, ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _cartService = cartService;
            _context = context;
            _userManager = userManager;
        }

        // Action xử lý trang thanh toán
        public async Task<IActionResult> Checkout()
        {
            var cartItems = _cartService.GetCartItems();
            if (!cartItems.Any()) return RedirectToAction("Index", "Cart");
            var user = await _userManager.GetUserAsync(User);
            var viewModel = new CheckoutViewModel
            {
                CartItems = cartItems,
                CustomerName = user?.FullName ?? "",
                ShippingAddress = user?.Address ?? "",
                ShippingPhone = user?.PhoneNumber ?? ""
            };
            return View(viewModel);
        }
        // POST: /Order/Checkout
        // Xử lý việc đặt hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutViewModel viewModel)
        {
            var cartItems = _cartService.GetCartItems();
            if (!cartItems.Any())
            {
                ModelState.AddModelError("", "Giỏ hàng của bạn đang trống.");
            }

            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                var order = new Order
                {
                    UserId = user.Id,
                    OrderDate = DateTime.UtcNow,
                    Status = OrderStatus.Pending,
                    CustomerName = viewModel.CustomerName,
                    ShippingAddress = viewModel.ShippingAddress,
                    ShippingPhone = viewModel.ShippingPhone,

                    // SỬA LỖI Ở ĐÂY: Dùng "item.Price"
                    TotalAmount = cartItems.Sum(item => item.Price * item.Quantity),

                    OrderItems = cartItems.Select(cartItem => new OrderItem
                    {
                        ProductId = cartItem.ProductId,
                        Quantity = cartItem.Quantity,
                        // VÀ SỬA LỖI Ở ĐÂY: Dùng "cartItem.Price"
                        UnitPrice = cartItem.Price
                    }).ToList()
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                _cartService.ClearCart();
                return RedirectToAction(nameof(OrderConfirmation), new { id = order.Id });
            }

            viewModel.CartItems = cartItems;
            return View(viewModel);
        }

        // Action xác nhận đơn hàng thành công
        public IActionResult OrderConfirmation(int id)
        {
            return View(id);
        }

        // Action lịch sử đơn hàng
        public async Task<IActionResult> History()
        {
            // Lấy ID của người dùng đang đăng nhập
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Truy vấn CSDL để lấy các đơn hàng của người dùng này
            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                // YÊU CẦU QUAN TRỌNG: Lấy kèm theo danh sách các mục trong đơn hàng
                .Include(o => o.OrderItems)
                    // VỚI MỖI MỤC, LẤY KÈM THEO THÔNG TIN CHI TIẾT CỦA SẢN PHẨM
                    .ThenInclude(oi => oi.Product)
                .OrderByDescending(o => o.OrderDate) // Sắp xếp đơn hàng mới nhất lên đầu
                .AsNoTracking() // Tối ưu hiệu năng vì chỉ đọc dữ liệu
                .ToListAsync();

            return View(orders);
        }

        // Action xem chi tiết đơn hàng
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (order == null || order.UserId != userId) return Forbid();

            return View(order);
        }
    }
}