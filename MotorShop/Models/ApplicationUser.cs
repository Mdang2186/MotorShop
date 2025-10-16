using Microsoft.AspNetCore.Identity;

namespace MotorShop.Models
{
    // Mở rộng thông tin người dùng
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
        public string? Address { get; set; }
    }
}