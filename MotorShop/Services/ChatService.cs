using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MotorShop.Data;
using MotorShop.Models.Entities;

namespace MotorShop.Services
{
    public class ChatService : IChatService
    {
        private readonly ApplicationDbContext _db;

        public ChatService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<ChatThread> GetOrCreateCustomerThreadWithMessagesAsync(string customerId)
        {
            // 1. Tìm thread chưa đóng
            var thread = await _db.ChatThreads
                .Include(t => t.Messages)
                    .ThenInclude(m => m.Sender)
                .FirstOrDefaultAsync(t => t.CustomerId == customerId && !t.IsClosed);

            if (thread != null)
                return thread;

            // 2. Nếu chưa có, tạo mới
            thread = new ChatThread
            {
                CustomerId = customerId,
                CreatedAt = DateTime.UtcNow,
                IsClosed = false,
                Messages = new List<ChatMessage>() // Khởi tạo list rỗng để tránh null
            };

            _db.ChatThreads.Add(thread);
            await _db.SaveChangesAsync(); // Lưu để sinh ID

            return thread;
        }

        public async Task<ChatThread?> GetThreadWithMessagesAsync(int threadId)
        {
            return await _db.ChatThreads
                .Include(t => t.Customer)
                .Include(t => t.Messages)
                    .ThenInclude(m => m.Sender)
                .FirstOrDefaultAsync(t => t.Id == threadId);
        }

        public async Task<IReadOnlyList<ChatThread>> GetOpenThreadsAsync()
        {
            return await _db.ChatThreads
                .Include(t => t.Customer)
                .Include(t => t.Messages)
                .Where(t => !t.IsClosed) // Lấy tất cả thread mở, kể cả chưa có tin nhắn (để khách không bị tạo trùng)
                .OrderByDescending(t => t.LastMessageAt ?? t.CreatedAt)
                .ToListAsync();
        }

        public async Task<ChatMessage> AddMessageAsync(int threadId, string? senderId, bool isFromStaff, string content)
        {
            var thread = await _db.ChatThreads.FirstOrDefaultAsync(t => t.Id == threadId);
            if (thread == null)
                throw new InvalidOperationException("ChatThread không tồn tại.");

            var msg = new ChatMessage
            {
                ThreadId = threadId,
                SenderId = senderId, // Đảm bảo SenderId khớp với Id trong bảng AspNetUsers
                Content = content,
                IsFromStaff = isFromStaff,
                SentAt = DateTime.UtcNow
            };

            // Cập nhật thông tin thread
            thread.LastMessageAt = msg.SentAt;
            thread.LastMessagePreview = content.Length > 50 ? content.Substring(0, 50) + "..." : content;

            if (isFromStaff && string.IsNullOrEmpty(thread.StaffId) && !string.IsNullOrEmpty(senderId))
                thread.StaffId = senderId;

            _db.ChatMessages.Add(msg);
            _db.ChatThreads.Update(thread);

            await _db.SaveChangesAsync();

            // Load thông tin Sender để trả về cho SignalR hiển thị avatar/tên (chỉ nếu có Sender)
            if (!string.IsNullOrEmpty(msg.SenderId))
            {
                await _db.Entry(msg).Reference(m => m.Sender).LoadAsync();
            }

            return msg;
        }
    }
}