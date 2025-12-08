using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MotorShop.Models;

namespace MotorShop.Data.Seeders
{
    public static class BankSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            // ========== 1) Seed bảng Banks ==========
            if (!await context.Banks.AnyAsync())
            {
                var banks = new List<Bank>
                {
                    new Bank
                    {
                        Name       = "Ngân hàng TMCP Đầu tư và Phát triển Việt Nam",
                        ShortName  = "BIDV",
                        Code       = "BIDV",
                        Bin        = "970418",
                        LogoUrl    = "/images/banks/bidv.png",
                        IsActive   = true,
                        SortOrder  = 1
                    },
                    new Bank
                    {
                        Name       = "Ngân hàng TMCP Ngoại thương Việt Nam",
                        ShortName  = "Vietcombank",
                        Code       = "VCB",
                        Bin        = "970436",
                        LogoUrl    = "/images/banks/vcb.png",
                        IsActive   = true,
                        SortOrder  = 2
                    },
                    new Bank
                    {
                        Name       = "Ngân hàng TMCP Công thương Việt Nam",
                        ShortName  = "VietinBank",
                        Code       = "CTG",
                        Bin        = "970415",
                        LogoUrl    = "/images/banks/vietinbank.png",
                        IsActive   = true,
                        SortOrder  = 3
                    },
                    new Bank
                    {
                        Name       = "Ngân hàng TMCP Kỹ thương Việt Nam",
                        ShortName  = "Techcombank",
                        Code       = "TCB",
                        Bin        = "970407",
                        LogoUrl    = "/images/banks/techcombank.png",
                        IsActive   = true,
                        SortOrder  = 4
                    },
                    new Bank
                    {
                        Name       = "Ngân hàng TMCP Quân đội",
                        ShortName  = "MB Bank",
                        Code       = "MBB",
                        Bin        = "970422",
                        LogoUrl    = "/images/banks/mbbank.png",
                        IsActive   = true,
                        SortOrder  = 5
                    }
                };

                await context.Banks.AddRangeAsync(banks);
                await context.SaveChangesAsync();
            }

            // ========== 2) Seed bảng ShopBankAccounts (tài khoản ngân hàng MotorShop) ==========
            if (!await context.ShopBankAccounts.AnyAsync())
            {
                var bidv = await context.Banks.FirstOrDefaultAsync(b => b.Code == "BIDV");
                var vcb = await context.Banks.FirstOrDefaultAsync(b => b.Code == "VCB");

                var accounts = new List<ShopBankAccount>();

                if (bidv != null)
                {
                    accounts.Add(new ShopBankAccount
                    {
                        BankId = bidv.Id,
                        AccountNumber = "0123456789",
                        AccountName = "CTY TNHH MOTOSHOP",
                        Branch = "Chi nhánh Hà Nội",
                        Note = "Tài khoản demo BIDV dùng tạo QR",
                        IsDefault = true,
                        IsActive = true
                    });
                }

                if (vcb != null)
                {
                    accounts.Add(new ShopBankAccount
                    {
                        BankId = vcb.Id,
                        AccountNumber = "9876543210",
                        AccountName = "CTY TNHH MOTOSHOP",
                        Branch = "Chi nhánh TP.HCM",
                        Note = "Tài khoản demo Vietcombank",
                        IsDefault = false,
                        IsActive = true
                    });
                }

                if (accounts.Count > 0)
                {
                    await context.ShopBankAccounts.AddRangeAsync(accounts);
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}
