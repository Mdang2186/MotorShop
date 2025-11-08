using System;
using System.Collections.Generic;

namespace MotorShop.ViewModels
{
    public class UserDetailsViewModel
    {
        public string UserId { get; set; } = "";
        public string UserName { get; set; } = "";
        public string Email { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public bool IsLockedOut { get; set; }
        public List<RoleCheckboxVM> Roles { get; set; } = new();
         
    }
}