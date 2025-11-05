// File: ViewModels/CheckoutViewModel.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using MotorShop.Models;            // <-- dùng Bank/Branch từ Models
using MotorShop.Models.Enums;

namespace MotorShop.ViewModels
{
    public class CheckoutViewModel : IValidatableObject
    {
        // ===== Giỏ hàng & tổng tiền =====
        public List<CartItem> Items { get; set; } = new();

        [DataType(DataType.Currency)] public decimal Subtotal { get; set; }
        [DataType(DataType.Currency)] public decimal ShippingFee { get; set; }
        [DataType(DataType.Currency)] public decimal DiscountAmount { get; set; }
        [DataType(DataType.Currency)] public decimal Total { get; set; }

        // Chỉ thanh toán một phần giỏ
        public int[]? SelectedProductIds { get; set; }

        // ===== Giao / Nhận =====
        [Display(Name = "Hình thức nhận hàng")]
        public DeliveryMethod DeliveryMethod { get; set; } = DeliveryMethod.HomeDelivery;

        [Display(Name = "Chi nhánh nhận")]
        public int? PickupBranchId { get; set; } // dùng khi PickupAtStore

        [Display(Name = "Địa chỉ giao hàng"), StringLength(255)]
        public string? ShippingAddress { get; set; } // dùng khi HomeDelivery

        // ===== Thông tin người nhận =====
        [Required, StringLength(100)]
        [Display(Name = "Người nhận")]
        public string? ReceiverName { get; set; }

        [Required, StringLength(20)]
        [Display(Name = "Số điện thoại")]
        public string? ReceiverPhone { get; set; }

        [Required, EmailAddress, StringLength(255)]
        [Display(Name = "Email")]
        public string? ReceiverEmail { get; set; }

        [StringLength(500)]
        [Display(Name = "Ghi chú cho đơn hàng")]
        public string? CustomerNote { get; set; }

        // ===== Thanh toán =====
        [Display(Name = "Phương thức thanh toán")]
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Card;

        [Display(Name = "Ngân hàng")]
        public string? SelectedBankCode { get; set; } // map với Bank.Code (vd: "tcb","vcb"...)

        [Display(Name = "Tên chủ thẻ")] public string? CardHolder { get; set; }
        [Display(Name = "Số thẻ")] public string? CardNumber { get; set; } // chỉ bind từ form
        [Display(Name = "Hạn thẻ (MM/YY)")] public string? CardExpiry { get; set; }

        // ===== Data cho View =====
        public List<Bank> Banks { get; set; } = new(); // <-- dùng Bank từ Models
        public List<Branch> Branches { get; set; } = new();

        // ===== Conditional validation =====
        public IEnumerable<ValidationResult> Validate(ValidationContext ctx)
        {
            var res = new List<ValidationResult>();

            if (DeliveryMethod == DeliveryMethod.HomeDelivery)
            {
                if (string.IsNullOrWhiteSpace(ShippingAddress))
                    res.Add(new ValidationResult("Vui lòng nhập địa chỉ giao hàng.", new[] { nameof(ShippingAddress) }));
            }
            else
            {
                if (PickupBranchId is null or <= 0)
                    res.Add(new ValidationResult("Vui lòng chọn chi nhánh nhận xe.", new[] { nameof(PickupBranchId) }));
            }

            if (PaymentMethod == PaymentMethod.Card)
            {
                if (string.IsNullOrWhiteSpace(SelectedBankCode))
                    res.Add(new ValidationResult("Vui lòng chọn ngân hàng.", new[] { nameof(SelectedBankCode) }));
                if (string.IsNullOrWhiteSpace(CardHolder))
                    res.Add(new ValidationResult("Vui lòng nhập tên chủ thẻ.", new[] { nameof(CardHolder) }));
                if (string.IsNullOrWhiteSpace(CardNumber))
                    res.Add(new ValidationResult("Vui lòng nhập số thẻ.", new[] { nameof(CardNumber) }));
                if (string.IsNullOrWhiteSpace(CardExpiry))
                    res.Add(new ValidationResult("Vui lòng nhập hạn thẻ (MM/YY).", new[] { nameof(CardExpiry) }));
            }

            if (Total != Subtotal + ShippingFee - DiscountAmount)
                res.Add(new ValidationResult("Tổng tiền không hợp lệ.", new[] { nameof(Total) }));

            return res;
        }
    }
}
