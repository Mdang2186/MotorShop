using MotorShop.Models;

namespace MotorShop.ViewModels
{
    public class DashboardViewModel
    {
        // 1. Thống kê nhanh
        public decimal MonthlyRevenue { get; set; }
        public decimal TodayRevenue { get; set; }
        public int NewOrdersToday { get; set; }
        public int PendingOrders { get; set; }
        public int NewCustomersToday { get; set; }
        public int TotalProducts { get; set; }

        // 2. Biểu đồ doanh thu (Line)
        public List<string> RevenueChartLabels { get; set; } = new();
        public List<decimal> RevenueChartData { get; set; } = new();

        // 3. Biểu đồ trạng thái đơn (Doughnut)
        public List<string> OrderStatusLabels { get; set; } = new();
        public List<int> OrderStatusCounts { get; set; } = new();

        // 4. Danh sách
        public List<Order> RecentOrders { get; set; } = new();
    }
}