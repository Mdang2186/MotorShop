using Microsoft.AspNetCore.Mvc.Rendering;
using MotorShop.Models;

namespace MotorShop.ViewModels
{
    public class ProductIndexViewModel
    {
        public List<Product> Products { get; set; } = [];

        // Fix: Initialize to prevent null reference warnings
        public SelectList Brands { get; set; } = new SelectList(new List<Brand>(), "Id", "Name");
        public SelectList Categories { get; set; } = new SelectList(new List<Category>(), "Id", "Name");

        public string? SearchString { get; set; }
        public int? BrandFilter { get; set; }
        public int? CategoryFilter { get; set; }
        public string? SortBy { get; set; }

        public int PageIndex { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;
    }
}