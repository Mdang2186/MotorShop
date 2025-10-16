using Microsoft.AspNetCore.Mvc.Rendering;
using MotorShop.Models;

namespace MotorShop.ViewModels
{
    public class ProductIndexViewModel
    {
        public List<Product> Products { get; set; } = new List<Product>();

        // Dữ liệu cho bộ lọc
        public SelectList Brands { get; set; }
        public SelectList Categories { get; set; }

        // Các giá trị bộ lọc người dùng đã chọn
        public string? SearchString { get; set; }
        public int? BrandFilter { get; set; }
        public int? CategoryFilter { get; set; }
        public string? SortBy { get; set; } // << ✨ THÊM DÒNG NÀY ✨

        // Phân trang
        public int PageIndex { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;
    }
}