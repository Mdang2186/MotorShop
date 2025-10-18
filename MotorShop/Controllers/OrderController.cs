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

namespace MotorShop.Controllers;

[Authorize]
public class OrderController(
    CartService cartService,
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager) : Controller
{
    // GET: /Order/Checkout
    // Nhận danh sách ID sản phẩm được chọn từ giỏ hàng
    public async Task<IActionResult> Checkout(string selectedProductIds)
    {
        var allCartItems = cartService.GetCartItems();
        List<CartItem> itemsToCheckout = [];

        if (!string.IsNullOrEmpty(selectedProductIds))
        {
            var ids = selectedProductIds.Split(',')
                                        .Select(int.Parse)
                                        .ToList();
            itemsToCheckout = allCartItems.Where(ci => ids.Contains(ci.ProductId)).ToList();
        }
        else // Nếu không có ID nào được gửi, có thể lấy tất cả (tùy logic mong muốn)
        {
            itemsToCheckout = allCartItems;
        }


        if (itemsToCheckout.Count == 0)
        {
            TempData["error"] = "Vui lòng chọn sản phẩm trong giỏ hàng để thanh toán.";
            return RedirectToAction("Index", "Cart");
        }

        var user = await userManager.GetUserAsync(User);

        var viewModel = new CheckoutViewModel
        {
            CartItems = itemsToCheckout, // Chỉ chứa các item được chọn
            CustomerName = user?.FullName ?? "",
            ShippingAddress = user?.Address ?? "",
            ShippingPhone = user?.PhoneNumber ?? ""
        };

        return View(viewModel);
    }

    // POST: /Order/Checkout
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(CheckoutViewModel viewModel)
    {
        // Lấy lại các items được chọn dựa trên thông tin ẩn hoặc logic khác nếu cần kiểm tra lại
        // Ở đây, ta tin tưởng viewModel đã chứa đúng các items cần checkout
        var itemsToCheckout = viewModel.CartItems ?? []; // Cần đảm bảo viewModel.CartItems không null

        if (itemsToCheckout.Count == 0)
        {
            ModelState.AddModelError("", "Không có sản phẩm nào được chọn để thanh toán.");
            // Cần lấy lại tất cả cart items để hiển thị lại form nếu lỗi
            viewModel.CartItems = cartService.GetCartItems();
            return View(viewModel); // Trả về view với lỗi
        }

        if (ModelState.IsValid)
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null) return Challenge(); // Chưa đăng nhập

            var order = new Order
            {
                UserId = user.Id,
                OrderDate = DateTime.UtcNow,
                Status = OrderStatus.Pending,
                CustomerName = viewModel.CustomerName,
                ShippingAddress = viewModel.ShippingAddress,
                ShippingPhone = viewModel.ShippingPhone,
                TotalAmount = itemsToCheckout.Sum(item => item.Subtotal), // Tính tổng từ CartItem

                // Chuyển đổi từ CartItem (ViewModel/Session) sang OrderItem (Database Model)
                OrderItems = itemsToCheckout.Select(cartItem => new OrderItem
                {
                    ProductId = cartItem.ProductId,
                    Quantity = cartItem.Quantity,
                    UnitPrice = cartItem.Price // Lấy giá từ CartItem
                }).ToList()
            };

            //----- KIỂM TRA TỒN KHO TRƯỚC KHI LƯU (QUAN TRỌNG) -----
            var productIds = order.OrderItems.Select(oi => oi.ProductId).ToList();
            var productsInDb = await context.Products
                                        .Where(p => productIds.Contains(p.Id))
                                        .ToDictionaryAsync(p => p.Id, p => p.StockQuantity);

            foreach (var item in order.OrderItems)
            {
                if (!productsInDb.TryGetValue(item.ProductId, out var stock) || stock < item.Quantity)
                {
                    ModelState.AddModelError("", $"Sản phẩm '{context.Products.Find(item.ProductId)?.Name ?? item.ProductId.ToString()}' không đủ số lượng tồn kho (còn {stock}).");
                    viewModel.CartItems = cartService.GetCartItems(); // Lấy lại giỏ hàng đầy đủ
                    return View(viewModel); // Trả về view với lỗi
                }
            }
            //----- KẾT THÚC KIỂM TRA TỒN KHO -----


            using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                // Lưu đơn hàng
                context.Orders.Add(order);
                await context.SaveChangesAsync();

                // Cập nhật tồn kho
                foreach (var item in order.OrderItems)
                {
                    var product = await context.Products.FindAsync(item.ProductId);
                    if (product != null)
                    {
                        product.StockQuantity -= item.Quantity;
                    }
                }
                await context.SaveChangesAsync();

                await transaction.CommitAsync();

                // Xóa các sản phẩm đã mua khỏi giỏ hàng
                cartService.RemovePurchasedItems(order.OrderItems.Select(oi => oi.ProductId));

                return RedirectToAction(nameof(OrderConfirmation), new { id = order.Id });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // Ghi log lỗi
                ModelState.AddModelError("", "Đã có lỗi xảy ra trong quá trình đặt hàng. Vui lòng thử lại.");
                viewModel.CartItems = cartService.GetCartItems(); // Lấy lại giỏ hàng đầy đủ
                return View(viewModel);
            }
        }

        // Nếu ModelState không hợp lệ, trả lại view với dữ liệu và lỗi
        viewModel.CartItems = cartService.GetCartItems(); // Lấy lại giỏ hàng đầy đủ
        return View(viewModel);
    }

    // GET: /Order/OrderConfirmation/5
    public IActionResult OrderConfirmation(int id)
    {
        // Có thể lấy thông tin đơn hàng ở đây để hiển thị chi tiết hơn
        return View(id);
    }

    // GET: /Order/History
    public async Task<IActionResult> History()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var orders = await context.Orders
                                .Where(o => o.UserId == userId)
                                .Include(o => o.OrderItems)
                                    .ThenInclude(oi => oi.Product) // Tải kèm sản phẩm
                                .OrderByDescending(o => o.OrderDate)
                                .AsNoTracking()
                                .ToListAsync();
        return View(orders);
    }

    // GET: /Order/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var order = await context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product) // Tải kèm sản phẩm
            .FirstOrDefaultAsync(m => m.Id == id);

        if (order == null || order.UserId != userId) return Forbid(); // Đảm bảo đúng chủ đơn hàng

        return View(order);
    }
}