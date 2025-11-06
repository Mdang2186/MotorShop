// File: Services/CartService.cs
using System.Text.Json;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using MotorShop.Data;
using MotorShop.Models;
using MotorShop.Models.Enums; // DeliveryMethod, PaymentMethod
using MotorShop.Utilities;

namespace MotorShop.Services
{
    /// <summary>
    /// Quản lý giỏ hàng & phiên checkout lưu trong Session (không lưu DB).
    /// </summary>
    public class CartService
    {
        private readonly IHttpContextAccessor _http;
        private readonly ApplicationDbContext _db; // <-- để Add(productId, qty)

        public CartService(IHttpContextAccessor httpContextAccessor, ApplicationDbContext db)
        {
            _http = httpContextAccessor;
            _db = db;
        }

        private ISession? Session => _http.HttpContext?.Session;

        private const string FallbackCartKey = "MotorShopCart";
        private const string FallbackCheckoutKey = "MotorShopCheckout";

        private static string CartKey => SD.SessionCart ?? FallbackCartKey;
        private static string CheckoutKey => SD.SessionCheckout ?? FallbackCheckoutKey;

        private static readonly JsonSerializerOptions JsonOpt = new() { };

        // ====== Kiểu phiên checkout phù hợp CheckoutController/CheckoutViewModel ======
        public sealed class CheckoutSession
        {
            // Giao nhận
            public DeliveryMethod DeliveryMethod { get; set; } = DeliveryMethod.HomeDelivery;
            public int? PickupBranchId { get; set; }
            public string? ShippingAddress { get; set; }

            // Người nhận
            public string? ReceiverName { get; set; }
            public string? ReceiverPhone { get; set; }
            public string? ReceiverEmail { get; set; }

            // Thanh toán (mock)
            public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Card;
            public string? SelectedBankCode { get; set; } // ví dụ "tcb", "vcb"
            public string? CardHolder { get; set; }
            public string? CardExpiry { get; set; } // MM/YY
        }

        // ====== Session: Checkout ======
        public CheckoutSession? GetCheckoutSession()
        {
            if (Session is null) return null;
            var json = Session.GetString(CheckoutKey);
            if (string.IsNullOrEmpty(json)) return null;
            try { return JsonSerializer.Deserialize<CheckoutSession>(json, JsonOpt); }
            catch { Session.Remove(CheckoutKey); return null; }
        }

        public void SaveCheckoutSession(CheckoutSession state)
        {
            if (Session is null) return;
            Session.SetString(CheckoutKey, JsonSerializer.Serialize(state, JsonOpt));
        }

        public void ClearCheckoutSession() => Session?.Remove(CheckoutKey);

        // ====== Session: Cart ======
        public List<CartItem> GetCartItems()
        {
            if (Session == null) return new();
            var json = Session.GetString(CartKey);
            if (string.IsNullOrEmpty(json)) return new();
            try { return JsonSerializer.Deserialize<List<CartItem>>(json, JsonOpt) ?? new(); }
            catch { Session.Remove(CartKey); return new(); }
        }

        private void SaveCartItems(List<CartItem> items)
        {
            if (Session == null) return;
            if (items == null || items.Count == 0) { Session.Remove(CartKey); return; }
            Session.SetString(CartKey, JsonSerializer.Serialize(items, JsonOpt));
        }

        // ====== API giỏ hàng ======

        /// <summary>Thêm sản phẩm theo Id (load Product từ DB). Bỏ qua nếu không tìm thấy hoặc không bán.</summary>
        public void Add(int productId, int quantity = 1)
        {
            if (quantity <= 0 || Session == null) return;

            // Chỉ lấy sản phẩm đang publish
            var p = _db.Products
                       .AsNoTracking()
                       .FirstOrDefault(x => x.Id == productId && x.IsPublished);
            if (p == null) return;

            Add(p, quantity);
        }

        /// <summary>Thêm sản phẩm với dữ liệu hiện có (giữ tương thích ngược với code cũ).</summary>
        public void Add(Product product, int quantity = 1) => AddToCart(product, quantity);

        /// <summary>Giữ lại hàm cũ để tương thích ngược.</summary>
        public void AddToCart(Product product, int quantity = 1)
        {
            if (Session == null || product == null || quantity <= 0) return;

            var items = GetCartItems();
            var existing = items.FirstOrDefault(i => i.ProductId == product.Id);

            if (existing != null)
            {
                try { checked { existing.Quantity = Math.Min(existing.Quantity + quantity, 9999); } }
                catch { existing.Quantity = 9999; }
            }
            else
            {
                items.Add(new CartItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Quantity = Math.Min(quantity, 9999),
                    Price = Math.Max(0, product.Price),
                    ImageUrl = product.ImageUrl // dùng ảnh chính
                });
            }

            SaveCartItems(items);
        }

        public void RemoveFromCart(int productId)
        {
            if (Session == null) return;
            var items = GetCartItems();
            items.RemoveAll(i => i.ProductId == productId);
            SaveCartItems(items);
        }

        /// <summary>Đặt số lượng tuyệt đối (không kiểm kho). &lt;=0 sẽ xoá item.</summary>
        public bool UpdateQuantity(int productId, int quantity)
        {
            if (Session == null) return false;

            var items = GetCartItems();
            var item = items.FirstOrDefault(i => i.ProductId == productId);
            if (item == null) return false;

            if (quantity <= 0) items.Remove(item);
            else item.Quantity = Math.Min(quantity, 9999);

            SaveCartItems(items);
            return true;
        }

        /// <summary>Set số lượng có kẹp bởi tồn kho (maxStock). requestedQty &lt;=0 xoá.</summary>
        public bool SetQuantityWithClamp(int productId, int requestedQty, int maxStock, out int appliedQty)
        {
            appliedQty = 0;
            if (Session == null) return false;

            var items = GetCartItems();
            var item = items.FirstOrDefault(i => i.ProductId == productId);
            if (item == null) return false;

            if (requestedQty <= 0 || maxStock <= 0)
            {
                items.Remove(item);
                SaveCartItems(items);
                return true;
            }

            var clamped = Math.Min(Math.Max(1, requestedQty), Math.Max(1, maxStock));
            item.Quantity = clamped;
            appliedQty = clamped;
            SaveCartItems(items);
            return true;
        }

        public int GetItemQuantity(int productId)
            => GetCartItems().FirstOrDefault(i => i.ProductId == productId)?.Quantity ?? 0;

        public void ClearCart() => Session?.Remove(CartKey);

        public void RemovePurchasedItems(IEnumerable<int> purchasedProductIds)
        {
            if (Session == null) return;
            var ids = purchasedProductIds?.ToArray() ?? Array.Empty<int>();
            if (ids.Length == 0) return;

            var items = GetCartItems();
            items.RemoveAll(i => ids.Contains(i.ProductId));
            SaveCartItems(items);
        }

        // ====== Helpers ======
        public decimal GetSubtotal() => GetCartItems().Sum(it => it.Price * it.Quantity);

        public int GetTotalQuantity() => GetCartItems().Sum(i => i.Quantity);

        /// <summary>
        /// Làm tươi giá/ảnh từ Product hiện tại (nếu cần), tránh dùng giá cũ trong session.
        /// </summary>
        public void RefreshPricesFrom(IEnumerable<Product> freshProducts)
        {
            if (Session == null) return;
            var map = (freshProducts ?? Enumerable.Empty<Product>()).ToDictionary(p => p.Id, p => p);
            var items = GetCartItems();
            var changed = false;

            foreach (var it in items)
            {
                if (!map.TryGetValue(it.ProductId, out var p)) continue;

                var newPrice = Math.Max(0, p.Price);
                if (it.Price != newPrice) { it.Price = newPrice; changed = true; }

                var newImg = p.ImageUrl;
                if (!string.IsNullOrWhiteSpace(newImg) &&
                    !string.Equals(it.ImageUrl, newImg, StringComparison.OrdinalIgnoreCase))
                {
                    it.ImageUrl = newImg; changed = true;
                }
            }

            if (changed) SaveCartItems(items);
        }
    }
}
