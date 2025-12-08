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

            // 2. PHÂN LOẠI: XE hay PHỤ TÙNG?
            bool isPartSearch = query.PreferredTags.Any(t => t.StartsWith("part-") || t == "is-part-search");

            if (isPartSearch)
            {
                q = q.Where(p => p.Category.Name == "Phụ tùng & Linh kiện");
            }
            else
            {
                q = q.Where(p => p.Category == null || p.Category.Name != "Phụ tùng & Linh kiện");
            }

            // 3. LỌC GIÁ (Hard Filter)
            if (query.BudgetMin.HasValue) q = q.Where(p => p.Price >= query.BudgetMin.Value);
            if (query.BudgetMax.HasValue) q = q.Where(p => p.Price <= query.BudgetMax.Value);

            // Lấy dữ liệu thô về RAM
            var candidates = await q.ToListAsync(ct);

            // 4. CHUẨN BỊ DỮ LIỆU CHẤM ĐIỂM
            var modelTag = query.PreferredTags.FirstOrDefault(t => t.StartsWith("model-"));
            string? modelName = modelTag?.Replace("model-", "").ToLower();

            var targetPartTags = query.PreferredTags.Where(t => t.StartsWith("part-")).ToList();
            var preferredBrands = query.PreferredBrands.Select(b => b.ToLower()).ToList();

            // 5. CHẤM ĐIỂM & LỌC CHÍNH XÁC
            var ranked = candidates.Select(p =>
            {
                decimal score = 0;
                var pTags = p.ProductTags?.Select(pt => pt.Tag?.Slug).ToList() ?? new List<string?>();
                var pNameLower = p.Name.ToLower();
                var pBrandLower = p.Brand?.Name.ToLower() ?? "";

                if (isPartSearch)
                {
                    // --- LOGIC PHỤ TÙNG ---
                    bool isTypeMatch = false;

                    // A. Kiểm tra Loại phụ tùng (Quan trọng nhất)
                    if (targetPartTags.Any())
                    {
                        foreach (var targetTag in targetPartTags)
                        {
                            // Khớp Tag trong DB
                            if (pTags.Contains(targetTag))
                            {
                                score += 100;
                                isTypeMatch = true;
                            }
                            // Fallback: Khớp tên (nếu chưa gán tag)
                            else if (targetTag == "part-oil" && (pNameLower.Contains("nhớt") || pNameLower.Contains("dầu"))) { score += 80; isTypeMatch = true; }
                            else if (targetTag == "part-tire" && (pNameLower.Contains("lốp") || pNameLower.Contains("vỏ"))) { score += 80; isTypeMatch = true; }
                            else if (targetTag == "part-brake" && (pNameLower.Contains("phanh") || pNameLower.Contains("thắng") || pNameLower.Contains("bố"))) { score += 80; isTypeMatch = true; }
                            else if (targetTag == "part-filter" && (pNameLower.Contains("lọc"))) { score += 80; isTypeMatch = true; }
                            else if (targetTag == "part-chain" && (pNameLower.Contains("sên") || pNameLower.Contains("xích") || pNameLower.Contains("curoa"))) { score += 80; isTypeMatch = true; }
                            else if (targetTag == "part-battery" && (pNameLower.Contains("ắc quy") || pNameLower.Contains("bugi"))) { score += 80; isTypeMatch = true; }
                            else if (targetTag == "part-mirror" && (pNameLower.Contains("gương") || pNameLower.Contains("kính"))) { score += 80; isTypeMatch = true; }
                        }

                        // Nếu tìm loại cụ thể mà sản phẩm KHÔNG khớp -> Loại bỏ ngay
                        if (!isTypeMatch) return new { Product = p, Score = -1m };
                    }
                    else
                    {
                        // Tìm chung chung "phụ tùng honda"
                        score += 10;
                    }

                    // B. Kiểm tra Hãng & Model
                    if (preferredBrands.Any())
                    {
                        if (preferredBrands.Contains(pBrandLower)) score += 20; // Hãng SX
                        if (preferredBrands.Any(b => pNameLower.Contains(b))) score += 25; // Tên có chứa hãng
                    }
                    if (!string.IsNullOrEmpty(modelName) && pNameLower.Contains(modelName)) score += 30;
                }
                else
                {
                    // --- LOGIC XE MÁY ---
                    if (preferredBrands.Contains(pBrandLower)) score += 50;
                    if (!string.IsNullOrEmpty(modelName) && pNameLower.Contains(modelName)) score += 100;
                    foreach (var t in query.PreferredTags) if (pTags.Contains(t)) score += 5;
                }

                // Điểm cộng giá
                if (query.BudgetMax.HasValue && p.Price <= query.BudgetMax.Value * 0.9m) score += 2;

                return new { Product = p, Score = score };
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Product.Price)
            .Take(12)
            .ToList();

            // 6. XỬ LÝ FALLBACK (NẾU KHÔNG TÌM THẤY)
            if (!ranked.Any())
            {
                List<Product> fallbackList = new();

                // Ưu tiên tìm sản phẩm cùng Hãng (nếu user có nói hãng)
                if (preferredBrands.Any())
                {
                    fallbackList = candidates
                        .Where(p => p.Brand != null && preferredBrands.Contains(p.Brand.Name.ToLower()))
                        .OrderByDescending(p => p.StockQuantity)
                        .Take(8)
                        .ToList();
                }

                // Nếu vẫn không có, lấy sản phẩm Hot / Mới nhất trong danh mục đó
                if (!fallbackList.Any() && candidates.Any())
                {
                    fallbackList = candidates
                        .OrderByDescending(p => p.Id) // Hoặc ViewCount nếu có
                        .Take(8)
                        .ToList();
                }

                // Gán Score = -1 để đánh dấu
                ranked = fallbackList.Select(p => new { Product = p, Score = -1m }).ToList();
            }

            // 7. MAP KẾT QUẢ
            return ranked.Select(x => new AiSuggestionItem
            {
                ProductId = x.Product.Id,
                Name = x.Product.Name,
                ImageUrl = x.Product.PrimaryImageUrl ?? x.Product.ImageUrl,
                Price = x.Product.Price,
                Brand = x.Product.Brand?.Name,
                Category = x.Product.Category?.Name,
                // Nếu Score = -1 -> Đổi lý do
                Reason = x.Score == -1
                         ? "Sản phẩm phổ biến có thể bạn quan tâm."
                         : BuildReason(x.Product, query, isPartSearch)
            }).ToList();
        }

        private string BuildReason(Product p, AiParsedQuery query, bool isPartSearch)
        {
            var tags = p.ProductTags?.Select(pt => pt.Tag?.Slug).ToList() ?? new List<string?>();
            var pNameLower = p.Name.ToLower();

            if (isPartSearch)
            {
                if (pNameLower.Contains("vinfast") || pNameLower.Contains("xe điện")) return "Phụ tùng chuyên dụng cho xe điện.";
                if (tags.Contains("part-oil")) return "Dầu nhớt chính hãng, bảo vệ động cơ.";
                if (tags.Contains("part-tire")) return "Lốp bám đường tốt, an toàn.";
                if (tags.Contains("part-brake")) return "Hệ thống phanh hiệu suất cao.";

                var modelTag = query.PreferredTags.FirstOrDefault(t => t.StartsWith("model-"));
                string modelName = modelTag?.Replace("model-", "") ?? "";
                if (!string.IsNullOrEmpty(modelName) && pNameLower.Contains(modelName))
                    return $"Tương thích với xe {char.ToUpper(modelName[0]) + modelName.Substring(1)}.";

                return "Phụ tùng phù hợp nhu cầu.";
            }

            if (p.Brand != null && query.PreferredBrands.Contains(p.Brand.Name.ToLower())) return $"Đúng hãng {p.Brand.Name}.";
            return "Sản phẩm phù hợp với tiêu chí của bạn.";
        }
    }
}