using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using MotorShop.ViewModels.Ai;

namespace MotorShop.Services.Ai
{
    /// <summary>
    /// Phân tích câu hỏi tiếng Việt tự do thành các thông số (chiều cao, ngân sách, mục đích...).
    /// Dùng rule-based, không phụ thuộc model hay DB.
    /// </summary>
    public class AiQueryParser
    {
        public AiParsedQuery Parse(string? message)
        {
            var q = new AiParsedQuery();
            if (string.IsNullOrWhiteSpace(message)) return q;

            var text = message.ToLowerInvariant();

            // ========== 1. Chiều cao: "1m6", "1m60", "cao 160", "160cm" ==========
            var heightMatch = Regex.Match(text, @"1m\s*([0-9]{1,2})");
            if (!heightMatch.Success)
                heightMatch = Regex.Match(text, @"([\d]{3})\s*cm");

            if (heightMatch.Success && int.TryParse(heightMatch.Groups[1].Value, out var h))
            {
                // "1m6" => 160
                if (h < 50) h *= 10;
                q.HeightCm = h;
            }

            // ========== 2. Ngân sách: "40-55tr", "40 đến 55 triệu", "khoảng 45 triệu" ==========
            var rangeMatch = Regex.Match(text, @"(\d{2,3})\s*[-–đếnto]+\s*(\d{2,3})\s*t");
            if (rangeMatch.Success &&
                decimal.TryParse(rangeMatch.Groups[1].Value, out var fromTr) &&
                decimal.TryParse(rangeMatch.Groups[2].Value, out var toTr))
            {
                q.BudgetMin = fromTr * 1_000_000;
                q.BudgetMax = toTr * 1_000_000;
            }
            else
            {
                var singleMatch = Regex.Match(text, @"(\d{2,3})\s*(triệu|tr\b)");
                if (singleMatch.Success &&
                    decimal.TryParse(singleMatch.Groups[1].Value, out var midTr))
                {
                    q.BudgetMin = (midTr - 5) * 1_000_000;
                    q.BudgetMax = (midTr + 5) * 1_000_000;
                }
            }

            // ========== 3. Mục đích ==========
            if (text.Contains("đi làm") || text.Contains("đi học") || text.Contains("đi phố"))
                q.Purpose = "city";
            else if (text.Contains("chạy đơn") || text.Contains("shipper") || text.Contains("giao hàng"))
                q.Purpose = "delivery";
            else if (text.Contains("phượt") || text.Contains("đi tour") || text.Contains("đi xa"))
                q.Purpose = "touring";

            // ========== 4. Kinh nghiệm ==========
            if (text.Contains("mới đi xe") || text.Contains("mới tập") ||
                text.Contains("chưa có kinh nghiệm") || text.Contains("tay lái yếu"))
                q.IsBeginner = true;

            if (text.Contains("thích mạnh") || text.Contains("thích bốc") ||
                text.Contains("thích sport") || text.Contains("thích thể thao"))
                q.IsBeginner = false;

            // ========== 5. Tag theo keyword ==========
            void AddTagIf(bool cond, string slug)
            {
                if (cond && !q.PreferredTags.Contains(slug))
                    q.PreferredTags.Add(slug);
            }

            AddTagIf(text.Contains("nhẹ") || text.Contains("dễ dắt") || text.Contains("dễ chống chân"),
                     "feature-lightweight");
            AddTagIf(text.Contains("tiết kiệm xăng") || text.Contains("ăn ít") || text.Contains("ít hao xăng"),
                     "feature-fuel-saving");
            AddTagIf(text.Contains("thể thao") || text.Contains("sport") || text.Contains("bốc"),
                     "feature-sporty");
            AddTagIf(text.Contains("sang") || text.Contains("cao cấp") || text.Contains("xịn"),
                     "feature-premium");

            // ========= 6. Brand yêu thích =========
            if (text.Contains("honda")) q.PreferredBrands.Add("honda");
            if (text.Contains("yamaha")) q.PreferredBrands.Add("yamaha");
            if (text.Contains("suzuki")) q.PreferredBrands.Add("suzuki");
            if (text.Contains("vespa") || text.Contains("piaggio"))
                q.PreferredBrands.Add("vespa");
            if (text.Contains("vinfast")) q.PreferredBrands.Add("vinfast");
            if (text.Contains("ducati")) q.PreferredBrands.Add("ducati");
            if (text.Contains("bmw")) q.PreferredBrands.Add("bmw");

            // ========= 7. Gán tag chiều cao từ HeightCm (dùng tag height-*) =========
            if (q.HeightCm.HasValue)
            {
                var hcm = q.HeightCm.Value;
                if (hcm <= 155) q.PreferredTags.Add("height-short");
                else if (hcm <= 165) q.PreferredTags.Add("height-155-165");
                else if (hcm <= 175) q.PreferredTags.Add("height-165-175");
                else q.PreferredTags.Add("height-tall");
            }

            // ========= 8. Gán tag usage theo Purpose =========
            if (q.Purpose == "city") q.PreferredTags.Add("usage-city");
            else if (q.Purpose == "delivery") q.PreferredTags.Add("usage-delivery");
            else if (q.Purpose == "touring") q.PreferredTags.Add("usage-touring");

            // Beginner / experienced
            if (q.IsBeginner == true) q.PreferredTags.Add("exp-beginner");
            if (q.IsBeginner == false) q.PreferredTags.Add("exp-experienced");

            return q;
        }
    }
}
