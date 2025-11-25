using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MotorShop.Controllers
{
    // Bắt buộc đăng nhập để upload ảnh (tránh spam)
    [Authorize]
    public class ChatController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        // API Upload ảnh dành cho khách hàng
        [HttpPost]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "Vui lòng chọn ảnh." });

            try
            {
                // 1. Tạo tên file độc nhất
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);

                // 2. Đường dẫn lưu: wwwroot/images/chat
                var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "chat");

                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                var filePath = Path.Combine(folderPath, fileName);

                // 3. Lưu file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // 4. Trả về URL cho Client
                return Ok(new { url = $"/images/chat/{fileName}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Lỗi server: " + ex.Message });
            }
        }
    }
}