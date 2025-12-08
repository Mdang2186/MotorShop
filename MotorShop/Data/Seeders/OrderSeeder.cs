using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MotorShop.Models;
using MotorShop.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MotorShop.Data.Seeders
{
    public static class OrderSeeder
    {
        public static async Task SeedAsync(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            // Nếu DB đã có > 10 đơn thì không seed nữa để tránh trùng / loạn thống kê
            if (await context.Orders.CountAsync() > 10) return;

            // Lấy khách hàng (role User)
            var customers = await userManager.GetUsersInRoleAsync(Utilities.SD.Role_User);

            // Lấy sản phẩm, chi nhánh, shipper
            var products = await context.Products.AsNoTracking().ToListAsync();
            var branches = await context.Branches.AsNoTracking().Where(b => b.IsActive).ToListAsync();
            var shippers = await context.Shippers.AsNoTracking().Where(s => s.IsActive).ToListAsync();

            if (!customers.Any() || !products.Any())
                return; // không đủ dữ liệu để seed

            var rnd = new Random();
            var orders = new List<Order>();

            // Mỗi khách hàng tạo 3-8 đơn hàng
            foreach (var customer in customers)
            {
                int orderCount = rnd.Next(3, 9);

                for (int i = 0; i < orderCount; i++)
                {
                    // Random ngày trong 6 tháng qua (ưu tiên nhiều vào 30 ngày gần nhất)
                    int daysAgo = rnd.Next(0, 10) < 3
                        ? rnd.Next(0, 30)
                        : rnd.Next(30, 180);

                    var date = DateTime.UtcNow
                        .AddDays(-daysAgo)
                        .AddHours(rnd.Next(-12, 12));

                    // Random trạng thái đơn
                    var status = (OrderStatus)rnd.Next(0, 6); // tuỳ enum của bạn có bao nhiêu trạng thái

                    // Chọn random phương thức thanh toán
                    var paymentMethod = (PaymentMethod)rnd.Next(0, 4);

                    // Gán chi nhánh nhận xe (nếu có)
                    int? pickupBranchId = null;
                    if (branches.Any())
                    {
                        var br = branches[rnd.Next(branches.Count)];
                        pickupBranchId = br.Id;
                    }

                    // Gán shipper (chủ yếu cho đơn giao tận nơi)
                    int? shipperId = null;
                    if (shippers.Any())
                    {
                        // ~70% đơn có shipper (giao tận nhà), 30% nhận tại chi nhánh
                        if (rnd.NextDouble() < 0.7)
                        {
                            shipperId = shippers[rnd.Next(shippers.Count)].Id;
                        }
                    }

                    // Địa chỉ giao hàng fallback nếu customer chưa có Address
                    var address = string.IsNullOrWhiteSpace(customer.Address)
                        ? "Khách nhận tại showroom / địa chỉ demo"
                        : customer.Address;

                    var order = new Order
                    {
                        UserId = customer.Id,
                        OrderDate = date,
                        Status = status,

                        ReceiverName = string.IsNullOrWhiteSpace(customer.FullName)
                            ? "Khách hàng MotorShop"
                            : customer.FullName,
                        ReceiverPhone = string.IsNullOrWhiteSpace(customer.PhoneNumber)
                            ? "0900 000 000"
                            : customer.PhoneNumber,
                        ShippingAddress = address,

                        PaymentMethod = paymentMethod,
                        PickupBranchId = pickupBranchId,
                        ShipperId = shipperId
                    };

                    // Mỗi đơn 1-2 sản phẩm (xe thường mua 1 chiếc)
                    int itemCount = rnd.Next(1, 3);
                    decimal itemsTotal = 0;

                    for (int j = 0; j < itemCount; j++)
                    {
                        var prod = products[rnd.Next(products.Count)];
                        var qty = 1; // xe máy thường chỉ 1 chiếc

                        order.OrderItems.Add(new OrderItem
                        {
                            ProductId = prod.Id,
                            Quantity = qty,
                            UnitPrice = prod.Price
                        });

                        itemsTotal += prod.Price * qty;
                    }

                    // Phí vận chuyển: đơn nhận tại chi nhánh → 0, còn lại 0 / 150k / 250k
                    decimal shippingFee = 0m;
                    if (shipperId.HasValue)
                    {
                        var feeChoice = rnd.Next(0, 3); // 0, 1, 2
                        shippingFee = feeChoice switch
                        {
                            0 => 0m,
                            1 => 150_000m,
                            _ => 250_000m
                        };
                    }

                    // Giảm giá: khoảng 0 – 3% cho đẹp
                    decimal discount = 0m;
                    if (rnd.NextDouble() < 0.4) // 40% đơn có giảm giá
                    {
                        var rate = rnd.NextDouble() * 0.03; // 0–3%
                        discount = Math.Round(itemsTotal * (decimal)rate, 0);
                    }

                    order.ShippingFee = shippingFee;
                    order.DiscountAmount = discount;
                    order.TotalAmount = itemsTotal + shippingFee - discount;

                    // Đơn hoàn thành/đã giao coi như đã thanh toán
                    order.PaymentStatus =
                        (status == OrderStatus.Completed || status == OrderStatus.Delivered)
                            ? PaymentStatus.Paid
                            : PaymentStatus.Pending;

                    orders.Add(order);
                }
            }

            // Sắp xếp theo ngày đặt trước khi insert để nhìn tự nhiên
            var ordered = orders.OrderBy(o => o.OrderDate).ToList();

            context.Orders.AddRange(ordered);
            await context.SaveChangesAsync();
        }
    }
}
