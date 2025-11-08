using System;
using System.Collections.Generic;

namespace MotorShop.ViewModels
{
    // ViewModel cho trang Manage User
    public class ManageUserViewModel
    {
        public string UserId { get; set; } = "";
        public string UserName { get; set; } = "";
        public string Email { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public List<RoleCheckboxVM> Roles { get; set; } = new();
    }

    // ViewModel cho từng checkbox quyền
    public class RoleCheckboxVM
    {
        public string RoleId { get; set; } = "";
        public string RoleName { get; set; } = "";
        public bool IsSelected { get; set; }
    }
}