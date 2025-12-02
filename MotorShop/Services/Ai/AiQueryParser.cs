using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using MotorShop.ViewModels.Ai;

namespace MotorShop.Services.Ai
{
    public class AiQueryParser
    {
        // Regex bắt giá tiền: (prefix)? (số) (đơn vị)
        // Hỗ trợ số thập phân: 25.5, 25,5
        private static readonly Regex PriceRegex =
            new(@"(dưới|tối đa|không quá|trên|tối thiểu|tầm|khoảng|~|<|>|giá dưới|giá trên)?\s*(\d{1,3}(?:[.,]\d)?)\s*(tr|triệu|t\b)",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Regex bắt chiều cao: 1m65, 165cm
        private static readonly Regex HeightRegex =
            new(@"(cao|height)?\s*(?:1m(?<h>\d{1,2})|(?<h>\d{2,3})\s*cm)",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Danh sách tên xe để bắt dính (Ưu tiên cao nhất)
        private static readonly Dictionary<string, string> SpecificModels = new()
        {
            { "vision", "vision" }, { "airblade", "air blade" }, { "ab", "air blade" },
            { "lead", "lead" }, { "sh", "sh" }, { "mode", "sh mode" },
            { "wave", "wave" }, { "alpha", "wave alpha" }, { "rsx", "wave rsx" },
            { "future", "future" }, { "winner", "winner" }, { "winnerx", "winner x" },
            { "exciter", "exciter" }, { "ex", "exciter" },
            { "grande", "grande" }, { "janus", "janus" }, { "nvx", "nvx" }, { "sirius", "sirius" },
            { "vespa", "vespa" }, { "liberty", "liberty" }, { "medley", "medley" },
            { "klara", "klara" }, { "feliz", "feliz" }, { "vento", "vento" }, { "evo", "evo" }
        };

        public AiParsedQuery Parse(string? text)
        {
            var q = new AiParsedQuery();
            if (string.IsNullOrWhiteSpace(text)) return q;

            text = text.Trim();
            var lower = text.ToLowerInvariant();

            // 1. Bắt tên xe cụ thể (Specific Model)
            foreach (var kvp in SpecificModels)
            {
                if (lower.Contains(kvp.Key))
                {
                    q.PreferredTags.Add($"model-{kvp.Value}");
                }
            }

            // 2. Bắt Hãng xe (Brand)
            DetectBrands(lower, q);

            // 3. Bắt Ngân sách (Logic chặt chẽ)
            DetectBudget(lower, q);

            // 4. Các thông số khác
            DetectHeight(lower, q);
            DetectPurpose(lower, q);
            DetectExperience(lower, q);
            DetectFeatureTags(lower, q);

            return q;
        }

        // --- HELPERS ---

        private static void DetectBrands(string lower, AiParsedQuery q)
        {
            var brands = new[] { "honda", "yamaha", "vinfast", "suzuki", "vespa", "piaggio", "sym", "ducati", "kawasaki", "bmw" };
            foreach (var b in brands)
            {
                if (lower.Contains(b)) q.PreferredBrands.Add(b);
            }
        }

        private static void DetectBudget(string lower, AiParsedQuery q)
        {
            // Case khoảng giá "20-30tr"
            var rangeMatch = Regex.Match(lower, @"(\d{1,3})\s*[-–đếnto]+\s*(\d{1,3})\s*(tr|triệu)");
            if (rangeMatch.Success &&
                decimal.TryParse(rangeMatch.Groups[1].Value, out var min) &&
                decimal.TryParse(rangeMatch.Groups[2].Value, out var max))
            {
                q.BudgetMin = min * 1_000_000m;
                q.BudgetMax = max * 1_000_000m;
                return;
            }

            // Case giá đơn "dưới 30tr"
            var match = PriceRegex.Match(lower);
            if (match.Success)
            {
                string prefix = match.Groups[1].Value.ToLower();
                string numStr = match.Groups[2].Value.Replace(',', '.');

                if (decimal.TryParse(numStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal amount))
                {
                    decimal money = amount * 1_000_000m;

                    if (prefix.Contains("dưới") || prefix.Contains("tối đa") || prefix.Contains("không quá") || prefix.Contains("<"))
                    {
                        q.BudgetMax = money; // Chốt Max
                    }
                    else if (prefix.Contains("trên") || prefix.Contains("tối thiểu") || prefix.Contains("hơn") || prefix.Contains(">"))
                    {
                        q.BudgetMin = money; // Chốt Min
                    }
                    else
                    {
                        // "Tầm 30tr" -> Tìm quanh biên độ 10%
                        q.BudgetMin = money * 0.9m;
                        q.BudgetMax = money * 1.1m;
                    }
                }
            }
        }

        private static void DetectHeight(string lower, AiParsedQuery q)
        {
            var m = HeightRegex.Match(lower);
            if (m.Success && int.TryParse(m.Groups["h"].Value, out var h))
            {
                if (h < 100) h *= 10; if (h < 100) h += 100; // 1m6 -> 160
                if (h > 200) h = 170; // Filter nhiễu
                q.HeightCm = h;
            }
            else if (lower.Contains("thấp") || lower.Contains("lùn")) q.HeightCm = 150;
            else if (lower.Contains("cao")) q.HeightCm = 175;
        }

        private static void DetectPurpose(string lower, AiParsedQuery q)
        {
            if (lower.Contains("đi làm") || lower.Contains("đi học") || lower.Contains("phố") || lower.Contains("văn phòng"))
                q.Purpose = "city";
            if (lower.Contains("shipper") || lower.Contains("ship") || lower.Contains("grab") || lower.Contains("chở hàng"))
                q.Purpose = "delivery";
            if (lower.Contains("phượt") || lower.Contains("tour") || lower.Contains("đi xa"))
                q.Purpose = "touring";
        }

        private static void DetectExperience(string lower, AiParsedQuery q)
        {
            if (lower.Contains("mới") || lower.Contains("chưa quen")) q.IsBeginner = true;
            if (lower.Contains("lâu năm") || lower.Contains("cứng")) q.IsBeginner = false;
        }

        private static void DetectFeatureTags(string lower, AiParsedQuery q)
        {
            void Add(string s) { if (!q.PreferredTags.Contains(s)) q.PreferredTags.Add(s); }

            if (lower.Contains("tiết kiệm") || lower.Contains("lợi xăng")) Add("feature-fuel-saving");
            if (lower.Contains("nhẹ") || lower.Contains("nữ")) { Add("feature-lightweight"); Add("feature-low-seat"); }
            if (lower.Contains("êm") || lower.Contains("thoải mái")) Add("feature-comfort");
            if (lower.Contains("thể thao") || lower.Contains("mạnh") || lower.Contains("ngầu")) Add("feature-sporty");
            if (lower.Contains("sang") || lower.Contains("đẹp")) Add("feature-premium");
            if (lower.Contains("cốp rộng")) Add("feature-storage");

            if (q.HeightCm.HasValue)
            {
                if (q.HeightCm <= 155) Add("height-short");
                else if (q.HeightCm >= 175) Add("height-tall");
            }
            if (q.Purpose == "city") Add("usage-city");
            if (q.Purpose == "touring") Add("usage-touring");
        }
    }
}