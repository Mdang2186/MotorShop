using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MotorShop.Models.Enums;

namespace MotorShop.Models
{
    public class Order
    {
        public int Id { get; set; }

        // Chủ đơn
        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        [DataType(DataType.Currency)]
        public decimal TotalAmount { get; set; }

        // Giao/nhận
        public DeliveryMethod DeliveryMethod { get; set; } = DeliveryMethod.HomeDelivery;

        public int? PickupBranchId { get; set; }
        public Branch? PickupBranch { get; set; }

        [StringLength(255)]
        public string? ShippingAddress { get; set; }

        [StringLength(100)]
        public string? ReceiverName { get; set; }

        [StringLength(20)]
        public string? ReceiverPhone { get; set; }

        [StringLength(255)]
        public string? ReceiverEmail { get; set; }

        public decimal ShippingFee { get; set; } = 0;
        public decimal DiscountAmount { get; set; } = 0;

        [StringLength(500)]
        public string? CustomerNote { get; set; }

        // Thanh toán
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Card;
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

        [StringLength(20)]
        public string? CardLast4 { get; set; }

        [StringLength(100)]
        public string? PaymentRef { get; set; }
        // ...
        public int? ShipperId { get; set; }
        public Shipper? Shipper { get; set; }

        [StringLength(100)]
        public string? TrackingCode { get; set; }

        // Ngân hàng khách đã chọn khi chuyển khoản / đặt cọc
        [StringLength(50)]
        public string? SelectedBankCode { get; set; }
        // ====== THÊM MỚI ======
        [Range(0, double.MaxValue)]
        public decimal? DepositAmount { get; set; }

        [StringLength(255)]
        public string? DepositNote { get; set; }
        public List<OrderItem> OrderItems { get; set; } = new();
    }
}
