using System;
using System.Collections.Generic;
using MotorShop.Models.Enums;

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

        // Đặt cọc
        public bool RequiresDeposit { get; set; }
        public decimal DepositAmount { get; set; }
        public decimal RemainingAmount { get; set; }

        public PaymentMethod PaymentMethod { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public string PaymentMethodLabel { get; set; } = "";

        // QR
        /// <summary>
        /// Chuỗi dữ liệu để tạo QR (sẽ render bằng JS QRCode).
        /// </summary>
        public string? QrPayload { get; set; }

        /// <summary>
        /// Mô tả ngắn về nội dung QR (hiển thị dưới hình).
        /// </summary>
        public string? QrDescription { get; set; }

        public List<CheckoutLineVm> Items { get; set; } = new();
    }
}
