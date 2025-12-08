// ViewModels/OrderSuccessViewModel.cs
using MotorShop.Models.Enums;
using MotorShop.ViewModels;
namespace MotorShop.ViewModels
{
    public class OrderSuccessViewModel
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }

        public string? ReceiverName { get; set; }
        public string? ReceiverPhone { get; set; }
        public string? ReceiverEmail { get; set; }

        public DeliveryMethod DeliveryMethod { get; set; }
        public string? ShippingAddress { get; set; }
        public string? BranchName { get; set; }

        public decimal Subtotal { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal Total { get; set; }

        public bool RequiresDeposit { get; set; }
        public decimal DepositAmount { get; set; }
        public decimal RemainingAmount { get; set; }

        public PaymentMethod PaymentMethod { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public string PaymentMethodLabel { get; set; } = "";

        // QR THANH TOÁN (đang dùng sẵn)
        public string? QrPayload { get; set; }          // VietQR (payment)
        public string? QrDescription { get; set; }

        // ========= MỚI: QR ĐƠN HÀNG =========
        /// <summary>
        /// Payload / URL cho mã QR đơn hàng (dùng để tra cứu nhanh).
        /// </summary>
        public string? OrderQrPayload { get; set; }

        /// <summary>
        /// Mô tả ngắn cho QR đơn hàng (hiển thị dưới mã QR).
        /// </summary>
        public string? OrderQrDescription { get; set; }

        public List<CheckoutLineVm> Items { get; set; } = new();
    }
}