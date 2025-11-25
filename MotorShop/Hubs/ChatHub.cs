using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MotorShop.Services;
using MotorShop.Utilities;

namespace MotorShop.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IChatService _chatService;

        public ChatHub(IChatService chatService)
        {
            _chatService = chatService;
        }

        public async Task JoinThreadGroup(int threadId)
        {
            if (threadId > 0)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"thread-{threadId}");
            }
        }

        // --- KHÁCH GỬI TIN ---
        public async Task SendCustomerMessage(int threadId, string content)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(content)) return;

                var user = Context.User;
                var customerId = user?.FindFirstValue(ClaimTypes.NameIdentifier);
                var customerName = user?.Identity?.Name ?? "Bạn";

                if (string.IsNullOrEmpty(customerId))
                {
                    await Clients.Caller.SendAsync("Error", "Vui lòng đăng nhập.");
                    return;
                }

                bool isNewThread = false;
                if (threadId <= 0)
                {
                    var thread = await _chatService.GetOrCreateCustomerThreadWithMessagesAsync(customerId);
                    threadId = thread.Id;
                    isNewThread = true;
                }

                await Groups.AddToGroupAsync(Context.ConnectionId, $"thread-{threadId}");

                if (isNewThread) await Clients.Caller.SendAsync("UpdateThreadId", threadId);

                var msg = await _chatService.AddMessageAsync(threadId, customerId, false, content);

                // SỬA: Gửi SentAt là DateTime gốc (SignalR sẽ tự chuyển thành ISO string)
                var payload = new
                {
                    threadId,
                    sender = customerName,
                    content = msg.Content,
                    sentAt = msg.SentAt,
                    isFromStaff = false
                };
                await Clients.Group($"thread-{threadId}").SendAsync("ReceiveMessage", payload);

                // Auto-Reply Bot
                var currentThread = await _chatService.GetThreadWithMessagesAsync(threadId);
                if (currentThread != null && currentThread.Messages.Count <= 1)
                {
                    await Task.Delay(1500);
                    string botContent = "Chào bạn! Admin sẽ phản hồi sớm nhất có thể ạ!";
                    var botMsg = await _chatService.AddMessageAsync(threadId, null, true, botContent);

                    var botPayload = new
                    {
                        threadId,
                        sender = "Hỗ trợ MotorShop",
                        content = botContent,
                        sentAt = botMsg.SentAt, // SỬA
                        isFromStaff = true
                    };
                    await Clients.Group($"thread-{threadId}").SendAsync("ReceiveMessage", botPayload);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                await Clients.Caller.SendAsync("Error", "Lỗi gửi tin.");
            }
        }

        // --- ADMIN GỬI TIN ---
        [Authorize(Roles = "Admin")]
        public async Task SendStaffMessage(int threadId, string content)
        {
            if (threadId <= 0 || string.IsNullOrWhiteSpace(content)) return;

            try
            {
                var staffId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
                var staffName = Context.User?.Identity?.Name ?? "Admin";

                await Groups.AddToGroupAsync(Context.ConnectionId, $"thread-{threadId}");

                var msg = await _chatService.AddMessageAsync(threadId, staffId, true, content);

                // SỬA: Gửi DateTime gốc
                var payload = new
                {
                    threadId,
                    sender = staffName,
                    content = msg.Content,
                    sentAt = msg.SentAt,
                    isFromStaff = true
                };

                await Clients.Group($"thread-{threadId}").SendAsync("ReceiveMessage", payload);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error Admin: " + ex.Message);
                await Clients.Caller.SendAsync("Error", "Lỗi gửi tin admin.");
            }
        }
    }
}