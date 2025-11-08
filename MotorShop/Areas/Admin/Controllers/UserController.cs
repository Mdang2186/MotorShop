using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MotorShop.Data;
using MotorShop.Models;
using MotorShop.Utilities;
using MotorShop.ViewModels; // Thêm ViewModel mới
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MotorShop.Areas.Admin.Controllers
{
    [Area(SD.AdminAreaName)]
    [Authorize(Roles = SD.Role_Admin)] // Chỉ Admin mới được vào
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager; // <-- THÊM MỚI

        public UserController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager) // <-- CẬP NHẬT
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager; // <-- THÊM MỚI
        }

        // ViewModel cho trang Index
        public class UserListItemVM
        {
            public string Id { get; set; } = default!;
            public string? UserName { get; set; }
            public string? Email { get; set; }
            public string Roles { get; set; } = "";
            public DateTime CreatedAt { get; set; }
            public bool IsLockedOut { get; set; }
        }

        // GET: Admin/User
        public async Task<IActionResult> Index(string? q, string? role, int page = 1, int pageSize = 15)
        {
            var query = _userManager.Users.AsQueryable();

            // Lọc theo tìm kiếm (q)
            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim().ToLower();
                query = query.Where(u => (u.UserName != null && u.UserName.ToLower().Contains(q)) ||
                                         (u.Email != null && u.Email.ToLower().Contains(q)));
            }

            // Lọc theo vai trò (role)
            if (!string.IsNullOrWhiteSpace(role))
            {
                var usersInRole = await _userManager.GetUsersInRoleAsync(role);
                var userIdsInRole = usersInRole.Select(u => u.Id).ToList();
                query = query.Where(u => userIdsInRole.Contains(u.Id));
            }

            // Phân trang
            var totalItems = await query.CountAsync();
            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var list = new List<UserListItemVM>();
            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                list.Add(new UserListItemVM
                {
                    Id = u.Id,
                    UserName = u.UserName,
                    Email = u.Email,
                    Roles = string.Join(", ", roles),
                    CreatedAt = u.CreatedAt,
                    IsLockedOut = u.LockoutEnd.HasValue && u.LockoutEnd.Value > DateTimeOffset.UtcNow
                });
            }

            // Đưa danh sách Roles ra View để lọc
            ViewBag.Roles = new SelectList(await _roleManager.Roles.ToListAsync(), "Name", "Name", role);
            ViewBag.Q = q;
            ViewBag.Role = role;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)System.Math.Ceiling((double)totalItems / pageSize);

            return View(list);
        }

        // (MỚI) GET: Admin/User/Manage/5
        public async Task<IActionResult> Manage(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var allRoles = await _roleManager.Roles.ToListAsync();
            var userRoles = await _userManager.GetRolesAsync(user);

            var vm = new ManageUserViewModel
            {
                UserId = user.Id,
                UserName = user.UserName ?? "N/A",
                Email = user.Email ?? "N/A",
                CreatedAt = user.CreatedAt,
                Roles = allRoles.Select(r => new RoleCheckboxVM
                {
                    RoleId = r.Id,
                    RoleName = r.Name ?? "N/A",
                    IsSelected = userRoles.Contains(r.Name)
                }).ToList()
            };

            return View(vm);
        }

        // (MỚI) POST: Admin/User/Manage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Manage(ManageUserViewModel vm)
        {
            var user = await _userManager.FindByIdAsync(vm.UserId);
            if (user == null) return NotFound();

            var currentRoles = await _userManager.GetRolesAsync(user);
            var selectedRoles = vm.Roles.Where(r => r.IsSelected).Select(r => r.RoleName).ToList();

            // Xoá các quyền cũ không được chọn
            var resultRemove = await _userManager.RemoveFromRolesAsync(user, currentRoles.Except(selectedRoles));
            if (!resultRemove.Succeeded)
            {
                TempData["Error"] = "Lỗi khi xoá quyền cũ của người dùng.";
                return View(vm);
            }

            // Thêm các quyền mới được chọn
            var resultAdd = await _userManager.AddToRolesAsync(user, selectedRoles.Except(currentRoles));
            if (!resultAdd.Succeeded)
            {
                TempData["Error"] = "Lỗi khi thêm quyền mới cho người dùng.";
                return View(vm);
            }

            TempData["Success"] = "Cập nhật quyền cho người dùng thành công.";
            return RedirectToAction(nameof(Index));
        }
    }
}