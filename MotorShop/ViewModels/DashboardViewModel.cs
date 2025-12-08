// ViewModels/DashboardViewModel.cs
using MotorShop.Models;
using System;
using System.Collections.Generic;

namespace MotorShop.ViewModels
{
    public class DashboardViewModel
    {
        // ===== Bộ lọc thời gian =====
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }

        // ===== KPI tổng quan =====
        public int TotalOrdersInRange { get; set; }
        public decimal TotalRevenueInRange { get; set; }
        public int SuccessfulOrdersInRange { get; set; }
        public int CancelledOrdersInRange { get; set; }
        public int NewCustomersInRange { get; set; }

        // ===== Biểu đồ doanh thu theo ngày (Line) =====
        public List<string> RevenueChartLabels { get; set; } = new();
        public List<decimal> RevenueChartData { get; set; } = new();

        // ===== Biểu đồ số đơn theo ngày (Bar) =====
        public List<string> OrderCountChartLabels { get; set; } = new();
        public List<int> OrderCountChartData { get; set; } = new();

        // ===== Biểu đồ khách hàng mới theo ngày (Bar) =====
        public List<string> NewCustomerChartLabels { get; set; } = new();
        public List<int> NewCustomerChartData { get; set; } = new();

        // ===== Biểu đồ trạng thái đơn hàng (Doughnut) =====
        public List<string> OrderStatusLabels { get; set; } = new();
        public List<int> OrderStatusCounts { get; set; } = new();

        // ===== Biểu đồ doanh thu theo danh mục (Bar) =====
        public List<string> RevenueByCategoryLabels { get; set; } = new();
        public List<decimal> RevenueByCategoryData { get; set; } = new();

        // ===== Top sản phẩm bán chạy =====
        public List<TopProductDto> TopProducts { get; set; } = new();

        // ===== Đơn hàng gần đây =====
        public List<Order> RecentOrders { get; set; } = new();

        // ===== Khách hàng VIP (top chi tiêu) =====
        public List<CustomerSummaryDto> VipCustomers { get; set; } = new();

        // ===== Khách hàng đăng ký gần đây =====
        public List<CustomerSummaryDto> NewCustomers { get; set; } = new();
    }

    public class TopProductDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public string? ImageUrl { get; set; }
        public int Quantity { get; set; }
        public decimal Revenue { get; set; }
    }

    public class CustomerSummaryDto
    {
        public string UserId { get; set; } = "";
        public string Email { get; set; } = "";
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public DateTime CreatedAt { get; set; }

        public int TotalOrders { get; set; }
        public decimal TotalSpent { get; set; }
    }
}
