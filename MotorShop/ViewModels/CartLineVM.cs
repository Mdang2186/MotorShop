// ViewModels/Cart/CartIndexViewModel.cs
using System.Collections.Generic;
using System.Linq;

namespace MotorShop.ViewModels
{
    public class CartLineVM
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = "";
        public string ImageUrl { get; set; } = "";
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal LineTotal => UnitPrice * Quantity;
    }

    public class CartIndexViewModel
    {
        public List<CartLineVM> Items { get; set; } = new();

        // Đổi để trùng với Checkout
        public int[]? SelectedProductIds { get; set; }

        public decimal Subtotal => Items.Sum(i => i.LineTotal);
    }
}
