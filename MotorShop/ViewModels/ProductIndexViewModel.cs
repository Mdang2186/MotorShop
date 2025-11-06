using Microsoft.AspNetCore.Mvc.Rendering;
using MotorShop.Models;
using MotorShop.Utilities;

namespace MotorShop.ViewModels
{
    public class ProductIndexViewModel
    {
        public List<Product> Products { get; set; } = new();

        public SelectList Brands { get; set; } = new(new List<Brand>(), "Id", "Name");
        public SelectList Categories { get; set; } = new(new List<Category>(), "Id", "Name");

        public string? SearchString { get; set; }
        public int? BrandFilter { get; set; }
        public int? CategoryFilter { get; set; }
        public string? SortBy { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }

        public int TotalProductCount { get; set; }

        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = SD.DefaultPageSize;
        public int TotalPages { get; set; }

        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;
    }
}
