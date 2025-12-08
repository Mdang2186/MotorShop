using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MotorShop.Models;
using MotorShop.Data;
using System.Collections.Generic; // Thêm namespace này để dùng List

namespace MotorShop.Data.Seeders
{
    public static class TagSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            // 1) Seed bảng Tags nếu chưa có
            if (!await context.Tags.AnyAsync())
            {
                var tags = new List<Tag>
                {
                    // ==================== [PHẦN CŨ: XE MÁY] ====================
                    new Tag { Name = "Đi phố / đi làm", Slug = "usage-city", Description = "Xe phù hợp đi lại trong phố, đi làm hằng ngày." },
                    new Tag { Name = "Chạy đơn / giao hàng", Slug = "usage-delivery", Description = "Xe phù hợp chạy đơn, giao hàng, shipper." },
                    new Tag { Name = "Đi xa / đi tour", Slug = "usage-touring", Description = "Xe phù hợp đi xa, đi tour, đường trường." },

                    new Tag { Name = "Tiết kiệm xăng", Slug = "feature-fuel-saving", Description = "Mức tiêu hao nhiên liệu thấp, tiết kiệm xăng." },
                    new Tag { Name = "Xe nhẹ, dễ dắt", Slug = "feature-lightweight", Description = "Trọng lượng xe nhẹ, dễ dắt và xoay trở." },
                    new Tag { Name = "Êm ái, thoải mái", Slug = "feature-comfort", Description = "Xe êm, tư thế ngồi thoải mái cho người lái và người ngồi sau." },
                    new Tag { Name = "Ngoại hình sang", Slug = "feature-premium", Description = "Thiết kế sang trọng, nhiều trang bị cao cấp." },
                    new Tag { Name = "Chi phí rẻ", Slug = "feature-cheap-maintenance", Description = "Chi phí bảo dưỡng, sửa chữa, thay thế phụ tùng thấp." },
                    new Tag { Name = "Yên thấp, dễ chống chân", Slug = "feature-low-seat", Description = "Độ cao yên thấp, dễ chống chân cho người thấp." },
                    new Tag { Name = "Thể thao, mạnh mẽ", Slug = "feature-sporty", Description = "Cảm giác lái thể thao, động cơ mạnh mẽ." },

                    new Tag { Name = "Người thấp", Slug = "height-short", Description = "Phù hợp người có chiều cao khiêm tốn." },
                    new Tag { Name = "Cao 1m55–1m65", Slug = "height-155-165", Description = "Phù hợp người cao khoảng 1m55 đến 1m65." },
                    new Tag { Name = "Cao 1m65–1m75", Slug = "height-165-175", Description = "Phù hợp người cao khoảng 1m65 đến 1m75." },
                    new Tag { Name = "Người cao", Slug = "height-tall", Description = "Phù hợp người có chiều cao trên trung bình." },

                    new Tag { Name = "Phù hợp người mới", Slug = "exp-beginner", Description = "Dễ điều khiển, phù hợp người mới đi xe." },
                    new Tag { Name = "Có kinh nghiệm", Slug = "exp-experienced", Description = "Phù hợp người đã có kinh nghiệm lái xe." },

                    // ==================== [PHẦN MỚI: PHỤ TÙNG] ====================
                    new Tag { Name = "Dầu nhớt & Phụ gia", Slug = "part-oil", Description = "Các loại nhớt máy, nhớt láp, nước mát, phụ gia..." },
                    new Tag { Name = "Lốp & Săm xe", Slug = "part-tire", Description = "Vỏ xe không săm, có săm, lốp thể thao..." },
                    new Tag { Name = "Hệ thống phanh", Slug = "part-brake", Description = "Bố thắng, đĩa phanh, dây dầu..." },
                    new Tag { Name = "Nhông sên dĩa", Slug = "part-chain", Description = "Bộ nhông xích, dây curoa truyền động..." },
                    new Tag { Name = "Lọc gió & Lọc nhớt", Slug = "part-filter", Description = "Các loại lọc bảo dưỡng định kỳ." },
                    new Tag { Name = "Ắc quy & Điện", Slug = "part-battery", Description = "Bình ắc quy, bugi, bóng đèn..." },
                    new Tag { Name = "Gương & Kính", Slug = "part-mirror", Description = "Kính chiếu hậu, kính chắn gió..." },
                    new Tag { Name = "Phụ kiện trang trí", Slug = "part-accessory", Description = "Bao tay, gù, ốc kiểu, đồ chơi xe..." },
                    new Tag { Name = "Phụ tùng chính hãng", Slug = "part-genuine", Description = "Hàng zin chính hãng Honda, Yamaha..." }
                };

                await context.Tags.AddRangeAsync(tags);
                await context.SaveChangesAsync();
            }

            // 2) Seed ProductTags
            // Lấy danh sách sản phẩm và tag để map
            var tagsDict = await context.Tags.ToDictionaryAsync(t => t.Slug, t => t.Id);
            var products = await context.Products.Include(p => p.ProductTags).ToListAsync();

            // Hàm local: Thêm tag cho sản phẩm nếu tên chứa từ khóa
            void AddTag(string productKeyword, params string[] tagSlugs)
            {
                // Tìm tất cả sản phẩm có tên chứa keyword (không phân biệt hoa thường)
                var matchedProducts = products.Where(p => p.Name.ToLower().Contains(productKeyword.ToLower()));

                foreach (var prod in matchedProducts)
                {
                    foreach (var slug in tagSlugs)
                    {
                        if (!tagsDict.TryGetValue(slug, out var tagId)) continue;

                        // Kiểm tra nếu chưa có tag thì mới thêm
                        if (!prod.ProductTags.Any(pt => pt.TagId == tagId))
                        {
                            context.ProductTags.Add(new ProductTag
                            {
                                ProductId = prod.Id,
                                TagId = tagId
                            });
                        }
                    }
                }
            }

            // ==================== [PHẦN CŨ: GÁN TAG XE MÁY] ====================
            AddTag("Vision", "usage-city", "feature-fuel-saving", "feature-comfort", "feature-low-seat", "exp-beginner");
            AddTag("Wave Alpha", "usage-city", "usage-delivery", "feature-fuel-saving", "feature-cheap-maintenance", "exp-beginner");
            AddTag("Future", "usage-city", "usage-delivery", "feature-fuel-saving", "feature-comfort");
            AddTag("Janus", "usage-city", "feature-lightweight", "feature-low-seat", "feature-fuel-saving", "exp-beginner");
            AddTag("Sirius", "usage-city", "usage-delivery", "feature-cheap-maintenance", "feature-fuel-saving", "exp-beginner");
            AddTag("Winner X", "usage-city", "usage-touring", "feature-sporty", "exp-experienced");
            AddTag("Exciter", "usage-city", "usage-touring", "feature-sporty", "exp-experienced");
            AddTag("Raider", "usage-city", "feature-sporty", "exp-experienced");
            AddTag("Satria", "usage-city", "feature-sporty", "exp-experienced"); // Thêm Satria
            AddTag("SH 160i", "usage-city", "feature-premium", "feature-comfort", "height-tall");
            AddTag("SH Mode", "usage-city", "feature-premium", "feature-comfort");
            AddTag("Vespa", "usage-city", "feature-premium", "feature-comfort");
            AddTag("Liberty", "usage-city", "feature-premium", "feature-comfort"); // Thêm Liberty
            AddTag("Klara", "usage-city", "feature-fuel-saving", "feature-cheap-maintenance");
            AddTag("Vento", "usage-city", "feature-fuel-saving", "feature-cheap-maintenance");
            AddTag("Evo 200", "usage-city", "feature-fuel-saving", "feature-cheap-maintenance");
            AddTag("Ducati", "feature-sporty", "feature-premium", "exp-experienced"); // Thêm PKL
            AddTag("BMW", "feature-sporty", "feature-premium", "exp-experienced", "usage-touring");

            // ==================== [PHẦN MỚI: GÁN TAG PHỤ TÙNG] ====================
            // Dầu nhớt
            AddTag("Nhớt", "part-oil");
            AddTag("Dầu máy", "part-oil");

            // Lốp xe
            AddTag("Lốp", "part-tire");
            AddTag("Vỏ xe", "part-tire");

            // Phanh
            AddTag("Má phanh", "part-brake", "part-genuine");
            AddTag("Bố thắng", "part-brake", "part-genuine");
            AddTag("Đĩa phanh", "part-brake");

            // Nhông sên dĩa / Dây curoa
            AddTag("Dây curoa", "part-chain", "part-genuine");
            AddTag("Nhông sên dĩa", "part-chain");
            AddTag("Sên", "part-chain");

            // Lọc gió
            AddTag("Lọc gió", "part-filter", "part-genuine");

            // Điện
            AddTag("Bugi", "part-battery");
            AddTag("Ắc quy", "part-battery");
            AddTag("Sạc pin", "part-battery", "part-genuine");

            // Gương kính
            AddTag("Kính chắn gió", "part-mirror", "part-accessory");
            AddTag("Gương chiếu hậu", "part-mirror", "part-accessory");

            // Đồ chơi / Phụ kiện
            AddTag("Bao tay", "part-accessory");
            AddTag("Gù", "part-accessory");

            await context.SaveChangesAsync();
        }
    }
}