// Controllers/AccountController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using MotorShop.Models;
using MotorShop.Utilities;
using MotorShop.ViewModels;
using MotorShop.ViewModels.Account;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;

namespace MotorShop.Controllers
{
    [AllowAnonymous]
    [AutoValidateAntiforgeryToken]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            IEmailSender emailSender,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _emailSender = emailSender;
            _logger = logger;
        }

        // =====================================================
        // REGISTER + EMAIL OTP
        // =====================================================
        [HttpGet]
        public IActionResult Register() => View(new RegisterViewModel());

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            // Thông báo “đã dùng” sớm, tránh message mặc định khó hiểu
            var existed = await _userManager.FindByEmailAsync(vm.Email);
            if (existed is not null)
            {
                ModelState.AddModelError(nameof(vm.Email), "Email đã được sử dụng.");
                return View(vm);
            }

            var user = new ApplicationUser
            {
                UserName = vm.Email,
                Email = vm.Email,
                FullName = vm.FullName,
                Address = vm.Address,
                PhoneNumber = vm.PhoneNumber,
                EmailConfirmed = false
            };

            var create = await _userManager.CreateAsync(user, vm.Password);
            if (!create.Succeeded)
            {
                foreach (var e in create.Errors) ModelState.AddModelError(string.Empty, e.Description);
                return View(vm);
            }

            // Role đảm bảo có (DbInitializer đã seed, nhưng thêm fallback)
            if (!await _roleManager.RoleExistsAsync(SD.Role_Admin))
                await _roleManager.CreateAsync(new IdentityRole(SD.Role_Admin));
            if (!await _roleManager.RoleExistsAsync(SD.Role_User))
                await _roleManager.CreateAsync(new IdentityRole(SD.Role_User));

            var hasAdmin = (await _userManager.GetUsersInRoleAsync(SD.Role_Admin)).Any();
            await _userManager.AddToRoleAsync(user, hasAdmin ? SD.Role_User : SD.Role_Admin);

            await SendOtpAsync(user, reason: "register");

            TempData[SD.Temp_Info] = "Chúng tôi đã gửi mã xác nhận 6 số tới email của bạn.";
            return RedirectToAction(nameof(VerifyEmailCode), new { email = user.Email });
        }

        // =====================================================
        // VERIFY EMAIL OTP
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> VerifyEmailCode(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return RedirectToAction(nameof(Login));

            var user = await _userManager.FindByEmailAsync(email);
            if (user is null) return RedirectToAction(nameof(Login));

            var remain = user.EmailOtpExpiryUtc is null
                ? 0
                : Math.Max(0, (int)Math.Ceiling((user.EmailOtpExpiryUtc.Value - DateTime.UtcNow).TotalSeconds));

            ViewBag.RemainingSeconds = remain;
            return View(new VerifyEmailCodeViewModel { Email = email });
        }

        [HttpPost]
        public async Task<IActionResult> VerifyEmailCode(VerifyEmailCodeViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var user = await _userManager.FindByEmailAsync(vm.Email);
            if (user is null)
            {
                ModelState.AddModelError(string.Empty, "Tài khoản không tồn tại.");
                return View(vm);
            }
            if (user.EmailConfirmed)
            {
                TempData[SD.Temp_Info] = "Email đã được xác nhận. Bạn có thể đăng nhập.";
                return RedirectToAction(nameof(Login));
            }
            if (user.EmailOtpExpiryUtc is null || user.EmailOtpExpiryUtc < DateTime.UtcNow)
            {
                ModelState.AddModelError(string.Empty, "Mã đã hết hạn. Vui lòng bấm “Gửi lại mã”.");
                ViewBag.RemainingSeconds = 0;
                return View(vm);
            }
            if (!string.Equals(user.EmailOtpCode, vm.Code, StringComparison.Ordinal))
            {
                ModelState.AddModelError(string.Empty, "Mã không đúng. Vui lòng kiểm tra và nhập lại.");
                ViewBag.RemainingSeconds = Math.Max(0, (int)Math.Ceiling((user.EmailOtpExpiryUtc.Value - DateTime.UtcNow).TotalSeconds));
                return View(vm);
            }

            user.EmailConfirmed = true;
            user.EmailOtpCode = null;
            user.EmailOtpExpiryUtc = null;
            await _userManager.UpdateAsync(user);

            await _signInManager.SignInAsync(user, isPersistent: false);
            TempData[SD.Temp_Success] = "Tài khoản đã được kích hoạt.";
            return RedirectToAction("Index", "Home");
        }

        // resend từ màn OTP (nút “Gửi lại mã” dùng cùng form với formaction)
        [HttpPost]
        public async Task<IActionResult> ResendEmailCode(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                TempData[SD.Temp_Error] = "Thiếu email.";
                return RedirectToAction(nameof(Login));
            }
            var user = await _userManager.FindByEmailAsync(email);
            if (user is null)
            {
                TempData[SD.Temp_Info] = "Nếu email tồn tại, chúng tôi đã gửi lại mã.";
                return RedirectToAction(nameof(Login));
            }
            if (user.EmailConfirmed)
            {
                TempData[SD.Temp_Info] = "Email đã được xác nhận. Bạn có thể đăng nhập.";
                return RedirectToAction(nameof(Login));
            }

            await SendOtpAsync(user, reason: "resend");
            TempData[SD.Temp_Success] = "Đã gửi lại mã xác nhận. Vui lòng kiểm tra email.";
            return RedirectToAction(nameof(VerifyEmailCode), new { email });
        }

        // =====================================================
        // LOGIN / LOGOUT
        // =====================================================
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (!string.IsNullOrEmpty(returnUrl))
                HttpContext.Session.SetString(SD.SessionReturnUrl, returnUrl);

            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var user = await _userManager.FindByEmailAsync(vm.Email);
            if (user is null)
            {
                ModelState.AddModelError(string.Empty, "Tài khoản hoặc mật khẩu không đúng.");
                return View(vm);
            }
            if (!user.EmailConfirmed)
            {
                TempData[SD.Temp_Warning] = "Email chưa xác nhận. Vui lòng nhập mã OTP.";
                return RedirectToAction(nameof(VerifyEmailCode), new { email = user.Email });
            }

            var result = await _signInManager.PasswordSignInAsync(user, vm.Password, vm.RememberMe, lockoutOnFailure: true);
            if (result.Succeeded)
            {
                var ret = vm.ReturnUrl ?? HttpContext.Session.GetString(SD.SessionReturnUrl);
                if (!string.IsNullOrEmpty(ret) && Url.IsLocalUrl(ret))
                    return Redirect(ret);
                return RedirectToAction("Index", "Home");
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "Tài khoản tạm bị khóa do đăng nhập sai nhiều lần.");
                return View(vm);
            }

            ModelState.AddModelError(string.Empty, "Tài khoản hoặc mật khẩu không đúng.");
            return View(vm);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            HttpContext.Session.Remove(SD.SessionReturnUrl);
            TempData[SD.Temp_Info] = "Bạn đã đăng xuất.";
            return RedirectToAction("Index", "Home");
        }

        // =====================================================
        // FORGOT / RESET PASSWORD (Base64Url để link an toàn)
        // =====================================================
        [HttpGet]
        public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var user = await _userManager.FindByEmailAsync(vm.Email);
            if (user is null || !(await _userManager.IsEmailConfirmedAsync(user)))
            {
                // Không tiết lộ sự tồn tại của email
                TempData[SD.Temp_Info] = "Nếu email tồn tại, chúng tôi đã gửi hướng dẫn đặt lại mật khẩu.";
                return RedirectToAction(nameof(Login));
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var callback = MakeAbsoluteUrl(nameof(ResetPassword), "Account", new { email = user.Email, code = encoded });

            try
            {
                var html = BuildPasswordResetEmailHtml(callback);
                await _emailSender.SendEmailAsync(user.Email!, "Đặt lại mật khẩu MotorShop", html);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gửi email đặt lại mật khẩu thất bại cho {Email}", user.Email);
            }

            TempData[SD.Temp_Success] = "Đã gửi email đặt lại mật khẩu (nếu email tồn tại).";
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult ResetPassword(string email, string code)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(code))
                return RedirectToAction(nameof(Login));

            return View(new ResetPasswordViewModel
            {
                Email = email,
                Token = code // giữ mã Base64Url để POST decode
            });
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var user = await _userManager.FindByEmailAsync(vm.Email);
            if (user is null)
            {
                TempData[SD.Temp_Info] = "Đã đặt lại mật khẩu.";
                return RedirectToAction(nameof(Login));
            }

            string decodedToken;
            try
            {
                decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(vm.Token));
            }
            catch
            {
                ModelState.AddModelError(string.Empty, "Liên kết không hợp lệ hoặc đã hết hạn.");
                return View(vm);
            }

            var result = await _userManager.ResetPasswordAsync(user, decodedToken, vm.Password);
            if (result.Succeeded)
            {
                TempData[SD.Temp_Success] = "Đặt lại mật khẩu thành công. Vui lòng đăng nhập.";
                return RedirectToAction(nameof(Login));
            }

            foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e.Description);
            return View(vm);
        }

        // =====================================================
        // HELPERS
        // =====================================================
        private async Task SendOtpAsync(ApplicationUser user, string reason)
        {
            var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString(); // 6 số
            user.EmailOtpCode = code;
            user.EmailOtpExpiryUtc = DateTime.UtcNow.AddMinutes(10);
            await _userManager.UpdateAsync(user);

            var html = CreateOtpEmailHtml(user, code, 10);
            try
            {
                await _emailSender.SendEmailAsync(user.Email!,
                    reason == "register" ? SD.EmailSubject_OtpRegister : SD.EmailSubject_OtpResend, html);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gửi OTP thất bại cho {Email}", user.Email);
            }
        }

        private string CreateOtpEmailHtml(ApplicationUser user, string code, int expiresMinutes)
        {
            var safeName = string.IsNullOrWhiteSpace(user.FullName)
                ? "bạn" : System.Net.WebUtility.HtmlEncode(user.FullName);

            var until = (user.EmailOtpExpiryUtc ?? DateTime.UtcNow.AddMinutes(expiresMinutes))
                        .ToLocalTime().ToString("HH:mm dd/MM/yyyy");

            var verifyUrl = HtmlEncoder.Default.Encode(
                MakeAbsoluteUrl(nameof(VerifyEmailCode), "Account", new { email = user.Email })
            );

            var boxes = string.Join("", code.Select(c =>
                $@"<div style=""width:44px;height:56px;border:1px solid #e5e7eb;border-radius:10px;display:grid;place-items:center;font-size:22px;font-weight:800;color:#111827"">{c}</div>"));

            return $@"
<!doctype html><html lang='vi'><meta charset='utf-8'>
<body style='margin:0;padding:0;background:#f5f7fb;font-family:-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#0f172a'>
  <div style='max-width:640px;margin:24px auto;background:#fff;border:1px solid #e5e7eb;border-radius:16px;padding:24px'>
    <h2 style='margin:0 0 8px'>Mã xác nhận tài khoản</h2>
    <p>Xin chào {safeName}, đây là mã xác nhận email của bạn.</p>
    <div style='display:flex;gap:10px;justify-content:center;margin:16px 0'>{boxes}</div>
    <p style='color:#64748b'>Mã hết hạn lúc <b>{until}</b> (trong {expiresMinutes} phút).</p>
    <p><a href='{verifyUrl}' style='display:inline-block;background:#2563eb;color:#fff;text-decoration:none;padding:10px 14px;border-radius:10px'>Mở trang nhập mã</a></p>
    <p style='color:#94a3b8;font-size:12px'>Nếu bạn không yêu cầu, hãy bỏ qua email này.</p>
  </div>
</body></html>";
        }

        private string BuildPasswordResetEmailHtml(string callbackUrl)
        {
            var url = HtmlEncoder.Default.Encode(callbackUrl);
            return $@"
<!doctype html><html lang='vi'><meta charset='utf-8'>
<body style='margin:0;padding:0;background:#f5f7fb;font-family:-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif'>
  <div style='max-width:640px;margin:24px auto;background:#fff;border:1px solid #e5e7eb;border-radius:16px;padding:24px'>
    <h2>Đặt lại mật khẩu</h2>
    <p>Nhấp vào nút bên dưới để đặt lại mật khẩu của bạn.</p>
    <p><a href='{url}' style='display:inline-block;background:#2563eb;color:#fff;text-decoration:none;padding:10px 14px;border-radius:10px'>Đặt lại mật khẩu</a></p>
    <p>Nếu nút không hoạt động, sao chép liên kết sau và dán vào trình duyệt:</p>
    <p><a href='{url}'>{url}</a></p>
  </div>
</body></html>";
        }

        private string MakeAbsoluteUrl(string action, string controller, object routeValues)
            => Url.Action(action, controller, routeValues, protocol: Request.Scheme) ?? "#";
    }
}
