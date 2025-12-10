using Microsoft.EntityFrameworkCore;
using MotorShop.Models;
using MotorShop.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MotorShop.Data.Seeders
{
    public static class ReviewSeeder
    {
        // --- 1. KHO DỮ LIỆU COMMENT MẪU (Cho thật hơn) ---
        private static readonly List<string> BikeComments_Good = new()
        {
            "Xe chạy rất êm, giao hàng đúng hẹn. Nhân viên tư vấn nhiệt tình.",
            "Màu sơn bên ngoài đẹp hơn trong ảnh. Rất ưng ý!",
            "Động cơ bốc, đi phượt rất sướng. Cảm ơn shop.",
            "Mua xe ở đây thủ tục nhanh gọn, 30 phút là xong giấy tờ.",
            "Đã nhận xe, đóng gói cẩn thận, không trầy xước tí nào.",
            "Xe chính hãng, check bảo hành điện tử ok. 5 sao!",
            "Tiết kiệm xăng thực sự, đi làm hàng ngày rất ổn.",
            "Kiểu dáng đẹp, ngồi thoải mái, cốp rộng để đồ tiện lợi."
        };

        private static readonly List<string> BikeComments_Normal = new()
        {
            "Xe ổn trong tầm giá.",
            "Giao hàng hơi chậm một chút do vướng lịch đăng ký biển.",
            "Tạm được, chưa thấy lỗi lầm gì.",
            "Xe đi ổn, nhưng quà tặng kèm hơi ít.",
            "Cần chạy rodai kỹ mới mượt được."
        };

        private static readonly List<string> PartComments = new()
        {
            "Hàng chính hãng, tem mác đầy đủ.",
            "Lắp vừa zin cho xe, không cần chế cháo gì cả.",
            "Chất lượng tốt, giá rẻ hơn ra hãng thay.",
            "Giao hàng siêu tốc, đặt sáng chiều có luôn.",
            "Nhớt này chạy êm máy, mát hơn hẳn loại cũ.",
            "Phụ tùng đóng gói kỹ, shop uy tín.",
            "Đã lắp lên xe, hoạt động hoàn hảo."
        };

        public static async Task SeedAsync(ApplicationDbContext context)
        {
            // 1. Kiểm tra điều kiện tiên quyết
            if (!await context.Users.AnyAsync() || !await context.Products.AnyAsync()) return;

            var users = await context.Users.ToListAsync();
            var products = await context.Products
                                        .Include(p => p.Category) // Để check loại sản phẩm
                                        .ToListAsync();

            var rnd = new Random();
            var reviewsToAdd = new List<ProductReview>();

            // 2. Duyệt qua từng sản phẩm để đảm bảo xe nào cũng có đánh giá
            foreach (var product in products)
            {
                // Kiểm tra số lượng review hiện tại
                int currentReviews = await context.ProductReviews.CountAsync(r => r.ProductId == product.Id);

                // Mục tiêu: Mỗi sản phẩm có từ 3 đến 6 đánh giá
                int targetReviews = rnd.Next(3, 7);

                if (currentReviews < targetReviews)
                {
                    int needed = targetReviews - currentReviews;

                    for (int i = 0; i < needed; i++)
                    {
                        // a. Chọn ngẫu nhiên 1 khách hàng
                        var user = users[rnd.Next(users.Count)];

                        // b. TẠO ĐƠN HÀNG GIẢ LẬP (QUAN TRỌNG)
                        // Vì review bắt buộc phải có OrderId và trạng thái Completed
                        var fakeOrder = new Order
                        {
                            UserId = user.Id,
                            ReceiverName = user.FullName ?? "Khách hàng",
                            ReceiverPhone = user.PhoneNumber ?? "0909000111",
                            ShippingAddress = user.Address ?? "Tại cửa hàng",
                            OrderDate = DateTime.UtcNow.AddDays(-rnd.Next(5, 60)), // Mua cách đây 5-60 ngày
                            Status = OrderStatus.Completed, // Đã hoàn thành mới được review
                            PaymentStatus = PaymentStatus.Paid,
                            PaymentMethod = PaymentMethod.COD,
                            TotalAmount = product.Price,
                            ShippingFee = 0,
                            DiscountAmount = 0
                        };

                        // Thêm chi tiết đơn hàng
                        fakeOrder.OrderItems = new List<OrderItem>
                        {
                            new OrderItem
                            {
                                ProductId = product.Id,
                                Quantity = 1,
                                UnitPrice = product.Price
                            }
                        };

                        // Lưu đơn hàng trước để lấy OrderId
                        context.Orders.Add(fakeOrder);
                        await context.SaveChangesAsync();

                        // c. Chọn nội dung comment & rating phù hợp
                        bool isPart = product.Category?.Name == "Phụ tùng & Linh kiện" || (product.SKU != null && product.SKU.StartsWith("PT-"));

                        int rating;
                        string comment;

                        // Tỷ lệ: 70% 5 sao, 20% 4 sao, 10% 3 sao (Shop uy tín)
                        int luck = rnd.Next(1, 101);
                        if (luck <= 70)
                        {
                            rating = 5;
                            comment = isPart ? PartComments[rnd.Next(PartComments.Count)] : BikeComments_Good[rnd.Next(BikeComments_Good.Count)];
                        }
                        else if (luck <= 90)
                        {
                            rating = 4;
                            comment = isPart ? PartComments[rnd.Next(PartComments.Count)] : BikeComments_Good[rnd.Next(BikeComments_Good.Count)];
                        }
                        else
                        {
                            rating = 3;
                            comment = isPart ? "Sản phẩm tạm ổn." : BikeComments_Normal[rnd.Next(BikeComments_Normal.Count)];
                        }

                        // d. Tạo Review
                        var review = new ProductReview
                        {
                            ProductId = product.Id,
                            UserId = user.Id,
                            OrderId = fakeOrder.Id, // Link vào đơn hàng vừa tạo
                            Rating = rating,
                            Comment = comment,
                            CreatedAt = fakeOrder.OrderDate.AddDays(rnd.Next(1, 5)), // Review sau khi mua 1-5 ngày
                            UpdatedAt = null
                        };

                        context.ProductReviews.Add(review);
                    }
                }
            }

            // Lưu toàn bộ review
            await context.SaveChangesAsync();
        }
    }
}