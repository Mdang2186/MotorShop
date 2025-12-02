using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace MotorShop.ViewModels.Ai
{
    /// <summary>
    /// Kết quả phân tích câu hỏi người dùng.
    /// </summary>
    public class AiParsedQuery
    {
        public int? HeightCm { get; set; }
        public decimal? BudgetMin { get; set; }
        public decimal? BudgetMax { get; set; }
        public string? Purpose { get; set; } // city, touring, delivery
        public bool? IsBeginner { get; set; }

        public List<string> PreferredTags { get; set; } = new();
        public List<string> PreferredBrands { get; set; } = new();

        /// <summary>
        /// Cộng dồn thông tin từ một query khác vào query hiện tại (Context Learning).
        /// Dữ liệu mới sẽ ghi đè hoặc bổ sung cho dữ liệu cũ.
        /// </summary>
        public void Merge(AiParsedQuery newQuery)
        {
            if (newQuery == null) return;

            // 1. Các trường đơn lẻ: Nếu tin nhắn mới có nhắc tới thì ghi đè
            if (newQuery.HeightCm.HasValue) this.HeightCm = newQuery.HeightCm;
            if (newQuery.BudgetMin.HasValue) this.BudgetMin = newQuery.BudgetMin;
            if (newQuery.BudgetMax.HasValue) this.BudgetMax = newQuery.BudgetMax;
            if (!string.IsNullOrEmpty(newQuery.Purpose)) this.Purpose = newQuery.Purpose;
            if (newQuery.IsBeginner.HasValue) this.IsBeginner = newQuery.IsBeginner;

            // 2. Danh sách (List): Cộng dồn và xóa trùng
            if (newQuery.PreferredBrands.Any())
            {
                foreach (var b in newQuery.PreferredBrands)
                {
                    if (!this.PreferredBrands.Contains(b))
                        this.PreferredBrands.Add(b);
                }
            }

            if (newQuery.PreferredTags.Any())
            {
                foreach (var t in newQuery.PreferredTags)
                {
                    if (!this.PreferredTags.Contains(t))
                        this.PreferredTags.Add(t);
                }
            }
        }

        /// <summary>
        /// Tạo câu tóm tắt để hiển thị "AI Hiểu: ..."
        /// </summary>
        public string? BuildInsightSentence()
        {
            var parts = new List<string>();
            var culture = CultureInfo.GetCultureInfo("vi-VN");

            // Brand
            if (PreferredBrands.Any())
                parts.Add($"hãng {string.Join(", ", PreferredBrands.Select(b => char.ToUpper(b[0]) + b.Substring(1)))}");

            // Model cụ thể (Vision, Airblade...)
            var models = PreferredTags
                .Where(t => t.StartsWith("model-"))
                .Select(t => char.ToUpper(t[6]) + t.Substring(7))
                .ToList();
            if (models.Any()) parts.Add($"dòng {string.Join(", ", models)}");

            // Ngân sách
            if (BudgetMin.HasValue && BudgetMax.HasValue)
                parts.Add($"giá {BudgetMin.Value:N0}-{BudgetMax.Value:N0}đ");
            else if (BudgetMax.HasValue)
                parts.Add($"giá dưới {BudgetMax.Value:N0}đ");
            else if (BudgetMin.HasValue)
                parts.Add($"giá trên {BudgetMin.Value:N0}đ");

            // Chiều cao
            if (HeightCm.HasValue) parts.Add($"cao ~{HeightCm}cm");

            // Mục đích
            if (Purpose == "city") parts.Add("đi phố");
            else if (Purpose == "touring") parts.Add("đi phượt");
            else if (Purpose == "delivery") parts.Add("chạy grab/ship");

            if (!parts.Any()) return null;
            return "AI hiểu: " + string.Join(", ", parts) + ".";
        }
    }
}