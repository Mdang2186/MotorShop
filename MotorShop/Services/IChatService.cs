using System.Collections.Generic;
using System.Threading.Tasks;
using MotorShop.Models.Entities;

namespace MotorShop.Services
{
    public interface IChatService
    {
        Task<ChatThread> GetOrCreateCustomerThreadWithMessagesAsync(string customerId);
        Task<ChatThread?> GetThreadWithMessagesAsync(int threadId);
        Task<IReadOnlyList<ChatThread>> GetOpenThreadsAsync();
        Task<ChatMessage> AddMessageAsync(int threadId, string senderId, bool isFromStaff, string content);
    }
}
