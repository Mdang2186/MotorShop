using System.Text.Json;
using MotorShop.Models; // Namespace chứa CartItem và Product

namespace MotorShop.Services;

public class CartService(IHttpContextAccessor httpContextAccessor)
{
    private const string CartSessionKey = "MotorShopCart";
    private ISession Session => httpContextAccessor.HttpContext!.Session;

    /// <summary>
    /// Lấy danh sách CartItem từ Session.
    /// </summary>
    public List<CartItem> GetCartItems()
    {
        var jsonCart = Session.GetString(CartSessionKey);
        try
        {
            return string.IsNullOrEmpty(jsonCart) ? [] : JsonSerializer.Deserialize<List<CartItem>>(jsonCart)!;
        }
        catch (JsonException) // Xử lý nếu dữ liệu Session bị hỏng
        {
            Session.Remove(CartSessionKey); // Xóa dữ liệu hỏng
            return [];
        }
    }

    /// <summary>
    /// Lưu danh sách CartItem vào Session.
    /// </summary>
    private void SaveCartItems(List<CartItem> cartItems)
    {
        if (cartItems == null || cartItems.Count == 0)
        {
            Session.Remove(CartSessionKey);
        }
        else
        {
            var jsonCart = JsonSerializer.Serialize(cartItems);
            Session.SetString(CartSessionKey, jsonCart);
        }
    }

    /// <summary>
    /// Thêm một sản phẩm vào giỏ hàng hoặc tăng số lượng.
    /// </summary>
    public void AddToCart(Product product, int quantity = 1)
    {
        if (product == null || quantity <= 0) return;

        var cartItems = GetCartItems();
        var existingItem = cartItems.FirstOrDefault(item => item.ProductId == product.Id);

        if (existingItem != null)
        {
            existingItem.Quantity += quantity;
        }
        else
        {
            // Tạo CartItem mới
            cartItems.Add(new CartItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Quantity = quantity,
                Price = product.Price, // Lấy giá bán hiện tại
                ImageUrl = product.ImageUrl
            });
        }
        SaveCartItems(cartItems);
    }

    /// <summary>
    /// Xóa một sản phẩm khỏi giỏ hàng.
    /// </summary>
    public void RemoveFromCart(int productId)
    {
        var cartItems = GetCartItems();
        cartItems.RemoveAll(item => item.ProductId == productId); // Xóa tất cả item có productId này
        SaveCartItems(cartItems);
    }

    /// <summary>
    /// Cập nhật số lượng cho một sản phẩm trong giỏ.
    /// </summary>
    public bool UpdateQuantity(int productId, int quantity)
    {
        if (quantity <= 0)
        {
            RemoveFromCart(productId);
            return true;
        }

        var cartItems = GetCartItems();
        var itemToUpdate = cartItems.FirstOrDefault(item => item.ProductId == productId);

        if (itemToUpdate != null)
        {
            itemToUpdate.Quantity = quantity;
            SaveCartItems(cartItems);
            return true;
        }
        return false; // Không tìm thấy sản phẩm để cập nhật
    }

    /// <summary>
    /// Lấy số lượng hiện tại của một sản phẩm trong giỏ.
    /// </summary>
    public int GetItemQuantity(int productId)
    {
        var cartItems = GetCartItems();
        return cartItems.FirstOrDefault(item => item.ProductId == productId)?.Quantity ?? 0;
    }

    /// <summary>
    /// Xóa toàn bộ giỏ hàng.
    /// </summary>
    public void ClearCart()
    {
        Session.Remove(CartSessionKey);
    }

    /// <summary>
    /// Xóa các sản phẩm đã được mua khỏi giỏ hàng.
    /// </summary>
    public void RemovePurchasedItems(IEnumerable<int> purchasedProductIds)
    {
        if (purchasedProductIds == null || !purchasedProductIds.Any()) return;
        var cartItems = GetCartItems();
        cartItems.RemoveAll(item => purchasedProductIds.Contains(item.ProductId));
        SaveCartItems(cartItems);
    }
}