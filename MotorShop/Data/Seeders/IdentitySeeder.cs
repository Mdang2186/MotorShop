using Microsoft.AspNetCore.Identity;
using MotorShop.Models;
using MotorShop.Utilities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MotorShop.Data.Seeders
{
    public static class IdentitySeeder
    {
        public static async Task SeedAsync(RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager)
        {
            // --- 2.1. Roles ---
            if (!await roleManager.RoleExistsAsync(SD.Role_Admin)) await roleManager.CreateAsync(new IdentityRole(SD.Role_Admin));
            if (!await roleManager.RoleExistsAsync(SD.Role_User)) await roleManager.CreateAsync(new IdentityRole(SD.Role_User));

            // --- 2.2. Admin & Manager ---
            if (await userManager.FindByEmailAsync("admin@motorshop.vn") == null)
            {
                var admin = new ApplicationUser { UserName = "admin@motorshop.vn", Email = "admin@motorshop.vn", FullName = "Quản Trị Hệ Thống", PhoneNumber = "0909999888", Address = "Tòa nhà Bitexco, Q1, TP.HCM", EmailConfirmed = true, CreatedAt = DateTime.UtcNow };
                await userManager.CreateAsync(admin, "Admin@123");
                await userManager.AddToRoleAsync(admin, SD.Role_Admin);
            }

            if (await userManager.FindByEmailAsync("manager@motorshop.vn") == null)
            {
                var manager = new ApplicationUser { UserName = "manager@motorshop.vn", Email = "manager@motorshop.vn", FullName = "Trần Quản Lý", EmailConfirmed = true, PhoneNumber = "0909111222", Address = "Cầu Giấy, Hà Nội", CreatedAt = DateTime.UtcNow };
                await userManager.CreateAsync(manager, "Manager@123");
                await userManager.AddToRoleAsync(manager, SD.Role_Admin);
            }

            // --- 2.3. 30 Khách hàng mẫu ---
            if (await userManager.GetUsersInRoleAsync(SD.Role_User).ContinueWith(t => t.Result.Count) < 5)
            {
                var customerData = new List<(string Email, string Name, string Phone, string Addr)>
                {
                    ("minh.tuan@gmail.com", "Trần Minh Tuấn", "0912345601", "123 Nguyễn Trãi, Q.Thanh Xuân, Hà Nội"),
                    ("huong.lan@yahoo.com", "Nguyễn Thị Lan Hương", "0987654302", "45 Lê Lợi, Q.1, TP.HCM"),
                    ("duc.thang@hotmail.com", "Lê Đức Thắng", "0909123403", "78 đường 3/2, Q.Ninh Kiều, Cần Thơ"),
                    ("thanh.hang@gmail.com", "Phạm Thanh Hằng", "0933445504", "12 Trần Phú, TP.Đà Nẵng"),
                    ("quang.huy@outlook.com", "Hoàng Quang Huy", "0977889905", "Số 5, đường Hùng Vương, TP.Huế"),
                    ("ngoc.anh@gmail.com", "Vũ Ngọc Anh", "0911223306", "Khu đô thị Ecopark, Hưng Yên"),
                    ("van.nam@gmail.com", "Đỗ Văn Nam", "0944556607", "88 Lý Thường Kiệt, TP.Vinh, Nghệ An"),
                    ("thi.mai@yahoo.com", "Bùi Thị Mai", "0966778808", "25 Nguyễn Văn Linh, Q.7, TP.HCM"),
                    ("huy.hoang@gmail.com", "Ngô Huy Hoàng", "0999000109", "102 Bà Triệu, Hoàn Kiếm, Hà Nội"),
                    ("phuong.thao@hotmail.com", "Đặng Phương Thảo", "0988777610", "Tòa nhà Landmark 81, Bình Thạnh, TP.HCM"),
                    ("tuan.kiet@gmail.com", "Lý Tuấn Kiệt", "0977111211", "Số 10, đường 30/4, TP.Biên Hòa, Đồng Nai"),
                    ("minh.chau@yahoo.com", "Hồ Minh Châu", "0966222312", "56 Đại lộ Bình Dương, TP.Thủ Dầu Một"),
                    ("quoc.bao@gmail.com", "Dương Quốc Bảo", "0955333413", "789 Trần Hưng Đạo, TP.Quy Nhơn"),
                    ("anh.thu@outlook.com", "Trương Anh Thư", "0944444514", "34 Lê Duẩn, TP.Buôn Ma Thuột"),
                    ("hoang.son@gmail.com", "Võ Hoàng Sơn", "0933555615", "12 Nguyễn Văn Cừ, TP.Hạ Long, Quảng Ninh"),
                    ("thuy.tien@yahoo.com", "Đinh Thủy Tiên", "0922666716", "99 đường 2/9, Hải Châu, Đà Nẵng"),
                    ("duc.minh@gmail.com", "Tạ Đức Minh", "0911777817", "Số 1 Đinh Tiên Hoàng, Q.Hoàn Kiếm, Hà Nội"),
                    ("bao.ngoc@hotmail.com", "Cao Bảo Ngọc", "0909888918", "456 Nguyễn Thị Minh Khai, Q.3, TP.HCM"),
                    ("gia.huy@gmail.com", "Phan Gia Huy", "0988999019", "77 đường Cách Mạng Tháng 8, TP.Cần Thơ"),
                    ("khanh.linh@yahoo.com", "Mai Khánh Linh", "0977000120", "234 Phạm Văn Đồng, TP.Nha Trang"),
                    ("thanh.tung@gmail.com", "Bùi Thanh Tùng", "0966111221", "55 đường Lạch Tray, TP.Hải Phòng"),
                    ("hai.yen@outlook.com", "Lê Hải Yến", "0955222322", "89 Nguyễn Huệ, TP.Huế"),
                    ("viet.dung@gmail.com", "Trần Việt Dũng", "0944333423", "67 Trần Phú, TP.Vũng Tàu"),
                    ("minh.hang@yahoo.com", "Nguyễn Minh Hằng", "0933444524", "123 đường Phan Đình Phùng, TP.Đà Lạt"),
                    ("trong.hieu@gmail.com", "Phạm Trọng Hiếu", "0922555625", "456 đường Trần Hưng Đạo, TP.Nam Định"),
                    ("thu.ha@hotmail.com", "Vũ Thu Hà", "0911666726", "789 đường Lê Lợi, TP.Thanh Hóa"),
                    ("tuan.anh@gmail.com", "Đỗ Tuấn Anh", "0909777827", "Số 5 đường Hùng Vương, TP.Việt Trì"),
                    ("ngoc.huyen@yahoo.com", "Hoàng Ngọc Huyền", "0988888928", "34 đường Nguyễn Trãi, TP.Hà Giang"),
                    ("van.thanh@gmail.com", "Lương Văn Thành", "0977999029", "56 đường Lê Duẩn, TP.Pleiku"),
                    ("phuong.mai@outlook.com", "Trịnh Phương Mai", "0966000130", "99 đường 30/4, TP.Tây Ninh")
                };

                foreach (var cus in customerData)
                {
                    if (await userManager.FindByEmailAsync(cus.Email) == null)
                    {
                        var user = new ApplicationUser
                        {
                            UserName = cus.Email,
                            Email = cus.Email,
                            FullName = cus.Name,
                            PhoneNumber = cus.Phone,
                            Address = cus.Addr,
                            EmailConfirmed = true,
                            CreatedAt = DateTime.UtcNow.AddDays(-new Random().Next(1, 365))
                        };
                        await userManager.CreateAsync(user, "Customer@123");
                        await userManager.AddToRoleAsync(user, SD.Role_User);
                    }
                }
            }
        }
    }
}