using System.Text;
using System.Text.RegularExpressions;

namespace MotorShop.Utilities
{
    /// <summary>
    /// Cung cấp các phương thức tiện ích để tạo chuỗi URL thân thiện (slug).
    /// </summary>
    public static class SlugHelper
    {
        /// <summary>
        /// Chuyển đổi một chuỗi văn bản thành dạng slug (viết thường, không dấu, gạch nối).
        /// </summary>
        /// <param name="phrase">Chuỗi cần chuyển đổi.</param>
        /// <returns>Chuỗi đã được chuyển thành slug.</returns>
        public static string GenerateSlug(string phrase)
        {
            if (string.IsNullOrEmpty(phrase)) return "";

            // Chuyển thành chữ thường
            string str = phrase.ToLowerInvariant();

            // Bỏ dấu tiếng Việt
            str = Regex.Replace(str, @"[àáạảãâầấậẩẫăằắặẳẵ]", "a");
            str = Regex.Replace(str, @"[èéẹẻẽêềếệểễ]", "e");
            str = Regex.Replace(str, @"[ìíịỉĩ]", "i");
            str = Regex.Replace(str, @"[òóọỏõôồốộổỗơờớợởỡ]", "o");
            str = Regex.Replace(str, @"[ùúụủũưừứựửữ]", "u");
            str = Regex.Replace(str, @"[ỳýỵỷỹ]", "y");
            str = Regex.Replace(str, @"[đ]", "d");

            // Xóa các ký tự đặc biệt không mong muốn
            str = Regex.Replace(str, @"[^a-z0-9\s-]", "");
            // Thay thế các khoảng trắng bằng một dấu gạch ngang duy nhất
            str = Regex.Replace(str, @"\s+", "-").Trim();
            // Xóa các dấu gạch ngang liền kề (nếu có)
            str = Regex.Replace(str, @"-+", "-");

            return str;
        }
    }
}