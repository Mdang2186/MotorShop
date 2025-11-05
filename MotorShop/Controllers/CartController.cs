// File: Controllers/CartController.cs
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MotorShop.Data;
using MotorShop.Models;
using MotorShop.Services;
using MotorShop.Utilities;
using MotorShop.ViewModels.Cart;

namespace MotorShop.Controllers;

[AutoValidateAntiforgeryToken]
public class CartController(
    CartService cartService,
    ApplicationDbContext context,
    ILogger<CartController> logger) : Controller
{
    // =========================
    // Helpers (JSON shape)
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

    // Map snapshot cart -> VM (đã kẹp theo DB, không động tới session lần 2)
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
                p.ImageUrl
            })
            .ToListAsync(ct);

        var map = products.ToDictionary(p => p.Id);
        var list = new List<CartItemVm>(snapshot.Count);

        foreach (var it in snapshot)
        {
            if (!map.TryGetValue(it.ProductId, out var p)) continue; // đã lọc ở bước đồng bộ
            list.Add(new CartItemVm
            {
                ProductId = it.ProductId,
                Name = p.Name ?? "Sản phẩm",
                ImageUrl = string.IsNullOrWhiteSpace(p.ImageUrl) ? "/images/products/placeholder.png" : p.ImageUrl,
                UnitPrice = p.Price,
                Quantity = it.Quantity
            });
        }
        return list;
    }

    // =========================
    // GET: /Cart
    // =========================
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        // 1) Refresh giá/ảnh trong session theo DB hiện tại
        var snap = cartService.GetCartItems();
        if (snap.Count > 0)
        {
            var ids = snap.Select(i => i.ProductId).Distinct().ToArray();
            var fresh = await context.Products
                                     .Where(p => ids.Contains(p.Id))
                                     .AsNoTracking()
                                     .ToListAsync(ct);

            cartService.RefreshPricesFrom(fresh);

            // 2) Chỉ loại những sản phẩm thực sự không còn bán
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
                TempData[SD.Temp_Info] = $"Đã loại {removed} sản phẩm không còn bán khỏi giỏ.";
        }

        // 3) Map sang VM để view dùng ổn định
        var vmItems = await BuildVmAsync(ct);
        return View(vmItems);
    }

    // =========================
    // == JSON APIs ==
    // =========================

    // GET: /Cart/Summary (đếm nhanh cho badge)
    [HttpGet]
    [Produces("application/json")]
    public IActionResult Summary() => JSuccess();

    // POST: /Cart/Clear (xoá sạch giỏ)
    [HttpPost]
    [Produces("application/json")]
    public IActionResult Clear()
    {
        cartService.ClearCart();
        return Ok(new { success = true, message = "Đã xoá giỏ hàng.", cartCount = 0, newTotalAmount = 0m });
    }

    // POST: /Cart/Refresh (đồng bộ giá/ảnh + loại hàng đã gỡ bán)
    [HttpPost]
    [Produces("application/json")]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        var items = cartService.GetCartItems();
        if (items.Count > 0)
        {
            var ids = items.Select(i => i.ProductId).Distinct().ToArray();
            var fresh = await context.Products
                                     .Where(p => ids.Contains(p.Id))
                                     .AsNoTracking()
                                     .ToListAsync(ct);

            cartService.RefreshPricesFrom(fresh);

            var validIds = fresh.Where(p => p.IsPublished).Select(p => p.Id).ToHashSet();
            foreach (var it in cartService.GetCartItems().ToList())
                if (!validIds.Contains(it.ProductId)) cartService.RemoveFromCart(it.ProductId);
        }
        return JSuccess("Đã đồng bộ giỏ hàng.");
    }

    // POST: /Cart/AddToCart  (AJAX JSON)
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
                return JFail(404, "Sản phẩm không tồn tại hoặc ngừng bán.");

            if (product.StockQuantity <= 0)
                return JFail(400, "Sản phẩm tạm thời hết hàng.");

            // Kẹp theo tồn kho hiện có (bao gồm lượng đã có trong giỏ)
            var quantityInCart = cartService.GetItemQuantity(request.ProductId);
            var canAdd = Math.Max(0, product.StockQuantity - quantityInCart);
            var addQty = Math.Min(Math.Max(1, request.Quantity), canAdd);

            if (addQty <= 0)
            {
                return JFail(400, $"Bạn đã đạt tối đa theo tồn kho hiện có ({product.StockQuantity}).",
                    new { currentStock = product.StockQuantity, allowedToAdd = 0 });
            }

            cartService.Add(product, addQty);
            return JSuccess($"Đã thêm '{product.Name}' ({addQty}).");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi khi thêm vào giỏ hàng: {@req}", request);
            return JFail(500, "Lỗi máy chủ khi thêm vào giỏ.");
        }
    }

    // POST: /Cart/BulkAdd (mua lại nhiều sản phẩm)
    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<IActionResult> BulkAdd([FromBody] BulkAddRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid || request.Items is null || request.Items.Count == 0)
            return JFail(400, "Danh sách sản phẩm không hợp lệ.");

        if (request.Items.Count > 50)
            return JFail(400, "Không thể thêm quá 50 dòng một lần.");

        var ids = request.Items.Select(x => x.ProductId).Distinct().ToArray();
        var products = await context.Products
            .Where(p => ids.Contains(p.Id) && p.IsPublished)
            .AsNoTracking()
            .ToListAsync(ct);

        var map = products.ToDictionary(p => p.Id);
        int addedLines = 0, addedUnits = 0;
        var errors = new List<object>();

        foreach (var it in request.Items)
        {
            if (!map.TryGetValue(it.ProductId, out var p))
            {
                errors.Add(new { productId = it.ProductId, reason = "Không còn bán." });
                continue;
            }
            if (p.StockQuantity <= 0)
            {
                errors.Add(new { productId = it.ProductId, name = p.Name, reason = "Hết hàng." });
                continue;
            }

            var inCart = cartService.GetItemQuantity(p.Id);
            var maxAddable = Math.Max(0, p.StockQuantity - inCart);
            var qty = Math.Max(1, it.Quantity);
            var applied = Math.Min(qty, maxAddable);

            if (applied <= 0)
            {
                errors.Add(new { productId = it.ProductId, name = p.Name, reason = "Đã đủ số lượng theo tồn kho." });
                continue;
            }

            cartService.Add(p, applied);
            addedLines++;
            addedUnits += applied;
        }

        var okMsg = $"Đã thêm {addedLines} dòng / {addedUnits} sản phẩm vào giỏ.";
        var res = (OkObjectResult)JSuccess(okMsg);
        // ghép thêm errors nếu có
        var dict = (res.Value as IDictionary<string, object?>)!;
        dict["errors"] = errors;
        return res;
    }

    // POST: /Cart/UpdateQuantity
    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<IActionResult> UpdateQuantity([FromBody] UpdateQuantityRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return JFail(400, "Số lượng không hợp lệ.");

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

            var ok = cartService.SetQuantityWithClamp(
                productId: request.ProductId,
                requestedQty: request.Quantity,
                maxStock: product.StockQuantity,
                out var appliedQty
            );

            if (!ok) return JFail(404, "Không tìm thấy sản phẩm trong giỏ.");

            if (appliedQty != request.Quantity)
            {
                return JFail(400, $"Sản phẩm chỉ còn {appliedQty} chiếc theo tồn kho.",
                    new
                    {
                        appliedQty,
                        currentStock = product.StockQuantity,
                        cartCount = cartService.GetTotalQuantity(),
                        newTotalAmount = cartService.GetSubtotal()
                    });
            }

            return JSuccess("Cập nhật số lượng thành công!");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi khi cập nhật số lượng: {@req}", request);
            return JFail(500, "Lỗi máy chủ khi cập nhật số lượng.");
        }
    }

    // POST: /Cart/RemoveItem (AJAX)
    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json")]
    public IActionResult RemoveItem([FromBody] RemoveItemRequest request)
    {
        if (!ModelState.IsValid)
            return JFail(400, "ID sản phẩm không hợp lệ.");

        try
        {
            cartService.RemoveFromCart(request.ProductId);
            return JSuccess("Đã xóa sản phẩm khỏi giỏ!");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi khi xóa sản phẩm khỏi giỏ: {@req}", request);
            return JFail(500, "Lỗi máy chủ khi xóa sản phẩm.");
        }
    }

    // (Tuỳ chọn) Xoá bằng form truyền thống
    [HttpPost]
    public IActionResult Remove(int productId)
    {
        cartService.RemoveFromCart(productId);
        TempData[SD.Temp_Success] = "Đã xóa sản phẩm khỏi giỏ hàng.";
        return RedirectToAction(nameof(Index));
    }
}

// ===== DTOs =====
public class AddToCartRequest
{
    [Required]
    public int ProductId { get; set; }

    [Range(1, 100, ErrorMessage = "Số lượng phải từ 1 đến 100.")]
    public int Quantity { get; set; } = 1;
}

public class BulkAddRequest
{
    [Required]
    public List<BulkAddItem> Items { get; set; } = new();
}

public class BulkAddItem
{
    [Required]
    public int ProductId { get; set; }

    [Range(1, 1000, ErrorMessage = "Số lượng phải từ 1 đến 1000.")]
    public int Quantity { get; set; } = 1;
}

public class UpdateQuantityRequest
{
    [Required]
    public int ProductId { get; set; }

    [Range(0, 1000, ErrorMessage = "Số lượng phải từ 0 đến 1000.")]
    public int Quantity { get; set; }
}

public class RemoveItemRequest
{
    [Required]
    public int ProductId { get; set; }
}
