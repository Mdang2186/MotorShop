using Microsoft.EntityFrameworkCore;
using MotorShop.Models;
using System.Threading.Tasks;

namespace MotorShop.Data.Seeders
{
    public static class ShipperSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            if (await context.Shippers.AnyAsync())
                return;

            context.Shippers.AddRange(
                new Shipper
                {
                    Name = "Giao Hàng Nhanh",
                    Code = "GHN",
                    Phone = "1900 636 677",
                    Note = "Đối tác giao nhanh nội địa, hỗ trợ nhiều tỉnh thành.",
                    IsActive = true
                },
                new Shipper
                {
                    Name = "Giao Hàng Tiết Kiệm",
                    Code = "GHTK",
                    Phone = "1900 6092",
                    Note = "Chuyên tuyến tỉnh, COD linh hoạt.",
                    IsActive = true
                },
                new Shipper
                {
                    Name = "Viettel Post",
                    Code = "VTPOST",
                    Phone = "1900 8095",
                    Note = "Phủ sóng toàn quốc, hỗ trợ giao xe giấy tờ.",
                    IsActive = true
                },
                new Shipper
                {
                    Name = "VNPost Nhanh",
                    Code = "VNPOST",
                    Phone = "1900 54 54 81",
                    Note = "Bưu điện Việt Nam, phù hợp khu vực xa.",
                    IsActive = false   // ví dụ để sẵn 1 đơn vị tạm ngưng
                }
            );

            await context.SaveChangesAsync();
        }
    }
}
