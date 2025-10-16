using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MotorShop.Data;
using MotorShop.Models;
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

        // Action xử lý POST từ form thanh toán
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
                    Status = 0, // 0: Pending
                    CustomerName = viewModel.CustomerName,
                    ShippingAddress = viewModel.ShippingAddress,
                    ShippingPhone = viewModel.ShippingPhone,
                    TotalAmount = cartItems.Sum(item => item.UnitPrice * item.Quantity),

                    // ✨✨✨ SỬA LỖI Ở ĐÂY ✨✨✨
                    // Tạo danh sách OrderItem mới thay vì gán trực tiếp từ giỏ hàng.
                    // Điều này ngăn EF Core theo dõi và cố gắng tạo lại các Product đã có.
                    OrderItems = cartItems.Select(cartItem => new OrderItem
                    {
                        ProductId = cartItem.ProductId, // Chỉ cần ID để tạo mối quan hệ
                        Quantity = cartItem.Quantity,
                        UnitPrice = cartItem.UnitPrice
                    }).ToList()
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync(); // <-- Lỗi sẽ không còn xảy ra ở đây

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
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var orders = await _context.Orders
                                    .Where(o => o.UserId == userId)
                                    .OrderByDescending(o => o.OrderDate)
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