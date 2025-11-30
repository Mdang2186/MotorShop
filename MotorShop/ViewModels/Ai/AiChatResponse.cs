using System.Collections.Generic;

namespace MotorShop.ViewModels.Ai
{
    public sealed class AiChatResponse
    {
        public int ConversationId { get; set; }
        public string? Insight { get; set; }
        public IReadOnlyList<AiSuggestionItem> Items { get; set; } = new List<AiSuggestionItem>();
    }
}
