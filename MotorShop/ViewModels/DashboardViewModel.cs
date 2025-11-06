using System.Collections.Generic;
using MotorShop.Models;

namespace MotorShop.ViewModels
{
    public class DashboardViewModel
    {
        // KPI
        public decimal MonthlyRevenue { get; set; }
        public decimal TodayRevenue { get; set; }
        public int NewOrdersToday { get; set; }
        public int PendingOrders { get; set; }
        public int NewCustomersToday { get; set; }
        public int TotalProducts { get; set; }

        // Charts
        public List<string> RevenueChartLabels { get; set; } = new();
        public List<decimal> RevenueChartData { get; set; } = new();
        public List<string> OrderStatusLabels { get; set; } = new();
        public List<int> OrderStatusCounts { get; set; } = new();

        // Recent
        public List<Order> RecentOrders { get; set; } = new();
        public List<ApplicationUser> RecentCustomers { get; set; } = new();
    }
}
