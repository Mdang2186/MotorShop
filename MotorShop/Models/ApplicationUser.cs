using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations;

namespace MotorShop.Models
{
    /// <summary>
    /// Mở rộng thông tin người dùng mặc định của Identity.
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        [StringLength(100)]
        public string? FullName { get; set; }

        [StringLength(255)]
        public string? Address { get; set; }

     
    }
}