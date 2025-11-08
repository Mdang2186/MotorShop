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
        public static async Task SeedAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            if (await context.Orders.CountAsync() > 10) return; // Đã có nhiều đơn thì thôi

            var customers = await userManager.GetUsersInRoleAsync(Utilities.SD.Role_User);
            var products = await context.Products.ToListAsync();
            if (!customers.Any() || !products.Any()) return;

            var rnd = new Random();
            var orders = new List<Order>();

            // Mỗi khách hàng tạo 3-8 đơn hàng
            foreach (var customer in customers)
            {
                int orderCount = rnd.Next(3, 9);
                for (int i = 0; i < orderCount; i++)
                {
                    // Random ngày trong 6 tháng qua (tập trung nhiều vào tháng hiện tại để dashboard đẹp)
                    int daysAgo = rnd.Next(0, 10) < 3 ? rnd.Next(0, 30) : rnd.Next(30, 180);
                    var date = DateTime.UtcNow.AddDays(-daysAgo).AddHours(rnd.Next(-12, 12));

                    var status = (OrderStatus)rnd.Next(0, 6); // 0-5

                    var order = new Order
                    {
                        UserId = customer.Id,
                        OrderDate = date,
                        Status = status,
                        ReceiverName = customer.FullName,
                        ReceiverPhone = customer.PhoneNumber,
                        ShippingAddress = customer.Address,
                        PaymentMethod = (PaymentMethod)rnd.Next(0, 4), // Random phương thức TT
                        // Đơn hoàn thành/đã giao coi như đã thanh toán
                        PaymentStatus = (status == OrderStatus.Completed || status == OrderStatus.Delivered) ? PaymentStatus.Paid : PaymentStatus.Pending
                    };

                    // Mỗi đơn 1-2 sản phẩm (xe máy thường mua 1 cái thôi)
                    int itemCount = rnd.Next(1, 3);
                    decimal total = 0;
                    for (int j = 0; j < itemCount; j++)
                    {
                        var prod = products[rnd.Next(products.Count)];
                        order.OrderItems.Add(new OrderItem { ProductId = prod.Id, Quantity = 1, UnitPrice = prod.Price });
                        total += prod.Price;
                    }
                    order.TotalAmount = total;
                    orders.Add(order);
                }
            }

            // Sắp xếp theo ngày đặt để trông tự nhiên hơn khi insert
            context.Orders.AddRange(orders.OrderBy(o => o.OrderDate));
            await context.SaveChangesAsync();
        }
    }
}