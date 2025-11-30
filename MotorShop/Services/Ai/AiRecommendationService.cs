using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MotorShop.Data;
using MotorShop.Models;
using MotorShop.ViewModels.Ai;

namespace MotorShop.Services.Ai
{
    /// <summary>
    /// Core "mô hình" chấm điểm sản phẩm dựa trên Tag + Brand + Price.
    /// Không dùng thuộc tính đặc biệt nên an toàn với model hiện tại.
    /// </summary>
    public class AiRecommendationService
    {
        private readonly ApplicationDbContext _db;

        public AiRecommendationService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<List<AiSuggestionItem>> SuggestAsync(AiParsedQuery query, CancellationToken ct = default)
        {
            // 1. Query cơ bản
            var q = _db.Products
                .AsNoTracking()
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.ProductTags).ThenInclude(pt => pt.Tag)
                .Where(p => p.IsActive && p.IsPublished);

            // Lọc sơ theo ngân sách
            if (query.BudgetMin.HasValue)
                q = q.Where(p => p.Price >= query.BudgetMin.Value);
            if (query.BudgetMax.HasValue)
                q = q.Where(p => p.Price <= query.BudgetMax.Value);

            // Lọc sơ theo brand yêu cầu
            if (query.PreferredBrands.Any())
            {
                var brands = query.PreferredBrands.Select(b => b.ToLower()).ToList();
                q = q.Where(p => p.Brand != null && brands.Contains(p.Brand.Name.ToLower()));
            }

            var products = await q.ToListAsync(ct);
            if (!products.Any())
                return new List<AiSuggestionItem>();

            string[] desiredTagSlugs = query.PreferredTags
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct()
                .ToArray();

            // 2. Hàm tính điểm cho từng sản phẩm
            decimal Score(Product p)
            {
                decimal s = 0m;
                var tagSlugs = p.ProductTags
                                .Where(pt => pt.Tag != null && !string.IsNullOrEmpty(pt.Tag.Slug))
                                .Select(pt => pt.Tag.Slug!)
                                .ToArray();

                // 2.1. Trùng tag ưu tiên (fuel-saving, lightweight...)
                foreach (var t in desiredTagSlugs)
                    if (tagSlugs.Contains(t))
                        s += 3m;

                // 2.2. Mục đích -> usage-*
                if (query.Purpose == "city" && tagSlugs.Contains("usage-city"))
                    s += 4m;
                if (query.Purpose == "delivery" && tagSlugs.Contains("usage-delivery"))
                    s += 4m;
                if (query.Purpose == "touring" && tagSlugs.Contains("usage-touring"))
                    s += 4m;

                // 2.3. Chiều cao dùng tag height-*
                if (query.HeightCm.HasValue)
                {
                    if (query.HeightCm <= 155 && tagSlugs.Contains("height-short")) s += 4m;
                    if (query.HeightCm > 155 && query.HeightCm <= 165 && tagSlugs.Contains("height-155-165")) s += 4m;
                    if (query.HeightCm > 165 && query.HeightCm <= 175 && tagSlugs.Contains("height-165-175")) s += 4m;
                    if (query.HeightCm >= 175 && tagSlugs.Contains("height-tall")) s += 4m;
                }

                // 2.4. Beginner / experienced
                if (query.IsBeginner == true && tagSlugs.Contains("exp-beginner"))
                    s += 3m;
                if (query.IsBeginner == true && tagSlugs.Contains("feature-sporty"))
                    s -= 2m; // xe sport không hợp người mới

                if (query.IsBeginner == false && tagSlugs.Contains("exp-experienced"))
                    s += 3m;

                // 2.5. Ngân sách: gần midpoint được cộng điểm
                if (query.BudgetMin.HasValue && query.BudgetMax.HasValue)
                {
                    var mid = (query.BudgetMin.Value + query.BudgetMax.Value) / 2m;
                    var half = (query.BudgetMax.Value - query.BudgetMin.Value) / 2m;
                    if (half > 0)
                    {
                        var diff = Math.Abs(p.Price - mid);
                        var norm = 1m - Math.Min(diff / half, 1m); // 0..1
                        s += norm * 4m;
                    }
                }

                // 2.6. Thêm chút ưu tiên cho xe giá hợp lý (không quá cao)
                if (!query.BudgetMax.HasValue && p.Price <= 40_000_000) s += 1.5m;

                return s;
            }

            // 3. Lý do tóm tắt cho UI
            string BuildReason(Product p, string[] tagSlugs)
            {
                var rs = new List<string>();

                if (query.Purpose == "city" && tagSlugs.Contains("usage-city"))
                    rs.Add("phù hợp đi phố / đi làm hằng ngày");
                if (query.Purpose == "delivery" && tagSlugs.Contains("usage-delivery"))
                    rs.Add("hợp chạy đơn, giao hàng");
                if (query.Purpose == "touring" && tagSlugs.Contains("usage-touring"))
                    rs.Add("hợp đi xa / đi tour");

                if (tagSlugs.Contains("feature-fuel-saving"))
                    rs.Add("tiết kiệm xăng");
                if (tagSlugs.Contains("feature-lightweight"))
                    rs.Add("trọng lượng nhẹ, dễ dắt xe");
                if (tagSlugs.Contains("feature-low-seat"))
                    rs.Add("yên thấp, dễ chống chân");
                if (tagSlugs.Contains("feature-sporty"))
                    rs.Add("cảm giác lái thể thao");
                if (tagSlugs.Contains("feature-premium"))
                    rs.Add("ngoại hình sang, nhiều trang bị");
                if (tagSlugs.Contains("feature-cheap-maintenance"))
                    rs.Add("chi phí bảo dưỡng rẻ");

                if (!rs.Any())
                    rs.Add("cân đối tốt giữa nhu cầu và tầm giá bạn đưa ra");

                return string.Join(", ", rs) + ".";
            }

            // 4. Xếp hạng & map sang DTO
            var ranked = products
                .Select(p =>
                {
                    var slugs = p.ProductTags
                                 .Where(pt => pt.Tag != null && !string.IsNullOrEmpty(pt.Tag.Slug))
                                 .Select(pt => pt.Tag.Slug!)
                                 .ToArray();
                    return new
                    {
                        Product = p,
                        TagSlugs = slugs,
                        Score = Score(p)
                    };
                })
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Product.Price)
                .Take(5) // lấy top 5
                .ToList();

            var result = ranked.Select(x => new AiSuggestionItem
            {
                ProductId = x.Product.Id,
                Name = x.Product.Name,
                ImageUrl = x.Product.PrimaryImageUrl ?? x.Product.ImageUrl,
                Price = x.Product.Price,
                Brand = x.Product.Brand?.Name,
                Category = x.Product.Category?.Name,
                Reason = BuildReason(x.Product, x.TagSlugs)
            }).ToList();

            return result;
        }
        // Cho controller dùng: cùng logic với SuggestAsync nhưng trả về IReadOnlyList
        public async Task<IReadOnlyList<AiSuggestionItem>> GetSuggestionsAsync(
            AiParsedQuery query,
            CancellationToken ct = default)
        {
            var list = await SuggestAsync(query, ct);
            return list;
        }

    }
}
