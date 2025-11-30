namespace MotorShop.ViewModels.Ai
{
    /// <summary>DTO trả về cho UI AI gợi ý.</summary>
    public class AiSuggestionItem
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = "";
        public string? ImageUrl { get; set; }
        public decimal Price { get; set; }

        public string? Brand { get; set; }
        public string? Category { get; set; }

        /// <summary>Lý do tóm tắt vì sao AI gợi ý mẫu này.</summary>
        public string Reason { get; set; } = "";
    }
}
