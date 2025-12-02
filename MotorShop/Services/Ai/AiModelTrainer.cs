// File: Services/Ai/AiModelTrainer.cs
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Trainers;
using MotorShop.Data;
using MotorShop.Services; // MlTrainingService.ProductRating

namespace MotorShop.Services.Ai
{
    public class AiModelTrainer
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<AiModelTrainer> _logger;

        public AiModelTrainer(
            ApplicationDbContext db,
            ILogger<AiModelTrainer> logger)
        {
            _db = db;
            _logger = logger;
        }

        /// <summary>
        /// Train mô hình gợi ý sản phẩm từ lịch sử đơn hàng
        /// rồi lưu model ra file MlModels/product_recommender.zip.
        /// </summary>
        public async Task TrainProductRecommenderAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Bắt đầu train mô hình gợi ý sản phẩm từ CSDL...");

            // ========== 1. Chuẩn bị dữ liệu rating từ Orders + OrderItems ==========
            // TODO: CHỈNH property UserId cho đúng với entity Order của bạn.
            var ratingsQuery =
                from oi in _db.OrderItems    // DbSet<OrderItem>
                where oi.Order != null
                      && oi.Order.UserId != null      // <-- ĐỔI UserId nếu cần
                select new MlTrainingService.ProductRating
                {
                    UserId = oi.Order.UserId!,        // <-- ĐỔI UserId nếu cần
                    ProductId = oi.ProductId.ToString(),
                    Label = 1f                        // implicit feedback: đã mua = 1
                };

            var ratings = await ratingsQuery.ToListAsync(ct);
            if (!ratings.Any())
            {
                _logger.LogWarning("Không có dữ liệu đơn hàng để train recommender.");
                return;
            }

            _logger.LogInformation("Có {Count} dòng rating để train.", ratings.Count);

            // ========== 2. Dùng ML.NET train MatrixFactorization ==========
            var ml = new MLContext(seed: 123);

            var dataView = ml.Data.LoadFromEnumerable(ratings);

            var pipeline = ml.Transforms.Conversion.MapValueToKey(
                                outputColumnName: "userIdEncoded",
                                inputColumnName: nameof(MlTrainingService.ProductRating.UserId))
                .Append(ml.Transforms.Conversion.MapValueToKey(
                                outputColumnName: "productIdEncoded",
                                inputColumnName: nameof(MlTrainingService.ProductRating.ProductId)))
                .Append(ml.Recommendation().Trainers.MatrixFactorization(
                    new MatrixFactorizationTrainer.Options
                    {
                        LabelColumnName = nameof(MlTrainingService.ProductRating.Label),
                        MatrixColumnIndexColumnName = "userIdEncoded",
                        MatrixRowIndexColumnName = "productIdEncoded",
                        NumberOfIterations = 50,
                        ApproximationRank = 100
                    }));

            _logger.LogInformation("Bắt đầu train model ML.NET...");
            var model = pipeline.Fit(dataView);
            _logger.LogInformation("Train xong model.");

            // ========== 3. Lưu model ra file ==========
            var modelsFolder = Path.Combine("MlModels");
            if (!Directory.Exists(modelsFolder))
                Directory.CreateDirectory(modelsFolder);

            var modelPath = Path.Combine(modelsFolder, "product_recommender.zip");
            ml.Model.Save(model, dataView.Schema, modelPath);

            _logger.LogInformation("Đã lưu model recommender ra file {Path}", modelPath);
        }
    }
}
