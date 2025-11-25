using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MotorShop.Models.ViewModels;
using MotorShop.Services;
using MotorShop.Utilities; // thêm

namespace MotorShop.ViewComponents
{
    public class ChatWidgetViewComponent : ViewComponent
    {
        private readonly IChatService _chatService;

        public ChatWidgetViewComponent(IChatService chatService)
        {
            _chatService = chatService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // 1. Bắt buộc đăng nhập
            if (!User.Identity?.IsAuthenticated ?? true)
                return Content(string.Empty);

            // 2. Không hiển thị cho Admin
            if (User.IsInRole(SD.Role_Admin))
                return Content(string.Empty);

            var principal = (ClaimsPrincipal)User;
            var customerId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var customerName = User.Identity?.Name ?? "Bạn";

            if (string.IsNullOrEmpty(customerId))
                return Content(string.Empty);

            var thread = await _chatService.GetOrCreateCustomerThreadWithMessagesAsync(customerId);

            var vm = new ChatWidgetViewModel
            {
                ThreadId = thread.Id,
                Messages = thread.Messages
                    .OrderBy(m => m.SentAt)
                    .Select(m => new ChatWidgetMessageViewModel
                    {
                        // Truyền thẳng giá trị IsFromStaff từ ChatMessage vào ViewModel
                        IsFromStaff = m.IsFromStaff,

                        SenderDisplayName = m.IsFromStaff
                            ? (m.Sender?.UserName ?? "Hỗ trợ MotorShop")
                            : (m.Sender?.UserName ?? customerName),
                        Content = m.Content,
                        SentAt = m.SentAt,
                        SentAtDisplay = m.SentAt.ToString("HH:mm dd/MM")
                    })
                    .ToList()
            };

            return View(vm);
        }
    }
}