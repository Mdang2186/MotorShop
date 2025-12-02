// File: Data/Seeders/TagSeeder.cs
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MotorShop.Models;
using MotorShop.Data;

namespace MotorShop.Data.Seeders
{
    public static class TagSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            // 1) Seed bảng Tags nếu chưa có
            if (!await context.Tags.AnyAsync())
            {
                var tags = new[]
                {
                    new Tag {
                        Name = "Đi phố / đi làm",
                        Slug = "usage-city",
                        Description = "Xe phù hợp đi lại trong phố, đi làm hằng ngày."
                    },
                    new Tag {
                        Name = "Chạy đơn / giao hàng",
                        Slug = "usage-delivery",
                        Description = "Xe phù hợp chạy đơn, giao hàng, shipper."
                    },
                    new Tag {
                        Name = "Đi xa / đi tour",
                        Slug = "usage-touring",
                        Description = "Xe phù hợp đi xa, đi tour, đường trường."
                    },

                    new Tag {
                        Name = "Tiết kiệm xăng",
                        Slug = "feature-fuel-saving",
                        Description = "Mức tiêu hao nhiên liệu thấp, tiết kiệm xăng."
                    },
                    new Tag {
                        Name = "Xe nhẹ, dễ dắt",
                        Slug = "feature-lightweight",
                        Description = "Trọng lượng xe nhẹ, dễ dắt và xoay trở."
                    },
                    new Tag {
                        Name = "Êm ái, thoải mái",
                        Slug = "feature-comfort",
                        Description = "Xe êm, tư thế ngồi thoải mái cho người lái và người ngồi sau."
                    },
                    new Tag {
                        Name = "Ngoại hình sang",
                        Slug = "feature-premium",
                        Description = "Thiết kế sang trọng, nhiều trang bị cao cấp."
                    },
                    new Tag {
                        Name = "Chi phí rẻ",
                        Slug = "feature-cheap-maintenance",
                        Description = "Chi phí bảo dưỡng, sửa chữa, thay thế phụ tùng thấp."
                    },
                    new Tag {
                        Name = "Yên thấp, dễ chống chân",
                        Slug = "feature-low-seat",
                        Description = "Độ cao yên thấp, dễ chống chân cho người thấp."
                    },
                    new Tag {
                        Name = "Thể thao, mạnh mẽ",
                        Slug = "feature-sporty",
                        Description = "Cảm giác lái thể thao, động cơ mạnh mẽ."
                    },

                    new Tag {
                        Name = "Người thấp",
                        Slug = "height-short",
                        Description = "Phù hợp người có chiều cao khiêm tốn."
                    },
                    new Tag {
                        Name = "Cao 1m55–1m65",
                        Slug = "height-155-165",
                        Description = "Phù hợp người cao khoảng 1m55 đến 1m65."
                    },
                    new Tag {
                        Name = "Cao 1m65–1m75",
                        Slug = "height-165-175",
                        Description = "Phù hợp người cao khoảng 1m65 đến 1m75."
                    },
                    new Tag {
                        Name = "Người cao",
                        Slug = "height-tall",
                        Description = "Phù hợp người có chiều cao trên trung bình."
                    },

                    new Tag {
                        Name = "Phù hợp người mới",
                        Slug = "exp-beginner",
                        Description = "Dễ điều khiển, phù hợp người mới đi xe."
                    },
                    new Tag {
                        Name = "Có kinh nghiệm",
                        Slug = "exp-experienced",
                        Description = "Phù hợp người đã có kinh nghiệm lái xe."
                    },
                };

                await context.Tags.AddRangeAsync(tags);
                await context.SaveChangesAsync();
            }

            // 2) Seed ProductTags cho một số xe tiêu biểu (nếu chưa có gì)
            if (await context.ProductTags.AnyAsync())
                return; // đã có rồi thì thôi, tránh trùng

            var tagsDict = await context.Tags
                .ToDictionaryAsync(t => t.Slug, t => t.Id);

            var products = await context.Products.ToListAsync();

            void AddTag(string productKeyword, params string[] tagSlugs)
            {
                var prod = products
                    .FirstOrDefault(p => p.Name.Contains(productKeyword));

                if (prod == null) return;

                foreach (var slug in tagSlugs)
                {
                    if (!tagsDict.TryGetValue(slug, out var tagId)) continue;

                    var exists = context.ProductTags
                        .Any(pt => pt.ProductId == prod.Id && pt.TagId == tagId);
                    if (!exists)
                    {
                        context.ProductTags.Add(new ProductTag
                        {
                            ProductId = prod.Id,
                            TagId = tagId
                        });
                    }
                }
            }

            // === Gán Tag cho một số xe phổ biến ===

            // Honda Vision – tay ga đi phố, tiết kiệm, cho người mới / người thấp
            AddTag("Vision", "usage-city", "feature-fuel-saving", "feature-comfort",
                               "feature-low-seat", "exp-beginner");

            // Honda Wave Alpha – xe số rẻ, bền, tiết kiệm, đi làm/chạy đơn
            AddTag("Wave Alpha", "usage-city", "usage-delivery",
                                 "feature-fuel-saving", "feature-cheap-maintenance",
                                 "exp-beginner");

            // Honda Future – xe số êm, bền, đi làm xa
            AddTag("Future", "usage-city", "usage-delivery",
                               "feature-fuel-saving", "feature-comfort");

            // Yamaha Janus – tay ga nhẹ, cho nữ/người thấp, đi phố
            AddTag("Janus", "usage-city", "feature-lightweight",
                             "feature-low-seat", "feature-fuel-saving",
                             "exp-beginner");

            // Yamaha Sirius – xe số chạy đơn, bền, rẻ
            AddTag("Sirius", "usage-city", "usage-delivery",
                              "feature-cheap-maintenance", "feature-fuel-saving",
                              "exp-beginner");

            // Exciter / Winner / Raider – xe côn tay thể thao, cho người có kinh nghiệm
            AddTag("Winner X", "usage-city", "usage-touring",
                               "feature-sporty", "exp-experienced");
            AddTag("Exciter", "usage-city", "usage-touring",
                               "feature-sporty", "exp-experienced");
            AddTag("Raider", "usage-city", "feature-sporty", "exp-experienced");

            // SH, Vespa – xe sang, đi phố, êm ái
            AddTag("SH 160i", "usage-city", "feature-premium", "feature-comfort");
            AddTag("SH Mode", "usage-city", "feature-premium", "feature-comfort");
            AddTag("Vespa", "usage-city", "feature-premium", "feature-comfort");

            // VinFast Klara / Vento / Evo – xe điện, đi phố, tiết kiệm chi phí
            AddTag("Klara", "usage-city", "feature-fuel-saving", "feature-cheap-maintenance");
            AddTag("Vento", "usage-city", "feature-fuel-saving", "feature-cheap-maintenance");
            AddTag("Evo 200", "usage-city", "feature-fuel-saving", "feature-cheap-maintenance");

            await context.SaveChangesAsync();
        }
    }
}
