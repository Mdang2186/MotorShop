namespace MotorShop.Models.Enums
{
    // Nhận hàng: giao tận nơi hoặc lấy tại cửa hàng
    public enum DeliveryMethod
    {
        HomeDelivery = 0,
        PickupAtStore = 1
    }

    // Phương thức thanh toán
    public enum PaymentMethod
    {
        Card = 0, // thẻ ngân hàng (nhập số thẻ)
        BankTransfer = 1, // chuyển khoản
        CashOnDelivery = 2, // thanh toán khi nhận hàng (COD)
        PayAtStore = 3,  // thanh toán tại cửa hàng
        COD = 4
    }

    // Trạng thái thanh toán
    public enum PaymentStatus
    {
        Pending = 0,
        Paid = 1,
        Failed = 2,
        Refunded = 3
    }

    // Trạng thái đơn hàng
    public enum OrderStatus
    {
        Pending = 0, // chờ tiếp nhận
        Processing = 1, // đang xử lý (đã nhận đơn)  ← khớp CheckoutController
        Confirmed = 2,
        Shipping = 3,
        Delivered = 4,
        Cancelled = 5,
        Completed = 6
    }
}
