using ClosedXML.Excel;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MotorShop.Models;
using MotorShop.Utilities;
using System.Security.Cryptography;
using System.Text;

// ALIAS để tránh trùng tên
using PdfDocument = iTextSharp.text.Document;
using PdfParagraph = iTextSharp.text.Paragraph;
using ItFont = iTextSharp.text.Font;
using ItBaseColor = iTextSharp.text.BaseColor;
using PdfPageSize = iTextSharp.text.PageSize;

namespace MotorShop.Areas.Admin.Controllers
{
    [Area(SD.AdminAreaName)]
    [Authorize(Roles = SD.Role_Admin)]
    public class UserController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IWebHostEnvironment _env;
        private readonly IEmailSender _emailSender;

        public UserController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IWebHostEnvironment env,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _env = env;
            _emailSender = emailSender;
        }

        // ================== VIEWMODELS ==================
        public class UserListItemVM
        {
            public string Id { get; set; } = default!;
            public string? UserName { get; set; }
            public string? Email { get; set; }
            public string Roles { get; set; } = "";
            public DateTime CreatedAt { get; set; }
            public bool IsLocked { get; set; }
        }

        public class UserIndexVM
        {
            public string? Search { get; set; }
            public string? Role { get; set; }
            public int Page { get; set; } = 1;
            public int TotalPages { get; set; }
            public List<string> AllRoles { get; set; } = new();
            public List<UserListItemVM> Users { get; set; } = new();
        }

        public class UserEditVM
        {
            public string Id { get; set; } = default!;
            public string? Email { get; set; }
            public string? UserName { get; set; }
            public string? FullName { get; set; }
            public string? PhoneNumber { get; set; }
            public string? Address { get; set; }

            public bool IsLockedOut { get; set; }
            public List<string> SelectedRoles { get; set; } = new();
            public List<string> AllRoles { get; set; } = new();

            // Admin tick để gửi email khi thay đổi quyền/trạng thái
            public bool SendNotification { get; set; }
        }

        // phân trang
        private const int UserPageSize = 15;

        // ================== INDEX (LIST + FILTER) ==================
        public async Task<IActionResult> Index(string? search, string? role, int page = 1)
        {
            page = page < 1 ? 1 : page;

            var query = _userManager.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(u =>
                    (u.Email ?? "").Contains(search) ||
                    (u.UserName ?? "").Contains(search) ||
                    (u.FullName ?? "").Contains(search));
            }

            var allUsers = await query
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            var roleFilter = role?.Trim();
            var list = new List<UserListItemVM>();

            foreach (var u in allUsers)
            {
                var roles = await _userManager.GetRolesAsync(u);
                if (!string.IsNullOrEmpty(roleFilter) && !roles.Contains(roleFilter))
                    continue;

                list.Add(new UserListItemVM
                {
                    Id = u.Id,
                    UserName = u.UserName,
                    Email = u.Email,
                    Roles = string.Join(", ", roles),
                    CreatedAt = u.CreatedAt,
                    IsLocked = u.LockoutEnd.HasValue && u.LockoutEnd.Value.UtcDateTime > DateTime.UtcNow
                });
            }

            var total = list.Count;
            var totalPages = (int)Math.Ceiling(total / (double)UserPageSize);
            var paged = list
                .Skip((page - 1) * UserPageSize)
                .Take(UserPageSize)
                .ToList();

            var vm = new UserIndexVM
            {
                Search = search,
                Role = roleFilter,
                Page = page,
                TotalPages = totalPages,
                Users = paged,
                AllRoles = await _roleManager.Roles
                    .OrderBy(r => r.Name)
                    .Select(r => r.Name!)
                    .ToListAsync()
            };

            return View(vm);
        }

        // ================== EDIT (GET) ==================
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            var allRoles = await _roleManager.Roles
                .OrderBy(r => r.Name)
                .Select(r => r.Name!)
                .ToListAsync();

            var vm = new UserEditVM
            {
                Id = user.Id,
                Email = user.Email,
                UserName = user.UserName,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                SelectedRoles = roles.ToList(),
                AllRoles = allRoles,
                IsLockedOut = user.LockoutEnd.HasValue && user.LockoutEnd.Value.UtcDateTime > DateTime.UtcNow,
                SendNotification = false
            };

            return View(vm);
        }

        // ================== EDIT (POST) ==================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserEditVM vm)
        {
            if (!ModelState.IsValid)
            {
                vm.AllRoles = await _roleManager.Roles
                    .OrderBy(r => r.Name)
                    .Select(r => r.Name!)
                    .ToListAsync();
                return View(vm);
            }

            var user = await _userManager.FindByIdAsync(vm.Id);
            if (user == null) return NotFound();

            // Cập nhật thông tin cơ bản
            user.Email = vm.Email;
            user.UserName = vm.UserName;
            user.FullName = vm.FullName;
            user.PhoneNumber = vm.PhoneNumber;
            user.Address = vm.Address;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var e in updateResult.Errors)
                    ModelState.AddModelError(string.Empty, e.Description);

                vm.AllRoles = await _roleManager.Roles
                    .OrderBy(r => r.Name)
                    .Select(r => r.Name!)
                    .ToListAsync();
                return View(vm);
            }

            // Cập nhật role
            var currentRoles = await _userManager.GetRolesAsync(user);
            var selected = vm.SelectedRoles ?? new List<string>();

            var toRemove = currentRoles.Where(r => !selected.Contains(r));
            var toAdd = selected.Where(r => !currentRoles.Contains(r));

            if (toRemove.Any())
                await _userManager.RemoveFromRolesAsync(user, toRemove);

            if (toAdd.Any())
                await _userManager.AddToRolesAsync(user, toAdd);

            // Khoá / mở khoá
            var currentlyLocked = user.LockoutEnd.HasValue && user.LockoutEnd.Value.UtcDateTime > DateTime.UtcNow;
            if (vm.IsLockedOut && !currentlyLocked)
            {
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(50));
            }
            else if (!vm.IsLockedOut && currentlyLocked)
            {
                await _userManager.SetLockoutEndDateAsync(user, null);
            }

            // Gửi email thông báo nếu admin tick
            if (vm.SendNotification && !string.IsNullOrWhiteSpace(user.Email))
            {
                try
                {
                    var rolesAfter = await _userManager.GetRolesAsync(user);
                    await SendUserUpdatedEmailAsync(user, rolesAfter.ToList(), vm.IsLockedOut);
                }
                catch { /* không cản trở flow nếu lỗi gửi mail */ }
            }

            TempData[SD.Temp_Success] = "Đã cập nhật thông tin người dùng.";
            return RedirectToAction(nameof(Index));
        }

        // ================== DELETE ==================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData[SD.Temp_Error] = "Không tìm thấy người dùng.";
                return RedirectToAction(nameof(Index));
            }

            var currentUserId = _userManager.GetUserId(User);
            if (user.Id == currentUserId)
            {
                TempData[SD.Temp_Error] = "Không thể xoá tài khoản đang đăng nhập.";
                return RedirectToAction(nameof(Index));
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData[SD.Temp_Success] = "Đã xoá tài khoản.";
            }
            else
            {
                TempData[SD.Temp_Error] = string.Join("; ", result.Errors.Select(e => e.Description));
            }

            return RedirectToAction(nameof(Index));
        }

        // ================== RESET PASSWORD (ADMIN) ==================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData[SD.Temp_Error] = "Không tìm thấy người dùng.";
                return RedirectToAction(nameof(Index));
            }

            var newPassword = GenerateSecurePassword(10);
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (!result.Succeeded)
            {
                TempData[SD.Temp_Error] = string.Join("; ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Index));
            }

            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                try
                {
                    var html = BuildAdminResetPasswordEmailHtml(user, newPassword);
                    await _emailSender.SendEmailAsync(
                        user.Email,
                        "Đặt lại mật khẩu tài khoản MotorShop",
                        html);
                }
                catch { }
            }

            TempData[SD.Temp_Success] = "Đã reset mật khẩu và gửi email cho người dùng.";
            return RedirectToAction(nameof(Index));
        }

        // ================== EXPORT EXCEL ==================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportExcel(string? search, string? role)
        {
            var data = await BuildUserListAsync(search, role);

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Users");

            ws.Cell(1, 1).Value = "BÁO CÁO DANH SÁCH NGƯỜI DÙNG";
            ws.Range(1, 1, 1, 6).Merge()
                .Style.Font.SetBold()
                .Font.SetFontSize(16)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(2, 1).Value = $"Thời gian xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
            ws.Range(2, 1, 2, 6).Merge()
                .Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            int row = 4;
            ws.Cell(row, 1).Value = "STT";
            ws.Cell(row, 2).Value = "Email";
            ws.Cell(row, 3).Value = "Tên đăng nhập";
            ws.Cell(row, 4).Value = "Quyền";
            ws.Cell(row, 5).Value = "Ngày tạo";
            ws.Cell(row, 6).Value = "Trạng thái";

            ws.Range(row, 1, row, 6).Style
                .Font.SetBold()
                .Fill.SetBackgroundColor(XLColor.LightGray);
            row++;

            int stt = 1;
            foreach (var u in data)
            {
                ws.Cell(row, 1).Value = stt++;
                ws.Cell(row, 2).Value = u.Email;
                ws.Cell(row, 3).Value = u.UserName;
                ws.Cell(row, 4).Value = u.Roles;
                ws.Cell(row, 5).Value = u.CreatedAt;
                ws.Cell(row, 5).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
                ws.Cell(row, 6).Value = u.IsLocked ? "Khoá" : "Hoạt động";
                row++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            var content = stream.ToArray();
            return File(content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Users_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
        }

        // ================== EXPORT PDF ==================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportPdf(string? search, string? role)
        {
            var data = await BuildUserListAsync(search, role);

            using var ms = new MemoryStream();
            var doc = new PdfDocument(PdfPageSize.A4.Rotate(), 25, 25, 30, 30);
            var writer = PdfWriter.GetInstance(doc, ms);
            doc.Open();

            // Font tiếng Việt
            string fontPath = Path.Combine(_env.WebRootPath, "fonts", "arial.ttf");
            if (!System.IO.File.Exists(fontPath))
            {
                fontPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Fonts),
                    "arial.ttf");
            }

            BaseFont bf = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
            var colorBlue = new ItBaseColor(99, 102, 241);
            var colorWhite = new ItBaseColor(255, 255, 255);
            var colorBlack = new ItBaseColor(0, 0, 0);
            var colorGray = new ItBaseColor(148, 163, 184);

            ItFont fontTitle = new ItFont(bf, 16, ItFont.BOLD, colorBlue);
            ItFont fontHeader = new ItFont(bf, 11, ItFont.BOLD, colorWhite);
            ItFont fontNormal = new ItFont(bf, 10, ItFont.NORMAL, colorBlack);

            var pTitle = new PdfParagraph("BÁO CÁO DANH SÁCH NGƯỜI DÙNG", fontTitle)
            {
                Alignment = Element.ALIGN_CENTER,
                SpacingAfter = 8f
            };
            doc.Add(pTitle);

            var pDate = new PdfParagraph(
                $"Thời gian xuất: {DateTime.Now:dd/MM/yyyy HH:mm}",
                new ItFont(bf, 9, ItFont.NORMAL, colorGray))
            {
                Alignment = Element.ALIGN_CENTER,
                SpacingAfter = 12f
            };
            doc.Add(pDate);

            PdfPTable table = new PdfPTable(6)
            {
                WidthPercentage = 100
            };
            table.SetWidths(new float[] { 1f, 3f, 3f, 3f, 3f, 2f });

            void AddHeader(string text)
            {
                var cell = new PdfPCell(new Phrase(text, fontHeader))
                {
                    BackgroundColor = colorBlue,
                    Padding = 5,
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    VerticalAlignment = Element.ALIGN_MIDDLE
                };
                table.AddCell(cell);
            }

            AddHeader("STT");
            AddHeader("Email");
            AddHeader("Tên đăng nhập");
            AddHeader("Quyền");
            AddHeader("Ngày tạo");
            AddHeader("Trạng thái");

            int stt = 1;
            foreach (var u in data)
            {
                PdfPCell Cell(string txt, int align = Element.ALIGN_LEFT)
                {
                    return new PdfPCell(new Phrase(txt, fontNormal))
                    {
                        Padding = 4,
                        HorizontalAlignment = align,
                        VerticalAlignment = Element.ALIGN_MIDDLE
                    };
                }

                table.AddCell(Cell(stt.ToString(), Element.ALIGN_CENTER));
                table.AddCell(Cell(u.Email ?? ""));
                table.AddCell(Cell(u.UserName ?? ""));
                table.AddCell(Cell(u.Roles));
                table.AddCell(Cell(u.CreatedAt.ToString("dd/MM/yyyy HH:mm"), Element.ALIGN_CENTER));
                table.AddCell(Cell(u.IsLocked ? "Khoá" : "Hoạt động", Element.ALIGN_CENTER));

                stt++;
            }

            doc.Add(table);

            doc.Close();
            writer.Close();

            return File(ms.ToArray(), "application/pdf",
                $"Users_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
        }

        // ================== HELPERS ==================
        private async Task<List<UserListItemVM>> BuildUserListAsync(string? search, string? role)
        {
            var query = _userManager.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(u =>
                    (u.Email ?? "").Contains(search) ||
                    (u.UserName ?? "").Contains(search) ||
                    (u.FullName ?? "").Contains(search));
            }

            var allUsers = await query
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            var roleFilter = role?.Trim();
            var list = new List<UserListItemVM>();

            foreach (var u in allUsers)
            {
                var roles = await _userManager.GetRolesAsync(u);
                if (!string.IsNullOrEmpty(roleFilter) && !roles.Contains(roleFilter))
                    continue;

                list.Add(new UserListItemVM
                {
                    Id = u.Id,
                    UserName = u.UserName,
                    Email = u.Email,
                    Roles = string.Join(", ", roles),
                    CreatedAt = u.CreatedAt,
                    IsLocked = u.LockoutEnd.HasValue && u.LockoutEnd.Value.UtcDateTime > DateTime.UtcNow
                });
            }

            return list;
        }

        private string GenerateSecurePassword(int length = 10)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz123456789!@#$%";
            var data = new byte[length];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(data);

            var sb = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                sb.Append(chars[data[i] % chars.Length]);
            }
            return sb.ToString();
        }

        private string BuildAdminResetPasswordEmailHtml(ApplicationUser user, string newPassword)
        {
            var safeName = string.IsNullOrWhiteSpace(user.FullName)
                ? "bạn" : System.Net.WebUtility.HtmlEncode(user.FullName);

            return $@"
<!doctype html><html lang='vi'><meta charset='utf-8'>
<body style=""margin:0;padding:0;background:#0f172a;font-family:-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#e5e7eb"">
  <div style=""max-width:640px;margin:24px auto;background:#020617;border-radius:16px;border:1px solid #1f2937;padding:24px"">
    <h2 style=""margin-top:0;color:#e5e7eb"">Đặt lại mật khẩu MotorShop</h2>
    <p>Xin chào {safeName},</p>
    <p>Quản trị viên đã đặt lại mật khẩu cho tài khoản của bạn.</p>
    <p>Mật khẩu mới của bạn là:</p>
    <div style=""display:inline-block;margin:8px 0;padding:10px 16px;border-radius:10px;
                background:linear-gradient(135deg,#22d3ee,#6366f1);color:#0f172a;font-weight:600;"">
        {System.Net.WebUtility.HtmlEncode(newPassword)}
    </div>
    <p>Vui lòng đăng nhập và đổi sang mật khẩu riêng của bạn trong phần hồ sơ tài khoản.</p>
    <p style=""font-size:12px;color:#9ca3af"">Nếu bạn không yêu cầu thao tác này, hãy liên hệ với bộ phận hỗ trợ MotorShop.</p>
  </div>
</body></html>";
        }

        private async Task SendUserUpdatedEmailAsync(ApplicationUser user, List<string> roles, bool isLocked)
        {
            if (string.IsNullOrWhiteSpace(user.Email)) return;

            var safeName = string.IsNullOrWhiteSpace(user.FullName)
                ? "bạn" : System.Net.WebUtility.HtmlEncode(user.FullName);

            var roleText = roles.Any() ? string.Join(", ", roles) : "Không có";
            var statusText = isLocked ? "Tài khoản đang bị khóa." : "Tài khoản đang hoạt động bình thường.";

            var html = $@"
<!doctype html><html lang='vi'><meta charset='utf-8'>
<body style=""margin:0;padding:0;background:#020617;font-family:-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#e5e7eb"">
  <div style=""max-width:640px;margin:24px auto;background:#020617;border-radius:16px;
              border:1px solid #1f2937;padding:24px"">
    <h2 style=""margin-top:0"">Cập nhật thông tin tài khoản</h2>
    <p>Xin chào {safeName},</p>
    <p>Cấu hình tài khoản MotorShop của bạn vừa được quản trị viên cập nhật.</p>
    <ul>
      <li><b>Quyền hiện tại:</b> {System.Net.WebUtility.HtmlEncode(roleText)}</li>
      <li><b>Trạng thái:</b> {System.Net.WebUtility.HtmlEncode(statusText)}</li>
    </ul>
    <p>Nếu đây không phải là thay đổi mà bạn mong muốn, vui lòng liên hệ bộ phận hỗ trợ MotorShop.</p>
  </div>
</body></html>";

            await _emailSender.SendEmailAsync(
                user.Email!,
                "Cập nhật thông tin tài khoản MotorShop",
                html);
        }
    }
}
