// File: Models/Ai/AiModelData.cs
using Microsoft.ML.Data;

namespace MotorShop.Models.Ai
{
    // Input: Dữ liệu lịch sử mua hàng
    public class ProductRating
    {
        [LoadColumn(0)] public string UserId { get; set; } = string.Empty;
        [LoadColumn(1)] public string ProductId { get; set; } = string.Empty; // Dùng String cho Key mapping
        [LoadColumn(2)] public float Label { get; set; } // 1 = Đã mua
    }

    // Output: Kết quả dự đoán
    public class ProductPrediction
    {
        public float Score { get; set; } // Điểm số gợi ý
        public float ProductId { get; set; }
    }
}