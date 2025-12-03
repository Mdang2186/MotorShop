using Microsoft.EntityFrameworkCore;
using MotorShop.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MotorShop.Data.Seeders
{
    public static class BranchInventorySeeder
    {
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            // Nếu đã có dữ liệu thì thôi
            if (await context.BranchInventories.AnyAsync()) return;

            var branches = await context.Branches
                .OrderBy(b => b.Id)
                .ToListAsync();

            var products = await context.Products
                .OrderBy(p => p.Id)
                .ToListAsync();

            if (!branches.Any() || !products.Any()) return;

            var inventories = new List<BranchInventory>();

            foreach (var product in products)
            {
                // Tổng tồn kho hiện tại đang lưu trong Product.StockQuantity
                var total = product.StockQuantity;

                // Nếu sản phẩm rất ít (<= 3), cho chỉ 1 chi nhánh có hàng
                if (total <= 3)
                {
                    inventories.Add(new BranchInventory
                    {
                        BranchId = branches[0].Id,   // Hà Nội 1
                        ProductId = product.Id,
                        Quantity = total
                    });

                    // Các chi nhánh còn lại: hết hàng
                    foreach (var br in branches.Skip(1))
                    {
                        inventories.Add(new BranchInventory
                        {
                            BranchId = br.Id,
                            ProductId = product.Id,
                            Quantity = 0
                        });
                    }
                }
                else
                {
                    // Chia tồn kho cho 5 chi nhánh, đảm bảo ít nhất 1 chi nhánh hết hàng
                    int b1 = (int)(total * 0.4m);
                    int b2 = (int)(total * 0.25m);
                    int b3 = (int)(total * 0.2m);
                    int b4 = (int)(total * 0.1m);

                    int used = b1 + b2 + b3 + b4;
                    int b5 = total - used;

                    // Để “hết hàng” ở một số chi nhánh:
                    // giả sử chi nhánh Đà Nẵng (index 2) đôi khi hết hàng
                    if (b3 < 1)
                    {
                        b3 = 0;
                        b5 = total - (b1 + b2 + b4);
                    }

                    inventories.Add(new BranchInventory { BranchId = branches[0].Id, ProductId = product.Id, Quantity = b1 });
                    inventories.Add(new BranchInventory { BranchId = branches[1].Id, ProductId = product.Id, Quantity = b2 });
                    inventories.Add(new BranchInventory { BranchId = branches[2].Id, ProductId = product.Id, Quantity = b3 }); // có thể = 0
                    inventories.Add(new BranchInventory { BranchId = branches[3].Id, ProductId = product.Id, Quantity = b4 });
                    inventories.Add(new BranchInventory { BranchId = branches[4].Id, ProductId = product.Id, Quantity = b5 });
                }
            }

            context.BranchInventories.AddRange(inventories);
            await context.SaveChangesAsync();
        }
    }
}
