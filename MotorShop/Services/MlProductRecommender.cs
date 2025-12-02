// File: Services/MlProductRecommender.cs
using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.ML;

namespace MotorShop.Services
{
    /// <summary>
    /// Service dùng ML.NET để dự đoán điểm ưa thích (Score) cho (UserId, ProductId).
    /// Nếu chưa có file model, service sẽ Fallback về "không dùng ML" (trả 0).
    /// </summary>
    public class MlProductRecommender
    {
        private readonly PredictionEngine<MlTrainingService.ProductRating, ProductScorePrediction>? _engine;
        private readonly ILogger<MlProductRecommender> _logger;
        private readonly bool _hasModel;

        public class ProductScorePrediction
        {
            public float Score { get; set; }
        }

        public MlProductRecommender(IWebHostEnvironment env, ILogger<MlProductRecommender> logger)
        {
            _logger = logger;

            var ml = new MLContext();

            // Đường dẫn: <ContentRoot>/MlModels/product_recommender.zip
            var modelsFolder = Path.Combine(env.ContentRootPath, "MlModels");
            var modelPath = Path.Combine(modelsFolder, "product_recommender.zip");

            if (!File.Exists(modelPath))
            {
                // KHÔNG ném exception nữa, chỉ log cảnh báo và tắt ML
                _logger.LogWarning(
                    "Không tìm thấy file model ML.NET tại {Path}. " +
                    "Hệ thống sẽ tạm thời bỏ qua điểm ML và chỉ dùng rule-based.",
                    modelPath);

                _hasModel = false;
                _engine = null;
                return;
            }

            try
            {
                using var fs = File.OpenRead(modelPath);
                var model = ml.Model.Load(fs, out var _);

                _engine = ml.Model.CreatePredictionEngine<MlTrainingService.ProductRating, ProductScorePrediction>(model);
                _hasModel = true;

                _logger.LogInformation("Đã load model ML.NET từ {Path}", modelPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Lỗi khi load model ML.NET từ {Path}. Sẽ tạm thời tắt ML và chỉ dùng rule-based.",
                    modelPath);

                _hasModel = false;
                _engine = null;
            }
        }

        /// <summary>
        /// Dự đoán điểm Score cho 1 cặp (userId, productId).
        /// Nếu chưa có model hoặc lỗi, trả 0f.
        /// </summary>
        public float Predict(string userId, string productId)
        {
            if (!_hasModel || _engine == null ||
                string.IsNullOrWhiteSpace(userId) ||
                string.IsNullOrWhiteSpace(productId))
            {
                return 0f;
            }

            try
            {
                var input = new MlTrainingService.ProductRating
                {
                    UserId = userId,
                    ProductId = productId,
                    Label = 0f // label không dùng khi predict
                };

                var result = _engine.Predict(input);
                return result?.Score ?? 0f;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Lỗi khi dự đoán điểm ML cho user {UserId}, product {ProductId}",
                    userId, productId);
                return 0f;
            }
        }
    }
}
