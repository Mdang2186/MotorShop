using MotorShop.Models;

namespace MotorShop.ViewModels
{
    public class PartsListViewModel
    {
        public IEnumerable<Product> Items { get; set; } = Enumerable.Empty<Product>();
        public string? CurrentBrand { get; set; }
        public string? Search { get; set; }

        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }

        public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
    }

}
