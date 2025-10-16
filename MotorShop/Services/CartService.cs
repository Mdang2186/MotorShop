using System.Text.Json;
using MotorShop.Models;

namespace MotorShop.Services
{
    public class CartService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private ISession Session => _httpContextAccessor.HttpContext.Session;

        public CartService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public List<OrderItem> GetCartItems()
        {
            var jsonCart = Session.GetString("Cart");
            return jsonCart == null ? new List<OrderItem>() : JsonSerializer.Deserialize<List<OrderItem>>(jsonCart);
        }

        public void SaveCartItems(List<OrderItem> cartItems)
        {
            var jsonCart = JsonSerializer.Serialize(cartItems);
            Session.SetString("Cart", jsonCart);
        }

        public void AddToCart(Product product, int quantity = 1)
        {
            var cartItems = GetCartItems();
            var existingItem = cartItems.FirstOrDefault(item => item.ProductId == product.Id);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                cartItems.Add(new OrderItem
                {
                    ProductId = product.Id,
                    Product = product, // Note: For display purposes, the actual Product object might not be fully saved in the session to keep it light.
                    Quantity = quantity,
                    UnitPrice = product.Price
                });
            }

            SaveCartItems(cartItems);
        }

        public void RemoveFromCart(int productId)
        {
            var cartItems = GetCartItems();
            var itemToRemove = cartItems.FirstOrDefault(item => item.ProductId == productId);
            if (itemToRemove != null)
            {
                cartItems.Remove(itemToRemove);
                SaveCartItems(cartItems);
            }
        }

        public void ClearCart()
        {
            Session.Remove("Cart");
        }
    }
}