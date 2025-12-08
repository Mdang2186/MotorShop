namespace MotorShop.ViewModels
{
    public class CheckoutLineVm
    {
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string? ImageUrl { get; set; }

        public decimal Subtotal => UnitPrice * Quantity;
    }
}
