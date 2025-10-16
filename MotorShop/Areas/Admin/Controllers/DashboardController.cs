using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MotorShop.Areas.Admin.Controllers
{
    [Area("Admin")] // Chỉ định đây là controller thuộc Area "Admin"
    [Authorize(Roles = "Admin")] // Yêu cầu phải đăng nhập với vai trò "Admin"
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}