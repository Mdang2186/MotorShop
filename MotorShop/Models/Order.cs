﻿using MotorShop.Models.Enums; // <-- THÊM DÒNG NÀY
using System.ComponentModel.DataAnnotations.Schema;

namespace MotorShop.Models
{
    // Đơn hàng
    public class Order
    {
        public int Id { get; set; }
        public DateTime OrderDate { get; set; }

        // THAY ĐỔI Ở ĐÂY:
        public OrderStatus Status { get; set; } // Đổi từ "int" sang "OrderStatus"

        [Column(TypeName = "decimal(18, 2)")]
        public decimal TotalAmount { get; set; }

        // Thông tin người mua
        public required string CustomerName { get; set; }
        public required string ShippingAddress { get; set; }
        public required string ShippingPhone { get; set; }

        // Khóa ngoại
        public string UserId { get; set; } = null!;

        // Thuộc tính điều hướng
        public ApplicationUser User { get; set; } = null!;
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}