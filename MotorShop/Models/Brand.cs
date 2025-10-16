namespace MotorShop.Models
{
    // Thương hiệu xe
    public class Brand
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}