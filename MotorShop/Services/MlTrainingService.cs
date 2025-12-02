using System;
using MotorShop.Data;

namespace MotorShop.Services
{
    /// <summary>
    /// Dịch vụ chuẩn bị dữ liệu training cho ML.NET.
    /// </summary>
    public class MlTrainingService
    {
        private readonly ApplicationDbContext _db;

        public MlTrainingService(ApplicationDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Mẫu dữ liệu Rating cho ML.NET:
        ///  - UserId: người mua
        ///  - ProductId: id sản phẩm
        ///  - Label: điểm đánh giá (ở đây dùng implicit feedback: đã mua = 1).
        /// </summary>
        public class ProductRating
        {
            public string UserId { get; set; } = string.Empty;
            public string ProductId { get; set; } = string.Empty;
            public float Label { get; set; }
        }

        /// <summary>
        /// TODO: Sau này bạn sẽ implement hàm này để train mô hình,
        /// nhưng tạm thời cứ giữ throw NotImplemented để không bị gọi nhầm.
        /// </summary>
        public void TrainProductRecommender()
        {
            throw new NotImplementedException(
                "TrainProductRecommender chưa được triển khai. " +
                "Hãy dùng AiModelTrainer.TrainProductRecommenderAsync để train bằng ML.NET."
            );
        }
    }
}
