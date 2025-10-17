using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MotorShop.Models;
using MotorShop.Utilities;
using MotorShop.ViewModels;

namespace MotorShop.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

        // --- HIỂN THỊ FORM ĐĂNG NHẬP ---
        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // --- XỬ LÝ ĐĂNG NHẬP ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    return LocalRedirect(returnUrl);
                }
                ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng.");
            }
            return View(model);
        }

        // --- HIỂN THỊ FORM ĐĂNG KÝ ---
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // --- XỬ LÝ ĐĂNG KÝ ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    Address = model.Address,
                    PhoneNumber = model.PhoneNumber
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Gán vai trò: người dùng đầu tiên luôn là Admin
                    if (!(await _roleManager.RoleExistsAsync(SD.Role_Admin)))
                    {
                        await _userManager.AddToRoleAsync(user, SD.Role_Admin);
                    }
                    else
                    {
                        await _userManager.AddToRoleAsync(user, SD.Role_User);
                    }

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Home");
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

        // --- XỬ LÝ ĐĂNG XUẤT ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // --- TRANG TRUY CẬP BỊ TỪ CHỐI ---
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}