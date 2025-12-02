namespace MotorShop.ViewModels.Ai
{
    public sealed class AiChatRequest
    {
        public string Message { get; set; } = string.Empty;

        // Các thông tin client (tùy, có thể bỏ qua)
        public int? ClientHeightCm { get; set; }
        public decimal? ClientBudgetMin { get; set; }
        public decimal? ClientBudgetMax { get; set; }
        public string? PurposeTag { get; set; }
        public string? UserAgent { get; set; }

        /// <summary>Id phiên chat AI, null = bắt đầu cuộc mới.</summary>
        public int? ConversationId { get; set; }
    }
}
