using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MotorShop.Models;
using MotorShop.ViewModels.Admin;
using MotorShop.Utilities;
using System.Threading.Tasks;

namespace MotorShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)] // nếu bạn có role Manager thì thêm vào: "Admin,Manager"
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // ========== THÔNG TIN TÀI KHOẢN ==========
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var vm = new AdminProfileViewModel
            {
                Email = user.Email ?? "",
                FullName = user.FullName ?? "",
                PhoneNumber = user.PhoneNumber,
                Address = user.Address
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(AdminProfileViewModel vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            user.FullName = vm.FullName.Trim();
            user.PhoneNumber = vm.PhoneNumber?.Trim();
            user.Address = vm.Address?.Trim();

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var err in result.Errors)
                    ModelState.AddModelError(string.Empty, err.Description);
                return View(vm);
            }

            TempData[SD.Temp_Success] = "Cập nhật thông tin tài khoản thành công.";
            return RedirectToAction(nameof(Profile));
        }

        // ========== ĐỔI MẬT KHẨU ==========
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View(new AdminChangePasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(AdminChangePasswordViewModel vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var result = await _userManager.ChangePasswordAsync(user, vm.OldPassword, vm.NewPassword);
            if (!result.Succeeded)
            {
                foreach (var err in result.Errors)
                    ModelState.AddModelError(string.Empty, err.Description);
                return View(vm);
            }

            // Refresh lại cookie đăng nhập
            await _signInManager.RefreshSignInAsync(user);

            TempData[SD.Temp_Success] = "Đổi mật khẩu thành công.";
            return RedirectToAction(nameof(ChangePassword));
        }
    }
}
