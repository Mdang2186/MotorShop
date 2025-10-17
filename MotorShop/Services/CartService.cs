using Microsoft.AspNetCore.Http;
using MotorShop.Data;
using MotorShop.Models;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace MotorShop.Services
{
    /// <summary>
    /// Dịch vụ quản lý logic giỏ hàng, lưu trữ dữ liệu trong Session.
    /// Sử dụng một model 'CartItem' riêng biệt để tối ưu và bảo mật.
    /// </summary>
    public class CartService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private ISession Session => _httpContextAccessor.HttpContext!.Session;
        private const string CartSessionKey = "MotorShopCart";

        public CartService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        // === CÁC PHƯƠNG THỨC CỐT LÕI ===

        /// <summary>
        /// Lấy danh sách các sản phẩm trong giỏ hàng từ Session.
        /// </summary>
        public List<CartItem> GetCartItems()
        {
            var jsonCart = Session.GetString(CartSessionKey);
            return jsonCart == null ? new List<CartItem>() : JsonConvert.DeserializeObject<List<CartItem>>(jsonCart);
        }

        /// <summary>
        /// Thêm một sản phẩm vào giỏ hàng hoặc cập nhật số lượng nếu đã tồn tại.
        /// </summary>
        public void AddToCart(Product product, int quantity = 1)
        {
            var cart = GetCartItems();
            var cartItem = cart.FirstOrDefault(i => i.ProductId == product.Id);

            if (cartItem != null)
            {
                // Nếu đã có, chỉ tăng số lượng
                cartItem.Quantity += quantity;
            }
            else
            {
                // Nếu chưa có, tạo một CartItem mới với các thông tin cần thiết
                cart.Add(new CartItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Price = product.Price, // "Chụp lại" giá tại thời điểm thêm vào giỏ
                    Quantity = quantity,
                    ImageUrl = product.ImageUrl ?? ""
                });
            }
            SaveCartItems(cart);
        }

        /// <summary>
        /// Cập nhật số lượng cho một sản phẩm trong giỏ.
        /// </summary>
        public void UpdateQuantity(int productId, int quantity)
        {
            var cart = GetCartItems();
            var cartItem = cart.FirstOrDefault(i => i.ProductId == productId);
            if (cartItem != null)
            {
                if (quantity > 0)
                {
                    cartItem.Quantity = quantity;
                }
                else
                {
                    // Nếu số lượng <= 0, coi như xóa sản phẩm
                    cart.Remove(cartItem);
                }
                SaveCartItems(cart);
            }
        }

        /// <summary>
        /// Xóa một sản phẩm khỏi giỏ hàng.
        /// </summary>
        public void RemoveFromCart(int productId)
        {
            var cart = GetCartItems();
            var itemToRemove = cart.FirstOrDefault(i => i.ProductId == productId);
            if (itemToRemove != null)
            {
                cart.Remove(itemToRemove);
                SaveCartItems(cart);
            }
        }

        /// <summary>
        /// Xóa sạch tất cả sản phẩm khỏi giỏ hàng.
        /// </summary>
        public void ClearCart()
        {
            Session.Remove(CartSessionKey);
        }

        // === CÁC PHƯƠNG THỨC TIỆN ÍCH ===

        /// <summary>
        /// Lấy số lượng của một sản phẩm cụ thể đang có trong giỏ.
        /// </summary>
        public int GetItemQuantity(int productId)
        {
            var cart = GetCartItems();
            var cartItem = cart.FirstOrDefault(i => i.ProductId == productId);
            return cartItem?.Quantity ?? 0;
        }

        /// <summary>
        /// Tính tổng giá trị của giỏ hàng.
        /// </summary>
        public decimal GetTotalAmount()
        {
            return GetCartItems().Sum(item => item.Subtotal);
        }

        // === PHƯƠNG THỨC RIÊNG TƯ ===

        /// <summary>
        /// Lưu danh sách giỏ hàng vào Session dưới dạng chuỗi JSON.
        /// </summary>
        private void SaveCartItems(List<CartItem> cartItems)
        {
            var jsonCart = JsonConvert.SerializeObject(cartItems);
            Session.SetString(CartSessionKey, jsonCart);
        }
    }
}