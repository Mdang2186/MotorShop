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
    public class AiRecommendationService
    {
        private readonly ApplicationDbContext _db;

        public AiRecommendationService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IReadOnlyList<AiSuggestionItem>> GetSuggestionsAsync(
            AiParsedQuery query, string? userId = null, CancellationToken ct = default)
        {
            // 1. QUERY CƠ BẢN
            var q = _db.Products
                .AsNoTracking()
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.ProductTags).ThenInclude(pt => pt.Tag)
                .Where(p => p.IsActive && p.IsPublished);

            // 2. LỌC CỨNG (Hard Filters) - Bắt buộc phải thỏa mãn

            // Fix lỗi giá: Tuyệt đối không dùng logic cộng trừ biên độ ở đây nếu User đã nói rõ "Dưới/Trên"
            if (query.BudgetMin.HasValue)
                q = q.Where(p => p.Price >= query.BudgetMin.Value);

            if (query.BudgetMax.HasValue)
                q = q.Where(p => p.Price <= query.BudgetMax.Value);

            // Lọc theo Brand (nếu có)
            if (query.PreferredBrands.Any())
            {
                var brands = query.PreferredBrands.Select(b => b.ToLower()).ToList();
                q = q.Where(p => p.Brand != null && brands.Contains(p.Brand.Name.ToLower()));
            }

            var candidates = await q.ToListAsync(ct);

            // 3. IN-MEMORY FILTER (Lọc tên xe cụ thể)
            // Logic này xử lý sau khi lấy DB về để linh hoạt hơn
            var specificModelTag = query.PreferredTags.FirstOrDefault(t => t.StartsWith("model-"));
            if (!string.IsNullOrEmpty(specificModelTag))
            {
                string modelName = specificModelTag.Replace("model-", ""); // vd: "vision"
                // Chỉ giữ lại xe có tên chứa modelName
                candidates = candidates.Where(p => p.Name.ToLower().Contains(modelName)).ToList();
            }

            if (!candidates.Any()) return Array.Empty<AiSuggestionItem>();

            // 4. SCORING (CHẤM ĐIỂM ĐỂ SẮP XẾP)
            var ranked = candidates.Select(p =>
            {
                decimal score = 0;
                var tags = p.ProductTags?.Select(pt => pt.Tag?.Slug).ToList() ?? new List<string?>();

                // Cộng điểm nếu trùng tag
                foreach (var t in query.PreferredTags) if (tags.Contains(t)) score += 3;

                // Ưu tiên Tag mục đích
                if (query.Purpose == "city" && tags.Contains("usage-city")) score += 2;
                if (query.Purpose == "touring" && tags.Contains("usage-touring")) score += 2;

                // Cộng điểm nếu giá RẺ HƠN ngân sách tối đa một chút (Tiết kiệm cho khách)
                if (query.BudgetMax.HasValue && p.Price <= query.BudgetMax.Value * 0.9m) score += 1;

                return new { Product = p, Score = score };
            })
            .OrderByDescending(x => x.Score) // Điểm cao xếp trước
            .ThenBy(x => x.Product.Price)    // Giá rẻ xếp trước (nếu cùng điểm)
            .Take(6)
            .ToList();

            // 5. Map kết quả
            return ranked.Select(x => new AiSuggestionItem
            {
                ProductId = x.Product.Id,
                Name = x.Product.Name,
                ImageUrl = x.Product.PrimaryImageUrl ?? x.Product.ImageUrl,
                Price = x.Product.Price,
                Brand = x.Product.Brand?.Name,
                Category = x.Product.Category?.Name,
                Reason = BuildReason(x.Product, query)
            }).ToList();
        }

        private string BuildReason(Product p, AiParsedQuery query)
        {
            var tags = p.ProductTags?.Select(pt => pt.Tag?.Slug).ToList() ?? new List<string?>();

            if (p.Brand != null && query.PreferredBrands.Contains(p.Brand.Name.ToLower()))
                return $"Đúng hãng {p.Brand.Name} bạn yêu cầu.";

            if (tags.Contains("feature-fuel-saving")) return "Tiết kiệm nhiên liệu vượt trội.";
            if (tags.Contains("feature-lightweight")) return "Xe nhẹ, dễ điều khiển.";
            if (tags.Contains("feature-sporty")) return "Kiểu dáng thể thao, động cơ mạnh.";
            if (tags.Contains("usage-city")) return "Linh hoạt di chuyển trong phố.";

            return "Phù hợp với tiêu chí và ngân sách của bạn.";
        }
    }
}