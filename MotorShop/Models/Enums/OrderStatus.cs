namespace MotorShop.Models.Enums
{
    public enum OrderStatus
    {
        Pending,      // Chờ xử lý
        Confirmed,    // Đã xác nhận
        Shipping,     // Đang giao hàng
        Delivered,    // Đã giao thành công
        Cancelled     // Đã hủy
    }
}