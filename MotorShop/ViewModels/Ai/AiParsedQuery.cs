namespace MotorShop.ViewModels.Ai
{
    /// <summary>
    /// Kết quả phân tích câu hỏi người dùng.
    /// </summary>
    public class AiParsedQuery
    {
        /// <summary>Chiều cao người lái (cm), nếu trích được.</summary>
        public int? HeightCm { get; set; }

        /// <summary>Ngân sách tối thiểu (VNĐ).</summary>
        public decimal? BudgetMin { get; set; }

        /// <summary>Ngân sách tối đa (VNĐ).</summary>
        public decimal? BudgetMax { get; set; }

        /// <summary>
        /// Mục đích: "city" (đi phố), "delivery" (chạy đơn),
        /// "touring" (đi tour xa) hoặc null.
        /// </summary>
        public string? Purpose { get; set; }

        /// <summary>Người mới đi xe hay đã có kinh nghiệm.</summary>
        public bool? IsBeginner { get; set; }

        /// <summary>Các tag ưu tiên (slug Tag), ví dụ: feature-fuel-saving, usage-city…</summary>
        public List<string> PreferredTags { get; set; } = new();

        /// <summary>Brand yêu thích: "honda", "yamaha"… (chữ thường).</summary>
        public List<string> PreferredBrands { get; set; } = new();
        /// <summary>
        /// Sinh câu mô tả ngắn gọn “AI hiểu gì về bạn” để hiển thị cho user.
        /// </summary>
        public string? BuildInsightSentence()
        {
            var parts = new List<string>();

            if (HeightCm is int h)
            {
                parts.Add($"chiều cao khoảng ~{h}cm");
            }

            if (BudgetMin is decimal bMin && BudgetMax is decimal bMax)
            {
                parts.Add($"ngân sách tầm {bMin / 1_000_000:0.#}–{bMax / 1_000_000:0.#} triệu");
            }
            else if (BudgetMin is decimal onlyMin)
            {
                parts.Add($"ngân sách khoảng {onlyMin / 1_000_000:0.#} triệu");
            }

            if (!string.IsNullOrWhiteSpace(Purpose))
            {
                parts.Add(Purpose switch
                {
                    "city" => "ưu tiên đi phố / đi làm hằng ngày",
                    "delivery" => "chạy đơn / giao hàng",
                    "touring" => "đi tour xa / đi phượt",
                    _ => null
                } ?? string.Empty);
            }

            if (IsBeginner == true)
            {
                parts.Add("mới đi xe, cần xe dễ lái");
            }

            if (PreferredBrands is { Count: > 0 })
            {
                var brands = string.Join(", ", PreferredBrands.Select(x => x.ToUpperInvariant()));
                parts.Add($"thích các hãng: {brands}");
            }

            if (PreferredTags.Contains("feature-fuel-saving"))
                parts.Add("ưu tiên tiết kiệm xăng");

            if (PreferredTags.Contains("feature-comfort"))
                parts.Add("ưu tiên êm ái, dễ ngồi");

            var clean = parts.Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
            if (!clean.Any()) return null;

            return "AI hiểu: " + string.Join(", ", clean) + ".";
        }
    }
}
