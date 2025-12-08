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
            // Nếu đã có dữ liệu thì bỏ qua
            if (await context.BranchInventories.AnyAsync())
                return;

            // Chỉ lấy chi nhánh đang hoạt động
            var branches = await context.Branches
                .Where(b => b.IsActive)
                .OrderBy(b => b.Id)
                .ToListAsync();

            var products = await context.Products
                .OrderBy(p => p.Id)
                .ToListAsync();

            if (!branches.Any() || !products.Any())
                return;

            var inventories = new List<BranchInventory>();

            foreach (var product in products)
            {
                var total = product.StockQuantity;

                // Nếu không có tồn kho hoặc âm -> coi như 0
                if (total <= 0)
                {
                    // Tạo bản ghi 0 cho tất cả chi nhánh để vẫn xem được
                    foreach (var br in branches)
                    {
                        inventories.Add(new BranchInventory
                        {
                            BranchId = br.Id,
                            ProductId = product.Id,
                            Quantity = 0
                        });
                    }
                    continue;
                }

                int branchCount = branches.Count;

                // Trường hợp chỉ có 1 chi nhánh -> dồn hết
                if (branchCount == 1)
                {
                    inventories.Add(new BranchInventory
                    {
                        BranchId = branches[0].Id,
                        ProductId = product.Id,
                        Quantity = total
                    });
                    continue;
                }

                // Trường hợp 2–4 chi nhánh: chia đều tương đối
                if (branchCount > 1 && branchCount < 5)
                {
                    int baseQty = total / branchCount;
                    int remainder = total % branchCount;

                    for (int i = 0; i < branchCount; i++)
                    {
                        int qty = baseQty + (i < remainder ? 1 : 0);
                        inventories.Add(new BranchInventory
                        {
                            BranchId = branches[i].Id,
                            ProductId = product.Id,
                            Quantity = qty
                        });
                    }
                    continue;
                }

                // Trường hợp >= 5 chi nhánh:
                // Dùng 5 chi nhánh đầu làm “chi nhánh chính”, các chi nhánh còn lại = 0
                var mainBranches = branches.Take(5).ToList();
                var otherBranches = branches.Skip(5).ToList();

                int b1 = (int)(total * 0.40m);
                int b2 = (int)(total * 0.25m);
                int b3 = (int)(total * 0.20m);
                int b4 = (int)(total * 0.10m);

                int used = b1 + b2 + b3 + b4;
                int b5 = total - used;

                // Đảm bảo không bị âm (do làm tròn)
                if (b5 < 0)
                {
                    b5 = 0;
                }

                // Nếu Chi nhánh 3 bị quá nhỏ thì cho “hết hàng”
                if (b3 < 1)
                {
                    b3 = 0;
                    used = b1 + b2 + b4;
                    b5 = total - used;
                    if (b5 < 0) b5 = 0;
                }

                var qtyList = new[] { b1, b2, b3, b4, b5 };

                for (int i = 0; i < mainBranches.Count; i++)
                {
                    inventories.Add(new BranchInventory
                    {
                        BranchId = mainBranches[i].Id,
                        ProductId = product.Id,
                        Quantity = qtyList[i]
                    });
                }

                // Các chi nhánh khác: chưa có hàng (0)
                foreach (var br in otherBranches)
                {
                    inventories.Add(new BranchInventory
                    {
                        BranchId = br.Id,
                        ProductId = product.Id,
                        Quantity = 0
                    });
                }
            }

            context.BranchInventories.AddRange(inventories);
            await context.SaveChangesAsync();
        }
    }
}
