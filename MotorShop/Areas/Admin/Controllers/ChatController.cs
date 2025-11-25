using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MotorShop.Hubs;
using MotorShop.Models.Entities;
using MotorShop.Services;
using MotorShop.Utilities;

namespace MotorShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ChatController : Controller
    {
        private readonly IChatService _chatService;
        // HubContext chỉ dùng cho Fallback HTTP Post, logic chính nằm ở ChatHub
        private readonly IHubContext<ChatHub> _hubContext;

        public ChatController(IChatService chatService, IHubContext<ChatHub> hubContext)
        {
            _chatService = chatService;
            _hubContext = hubContext;
        }

        public async Task<IActionResult> Index(int? id)
        {
            var threads = await _chatService.GetOpenThreadsAsync();
            ChatThread? activeThread = null;

            if (threads != null && threads.Count > 0)
            {
                var activeId = id ?? threads.First().Id;
                activeThread = await _chatService.GetThreadWithMessagesAsync(activeId);
            }

            ViewBag.Threads = threads;
            return View(activeThread);
        }

        public IActionResult Details(int id)
        {
            return RedirectToAction(nameof(Index), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(int threadId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return RedirectToAction(nameof(Index), new { id = threadId });

            var staffId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var staffName = User.Identity?.Name ?? "Admin";

            if (string.IsNullOrEmpty(staffId)) return Forbid();

            var msg = await _chatService.AddMessageAsync(threadId, staffId, isFromStaff: true, content);

            // Gửi signalR thủ công nếu dùng form POST
            await _hubContext.Clients.Group($"thread-{threadId}").SendAsync("ReceiveMessage", new
            {
                threadId,
                senderId = staffId,
                sender = "[Admin] " + staffName,
                isFromStaff = true,
                content = msg.Content,
                sentAt = msg.SentAt.ToString("HH:mm dd/MM")
            });

            return RedirectToAction(nameof(Index), new { id = threadId });
        }
        // Thêm vào trong ChatController
        [HttpPost]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("Vui lòng chọn ảnh");

            // 1. Lưu ảnh vào wwwroot/images/chat
            var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/chat");

            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            var filePath = Path.Combine(path, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // 2. Trả về URL ảnh
            var url = $"/images/chat/{fileName}";
            return Ok(new { url });
        }
    }
}