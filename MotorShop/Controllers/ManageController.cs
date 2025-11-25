// File: Controllers/ManageController.cs
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using MotorShop.Data;
using MotorShop.Models;
using MotorShop.Utilities;
using MotorShop.ViewModels;

namespace MotorShop.Controllers
{
    [Authorize]
    public class ManageController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly IWebHostEnvironment _env;
        private readonly ApplicationDbContext _db;
        private readonly ILogger<ManageController> _logger;

        public ManageController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailSender emailSender,
            IWebHostEnvironment env,
            ApplicationDbContext db,
            ILogger<ManageController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _env = env;
            _db = db;
            _logger = logger;
        }

        // =========================
        // HỒ SƠ CÁ NHÂN
        // =========================
        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound($"Không tìm thấy user {_userManager.GetUserId(User)}");

            var vm = new UpdateProfileViewModel
            {
                Username = user.UserName ?? "",
                FullName = user.FullName ?? "",
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                CurrentAvatarUrl = user.Avatar
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(UpdateProfileViewModel vm, CancellationToken ct)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound($"Không tìm thấy user {_userManager.GetUserId(User)}");

            if (!ModelState.IsValid)
            {
                vm.Username = user.UserName ?? "";
                vm.CurrentAvatarUrl = user.Avatar;
                return View(vm);
            }

            bool changed = false;

            // Cập nhật họ tên / địa chỉ
            var newFullName = vm.FullName?.Trim();
            var newAddress = vm.Address?.Trim();
            if (!string.Equals(user.FullName, newFullName, StringComparison.Ordinal))
            {
                user.FullName = newFullName;
                changed = true;
            }
            if (!string.Equals(user.Address, newAddress, StringComparison.Ordinal))
            {
                user.Address = newAddress;
                changed = true;
            }

            // Cập nhật số điện thoại
            var currentPhone = await _userManager.GetPhoneNumberAsync(user);
            if (!string.Equals(currentPhone, vm.PhoneNumber, StringComparison.Ordinal))
            {
                var setPhone = await _userManager.SetPhoneNumberAsync(user, vm.PhoneNumber);
                if (!setPhone.Succeeded)
                {
                    foreach (var e in setPhone.Errors) ModelState.AddModelError("", e.Description);
                    vm.Username = user.UserName ?? "";
                    vm.CurrentAvatarUrl = user.Avatar;
                    return View(vm);
                }
            }

            // Upload avatar (tuân theo SD)
            if (vm.AvatarFile is not null && vm.AvatarFile.Length > 0)
            {
                var ext = Path.GetExtension(vm.AvatarFile.FileName).ToLowerInvariant();
                var allowed = new HashSet<string>(SD.AllowedImageExtensions, StringComparer.OrdinalIgnoreCase);

                if (!allowed.Contains(ext))
                    ModelState.AddModelError(nameof(vm.AvatarFile), $"Vui lòng chọn ảnh {string.Join('/', SD.AllowedImageExtensions)}");
                if (vm.AvatarFile.Length > SD.MaxUploadBytes)
                    ModelState.AddModelError(nameof(vm.AvatarFile), $"Ảnh tối đa {(SD.MaxUploadBytes / (1024 * 1024))}MB");

                if (!ModelState.IsValid)
                {
                    vm.Username = user.UserName ?? "";
                    vm.CurrentAvatarUrl = user.Avatar;
                    return View(vm);
                }

                var folder = Path.Combine(_env.WebRootPath, "images", "avatars");
                Directory.CreateDirectory(folder);

                // Xoá ảnh cũ (best-effort)
                if (!string.IsNullOrWhiteSpace(user.Avatar))
                {
                    try
                    {
                        var old = Path.Combine(_env.WebRootPath, user.Avatar.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                        if (System.IO.File.Exists(old)) System.IO.File.Delete(old);
                    }
                    catch { /* ignore */ }
                }

                var fileName = $"{Guid.NewGuid():N}{ext}";
                var savePath = Path.Combine(folder, fileName);
                await using (var fs = new FileStream(savePath, FileMode.Create))
                {
                    await vm.AvatarFile.CopyToAsync(fs, ct);
                }
                user.Avatar = $"/images/avatars/{fileName}";
                changed = true;
            }

            if (changed)
            {
                var upd = await _userManager.UpdateAsync(user);
                if (!upd.Succeeded)
                {
                    foreach (var e in upd.Errors) ModelState.AddModelError("", e.Description);
                    vm.Username = user.UserName ?? "";
                    vm.CurrentAvatarUrl = user.Avatar;
                    return View(vm);
                }
            }

            await _signInManager.RefreshSignInAsync(user);
            TempData[SD.Temp_Success] = "Cập nhật hồ sơ thành công.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveAvatar()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            if (!string.IsNullOrWhiteSpace(user.Avatar))
            {
                try
                {
                    var path = Path.Combine(_env.WebRootPath, user.Avatar.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                }
                catch { /* ignore */ }

                user.Avatar = null;
                await _userManager.UpdateAsync(user);
                await _signInManager.RefreshSignInAsync(user);
            }

            TempData[SD.Temp_Info] = "Đã xoá ảnh đại diện.";
            return RedirectToAction(nameof(Index));
        }

        // =========================
        // ĐỔI MẬT KHẨU
        // =========================
        [HttpGet]
        public IActionResult ChangePassword() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var result = await _userManager.ChangePasswordAsync(user, vm.OldPassword, vm.NewPassword);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
                return View(vm);
            }

            await _signInManager.RefreshSignInAsync(user);
            TempData[SD.Temp_Success] = "Đổi mật khẩu thành công.";
            return RedirectToAction(nameof(ChangePassword));
        }

        // =========================
        // QUẢN LÝ EMAIL
        // =========================
        [HttpGet]
        public async Task<IActionResult> Email()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var vm = new EmailViewModel
            {
                Email = await _userManager.GetEmailAsync(user),
                IsEmailConfirmed = await _userManager.IsEmailConfirmedAsync(user)
            };
            return View(vm);
        }

        // Gửi link xác nhận cho email hiện tại (HTML đẹp)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendVerificationEmail()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var userId = await _userManager.GetUserIdAsync(user);
            var email = await _userManager.GetEmailAsync(user);

            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callback = Url.Action("ConfirmEmail", "Account",
                                     new { userId, code = encoded }, protocol: Request.Scheme);

            var html = BuildActionEmailHtml(
                title: "Xác nhận địa chỉ Email",
                intro: $"Xin chào {(string.IsNullOrWhiteSpace(user.FullName) ? "bạn" : user.FullName)}!",
                message: "Hãy xác nhận địa chỉ email của bạn để kích hoạt đầy đủ tính năng.",
                buttonText: "Xác nhận Email",
                buttonUrl: callback!,
                note: "Nếu bạn không yêu cầu thao tác này, vui lòng bỏ qua email."
            );

            await _emailSender.SendEmailAsync(email!, SD.EmailSubject_ChangeEmailConfirm, html);

            TempData[SD.Temp_Success] = "Đã gửi email xác nhận. Vui lòng kiểm tra hộp thư.";
            return RedirectToAction(nameof(Email));
        }

        // Yêu cầu đổi sang email mới (gửi link xác nhận tới email mới – HTML đẹp)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Email(EmailViewModel vm)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var currentEmail = await _userManager.GetEmailAsync(user);
            vm.IsEmailConfirmed = await _userManager.IsEmailConfirmedAsync(user);
            vm.Email = currentEmail;

            if (!ModelState.IsValid) return View(vm);
            if (string.Equals(currentEmail, vm.NewEmail, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(vm.NewEmail), "Email mới phải khác email hiện tại.");
                return View(vm);
            }

            var code = await _userManager.GenerateChangeEmailTokenAsync(user, vm.NewEmail!);
            var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callback = Url.Action(nameof(ConfirmEmailChange), "Manage",
                                     new { userId = user.Id, email = vm.NewEmail, code = encoded }, protocol: Request.Scheme);

            var html = BuildActionEmailHtml(
                title: "Xác nhận thay đổi Email",
                intro: $"Xin chào {(string.IsNullOrWhiteSpace(user.FullName) ? "bạn" : user.FullName)}!",
                message: $"Bạn vừa yêu cầu thay đổi email đăng nhập MotorShop sang <b>{HtmlEncoder.Default.Encode(vm.NewEmail!)}</b>.",
                buttonText: "Xác nhận thay đổi Email",
                buttonUrl: callback!,
                note: "Nếu không phải bạn thực hiện, hãy bỏ qua email này."
            );

            await _emailSender.SendEmailAsync(vm.NewEmail!, SD.EmailSubject_ChangeEmailConfirm, html);

            TempData[SD.Temp_Info] = "Đã gửi liên kết xác nhận đến email mới.";
            return RedirectToAction(nameof(Email));
        }

        // Người dùng nhấn link xác nhận đổi email
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmailChange(string userId, string email, string code)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(code))
                return RedirectToAction("Index", "Home");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound($"Không tìm thấy người dùng {userId}");

            var oldEmail = user.Email;
            var oldUserName = user.UserName;

            string decoded;
            try { decoded = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code)); }
            catch
            {
                TempData[SD.Temp_Error] = "Mã xác nhận không hợp lệ.";
                return View("ConfirmEmailChangeStatus");
            }

            var result = await _userManager.ChangeEmailAsync(user, email, decoded);
            if (!result.Succeeded)
            {
                TempData[SD.Temp_Error] = "Không thể thay đổi email.";
                return View("ConfirmEmailChangeStatus");
            }

            // Đồng bộ UserName nếu đang dùng email cũ làm username
            if (!string.IsNullOrEmpty(oldEmail) &&
                string.Equals(oldUserName, oldEmail, StringComparison.OrdinalIgnoreCase))
            {
                var setUserName = await _userManager.SetUserNameAsync(user, email);
                if (!setUserName.Succeeded)
                {
                    TempData[SD.Temp_Info] = "Email đã đổi nhưng tên đăng nhập chưa cập nhật.";
                    return View("ConfirmEmailChangeStatus");
                }
            }

            // Làm mới đăng nhập nếu đúng chủ
            if (User.Identity?.IsAuthenticated == true &&
                User.FindFirstValue(ClaimTypes.NameIdentifier) == userId)
            {
                await _signInManager.RefreshSignInAsync(user);
            }

            TempData[SD.Temp_Success] = "Đã xác nhận thay đổi email.";
            return View("ConfirmEmailChangeStatus");
        }

        // =========================
        // TIỆN ÍCH BỔ SUNG
        // =========================

        // 1) Đăng xuất khỏi tất cả thiết bị (đổi security stamp)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SignOutAll()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            await _userManager.UpdateSecurityStampAsync(user);
            await _signInManager.SignOutAsync(); // đăng xuất phiên hiện tại
            TempData[SD.Temp_Info] = "Đã đăng xuất khỏi tất cả thiết bị.";
            return RedirectToAction("Login", "Account");
        }

        // 2) Bật/Tắt 2FA (cờ TwoFactorEnabled). Không cấu hình Authenticator ở đây.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleTwoFactor(bool enable)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var res = await _userManager.SetTwoFactorEnabledAsync(user, enable);
            if (!res.Succeeded)
            {
                TempData[SD.Temp_Error] = "Không thể thay đổi trạng thái 2FA.";
                return RedirectToAction(nameof(Index));
            }

            await _signInManager.RefreshSignInAsync(user);
            TempData[SD.Temp_Success] = enable ? "Đã bật xác thực hai lớp." : "Đã tắt xác thực hai lớp.";
            return RedirectToAction(nameof(Index));
        }

        // 3) Xuất dữ liệu cá nhân (JSON): hồ sơ + đơn hàng của chính user
        [HttpGet]
        public async Task<IActionResult> ExportMyData(CancellationToken ct)
        {
            var uid = _userManager.GetUserId(User);
            var user = await _userManager.FindByIdAsync(uid);
            if (user == null) return NotFound();

            var orders = await _db.Orders
                .Where(o => o.UserId == uid)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                .AsNoTracking()
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new
                {
                    o.Id,
                    o.OrderDate,
                    o.Status,
                    o.PaymentMethod,
                    o.PaymentStatus,
                    o.TotalAmount,
                    Items = o.OrderItems.Select(i => new
                    {
                        i.ProductId,
                        ProductName = i.Product!.Name,
                        i.Quantity,
                        i.UnitPrice
                    })
                })
                .ToListAsync(ct);

            var payload = new
            {
                Profile = new
                {
                    user.Id,
                    user.Email,
                    user.UserName,
                    user.FullName,
                    user.PhoneNumber,
                    user.Address,
                    user.Avatar,
                    user.EmailConfirmed,
                    TwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user)
                },
                Orders = orders
            };

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            var bytes = Encoding.UTF8.GetBytes(json);
            var fileName = $"motorshop-mydata-{DateTime.UtcNow:yyyyMMddHHmmss}.json";
            return File(bytes, "application/json", fileName);
        }

        // =========================
        // Email HTML helper
        // =========================
        private static string BuildActionEmailHtml(
            string title,
            string intro,
            string message,
            string buttonText,
            string buttonUrl,
            string note)
        {
            var safeUrl = HtmlEncoder.Default.Encode(buttonUrl);
            return $@"
<table role='presentation' width='100%' cellpadding='0' cellspacing='0' style='background:#f5f7fb;padding:24px 0'>
  <tr>
    <td align='center'>
      <table role='presentation' width='600' cellpadding='0' cellspacing='0' style='background:#ffffff;border-radius:16px;box-shadow:0 10px 25px rgba(0,0,0,.06);overflow:hidden;font-family:system-ui,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#111827'>
        <tr>
          <td style='padding:28px 32px 0 32px'>
            <div style='display:flex;align-items:center;gap:10px'>
              <div style='width:36px;height:36px;border-radius:10px;background:#2563eb;display:grid;place-items:center'>
                <span style='font-size:18px;color:#fff'>M</span>
              </div>
              <div style='font-weight:700;font-size:18px;color:#111827'>MotorShop</div>
            </div>
            <h1 style='margin:18px 0 8px;font-size:22px;line-height:28px'>{title}</h1>
            <p style='margin:0 0 6px;color:#374151'>{intro}</p>
            <p style='margin:0 0 16px;color:#374151'>{message}</p>
            <div style='margin:24px 0'>
              <a href='{safeUrl}' style='display:inline-block;background:#2563eb;color:#fff;text-decoration:none;border-radius:10px;padding:12px 18px;font-weight:600'>
                {buttonText}
              </a>
            </div>
            <p style='margin:0 0 30px;color:#6b7280;font-size:13px'>{note}</p>
          </td>
        </tr>
        <tr><td style='height:1px;background:#e5e7eb'></td></tr>
        <tr>
          <td style='padding:16px 32px 24px 32px;color:#9ca3af;font-size:12px'>
            &copy; {DateTime.UtcNow:yyyy} MotorShop. All rights reserved.
          </td>
        </tr>
      </table>
    </td>
  </tr>
</table>";
        }
    }
}
