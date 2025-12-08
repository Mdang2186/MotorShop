using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MotorShop.Data;
using MotorShop.Models;
using MotorShop.Services;
using MotorShop.Utilities;
using MotorShop.ViewModels.Cart;

namespace MotorShop.Controllers
{
    [AutoValidateAntiforgeryToken]
    public class CartController(
        CartService cartService,
        ApplicationDbContext context,
        ILogger<CartController> logger) : Controller
    {
        // =========================
        // Helpers (JSON Response)
        // =========================
        private IActionResult JSuccess(string? message = null)
        {
            var items = cartService.GetCartItems();
            return Ok(new
            {
                success = true,
                message,
                cartCount = items.Sum(i => i.Quantity),
                newTotalAmount = items.Sum(i => i.Subtotal)
            });
        }

        private IActionResult JFail(int status, string message, object? extra = null)
        {
            var payload = new Dictionary<string, object?>
            {
                ["success"] = false,
                ["message"] = message
            };
            if (extra != null)
            {
                foreach (var kv in extra.GetType().GetProperties())
                    payload[kv.Name] = kv.GetValue(extra);
            }
            return StatusCode(status, payload);
        }

        // Helper: Map cart session -> ViewModel (kèm dữ liệu tươi từ DB)
        private async Task<List<CartItemVm>> BuildVmAsync(CancellationToken ct)
        {
            var snapshot = cartService.GetCartItems();
            if (snapshot.Count == 0) return [];

            var ids = snapshot.Select(i => i.ProductId).Distinct().ToArray();
            var products = await context.Products
                .AsNoTracking()
                .Where(p => ids.Contains(p.Id) && p.IsPublished)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Price,
                    p.ImageUrl,
                    p.StockQuantity,
                    p.Slug
                })
                .ToListAsync(ct);

            var map = products.ToDictionary(p => p.Id);
            var list = new List<CartItemVm>(snapshot.Count);

            foreach (var it in snapshot)
            {
                if (!map.TryGetValue(it.ProductId, out var p)) continue;

                list.Add(new CartItemVm
                {
                    ProductId = it.ProductId,
                    Name = p.Name ?? "Sản phẩm",
                    ImageUrl = string.IsNullOrWhiteSpace(p.ImageUrl) ? "/images/products/placeholder.png" : p.ImageUrl,
                    UnitPrice = p.Price,
                    Quantity = it.Quantity,
                    // Có thể map thêm MaxStock nếu view cần validation
                });
            }
            return list;
        }

        // =========================
        // PAGE: GIỎ HÀNG
        // GET: /Cart
        // =========================
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            // 1. Refresh giá & tồn kho từ DB
            var snap = cartService.GetCartItems();
            if (snap.Count > 0)
            {
                var ids = snap.Select(i => i.ProductId).Distinct().ToArray();
                var fresh = await context.Products
                                         .Where(p => ids.Contains(p.Id))
                                         .AsNoTracking()
                                         .ToListAsync(ct);

                // Cập nhật giá mới nhất vào session
                cartService.RefreshPricesFrom(fresh);

                // Loại bỏ sản phẩm đã bị ẩn/xóa/hết hàng hoàn toàn
                var validIds = fresh.Where(p => p.IsPublished).Select(p => p.Id).ToHashSet();
                var removed = 0;
                foreach (var it in cartService.GetCartItems().ToList())
                {
                    if (!validIds.Contains(it.ProductId))
                    {
                        cartService.RemoveFromCart(it.ProductId);
                        removed++;
                    }
                }
                if (removed > 0)
                    TempData[SD.Temp_Info] = $"Đã loại {removed} sản phẩm không còn kinh doanh khỏi giỏ.";
            }

            // 2. Build View Model
            var vmItems = await BuildVmAsync(ct);
            return View(vmItems);
        }

        // =========================
        // ACTION: MUA NGAY (Direct Checkout)
        // GET: /Cart/BuyNow?productId=...
        // =========================
        [HttpGet]
        public async Task<IActionResult> BuyNow(int productId, int quantity = 1, CancellationToken ct = default)
        {
            // 1. Kiểm tra sản phẩm
            var product = await context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == productId && p.IsPublished, ct);

            if (product == null)
            {
                TempData[SD.Temp_Error] = "Sản phẩm không tồn tại hoặc ngừng kinh doanh.";
                return RedirectToAction("Index", "Home");
            }

            if (product.StockQuantity <= 0)
            {
                TempData[SD.Temp_Warning] = "Sản phẩm này tạm thời hết hàng.";
                // Quay lại trang chi tiết để xem thông tin
                return RedirectToAction("Details", "Products", new { id = productId });
            }

            // 2. Tính toán số lượng có thể thêm
            var inCart = cartService.GetItemQuantity(productId);
            var canAdd = Math.Max(0, product.StockQuantity - inCart);
            var addQty = Math.Min(Math.Max(1, quantity), canAdd);

            if (addQty > 0)
            {
                cartService.Add(product, addQty);
            }
            else
            {
                // Nếu giỏ đã có đủ số lượng tồn kho, vẫn cho qua checkout nhưng cảnh báo nhẹ
                // Hoặc không làm gì cả
            }

            // 3. Chuyển hướng thẳng sang Checkout
            // Tham số 'selected' giúp trang Checkout chỉ tick chọn sản phẩm này (nếu logic checkout hỗ trợ partial payment)
            // Nếu checkout luôn thanh toán hết giỏ, tham số này có thể bỏ qua.
            return RedirectToAction("Index", "Checkout", new { selected = productId });
        }

        // =========================
        // API: THÊM VÀO GIỎ (AJAX)
        // POST: /Cart/AddToCart
        // =========================
        [HttpPost]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return JFail(400, "Dữ liệu không hợp lệ.");

            try
            {
                var product = await context.Products
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == request.ProductId && p.IsPublished, ct);

                if (product == null)
                    return JFail(404, "Sản phẩm không tồn tại.");

                if (product.StockQuantity <= 0)
                    return JFail(400, "Sản phẩm đã hết hàng.");

                // Kiểm tra giới hạn tồn kho
                var quantityInCart = cartService.GetItemQuantity(request.ProductId);
                var canAdd = Math.Max(0, product.StockQuantity - quantityInCart);
                var addQty = Math.Min(Math.Max(1, request.Quantity), canAdd);

                if (addQty <= 0)
                {
                    return JFail(400, $"Bạn đã thêm tối đa số lượng tồn kho ({product.StockQuantity}).",
                        new { currentStock = product.StockQuantity, allowedToAdd = 0 });
                }

                cartService.Add(product, addQty);
                return JSuccess($"Đã thêm '{product.Name}' vào giỏ.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Lỗi AddToCart: {@req}", request);
                return JFail(500, "Lỗi hệ thống.");
            }
        }

        // =========================
        // API: CẬP NHẬT SỐ LƯỢNG (AJAX)
        // POST: /Cart/UpdateQuantity
        // =========================
        [HttpPost]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<IActionResult> UpdateQuantity([FromBody] UpdateQuantityRequest request, CancellationToken ct)
        {
            if (!ModelState.IsValid) return JFail(400, "Số lượng không hợp lệ.");

            try
            {
                var product = await context.Products
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == request.ProductId, ct);

                if (product == null || !product.IsPublished)
                {
                    cartService.RemoveFromCart(request.ProductId);
                    return JFail(404, "Sản phẩm không còn bán.",
                        new { cartCount = cartService.GetTotalQuantity(), newTotalAmount = cartService.GetSubtotal() });
                }

                if (request.Quantity <= 0)
                {
                    cartService.RemoveFromCart(request.ProductId);
                    return JSuccess("Đã xóa sản phẩm khỏi giỏ.");
                }

                // Cập nhật số lượng (Clamp theo stock)
                var ok = cartService.SetQuantityWithClamp(
                    productId: request.ProductId,
                    requestedQty: request.Quantity,
                    maxStock: product.StockQuantity,
                    out var appliedQty
                );

                if (!ok) return JFail(404, "Không tìm thấy sản phẩm trong giỏ.");

                if (appliedQty != request.Quantity)
                {
                    return JFail(400, $"Chỉ còn {appliedQty} sản phẩm trong kho.",
                        new
                        {
                            appliedQty,
                            currentStock = product.StockQuantity,
                            cartCount = cartService.GetTotalQuantity(),
                            newTotalAmount = cartService.GetSubtotal()
                        });
                }

                return JSuccess("Cập nhật thành công.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Lỗi UpdateQuantity: {@req}", request);
                return JFail(500, "Lỗi hệ thống.");
            }
        }

        // =========================
        // API: XÓA SẢN PHẨM (AJAX)
        // POST: /Cart/RemoveItem
        // =========================
        [HttpPost]
        [Consumes("application/json")]
        [Produces("application/json")]
        public IActionResult RemoveItem([FromBody] RemoveItemRequest request)
        {
            if (!ModelState.IsValid) return JFail(400, "ID không hợp lệ.");
            cartService.RemoveFromCart(request.ProductId);
            return JSuccess("Đã xóa sản phẩm.");
        }

        // =========================
        // API: ĐẾM GIỎ HÀNG (Cho Badge)
        // GET: /Cart/Summary
        // =========================
        [HttpGet]
        [Produces("application/json")]
        public IActionResult Summary() => JSuccess();

        // =========================
        // API: XOÁ SẠCH GIỎ
        // POST: /Cart/Clear
        // =========================
        [HttpPost]
        [Produces("application/json")]
        public IActionResult Clear()
        {
            cartService.ClearCart();
            return Ok(new { success = true, message = "Giỏ hàng đã được làm trống.", cartCount = 0, newTotalAmount = 0m });
        }

        // =========================
        // API: THÊM NHIỀU (Bulk Add)
        // =========================
        [HttpPost]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<IActionResult> BulkAdd([FromBody] BulkAddRequest request, CancellationToken ct)
        {
            if (!ModelState.IsValid || request.Items == null || request.Items.Count == 0)
                return JFail(400, "Danh sách trống.");

            var ids = request.Items.Select(x => x.ProductId).Distinct().ToArray();
            var products = await context.Products
                .Where(p => ids.Contains(p.Id) && p.IsPublished)
                .AsNoTracking()
                .ToListAsync(ct);

            var map = products.ToDictionary(p => p.Id);
            int count = 0;

            foreach (var it in request.Items)
            {
                if (!map.TryGetValue(it.ProductId, out var p) || p.StockQuantity <= 0) continue;

                var inCart = cartService.GetItemQuantity(p.Id);
                var add = Math.Min(it.Quantity, p.StockQuantity - inCart);
                if (add > 0)
                {
                    cartService.Add(p, add);
                    count++;
                }
            }

            return JSuccess($"Đã thêm {count} dòng sản phẩm vào giỏ.");
        }

        // =========================
        // ACTION: XOÁ (Form Post - Legacy/Fallback)
        // =========================
        [HttpPost]
        public IActionResult Remove(int productId)
        {
            cartService.RemoveFromCart(productId);
            TempData[SD.Temp_Success] = "Đã xóa sản phẩm khỏi giỏ.";
            return RedirectToAction(nameof(Index));
        }
    }

    // =========================
    // Data Transfer Objects (DTOs)
    // =========================
    public class AddToCartRequest
    {
        [Required]
        public int ProductId { get; set; }

        [Range(1, 100)]
        public int Quantity { get; set; } = 1;
    }

    public class UpdateQuantityRequest
    {
        [Required]
        public int ProductId { get; set; }

        [Range(0, 1000)]
        public int Quantity { get; set; }
    }

    public class RemoveItemRequest
    {
        [Required]
        public int ProductId { get; set; }
    }

    public class BulkAddRequest
    {
        [Required]
        public List<BulkAddItem> Items { get; set; } = new();
    }

    public class BulkAddItem
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}