using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MotorShop.Data;
using MotorShop.Models;
using MotorShop.Utilities;

namespace MotorShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Manager")]
    public class BranchController : Controller
    {
        private readonly ApplicationDbContext _db;

        public BranchController(ApplicationDbContext db)
        {
            _db = db;
        }

        // ================== ViewModel cho Index ==================
        public class BranchIndexViewModel
        {
            public string? Search { get; set; }
            public bool? OnlyActive { get; set; }

            public int Page { get; set; }
            public int TotalPages { get; set; }
            public int TotalBranches { get; set; }
            public int ActiveCount { get; set; }
            public int InactiveCount { get; set; }

            public List<Branch> Items { get; set; } = new();
        }

        // ================== 1. INDEX: Danh sách + Lọc + Phân trang ==================
        public async Task<IActionResult> Index(string? q, bool? onlyActive, int page = 1)
        {
            const int pageSize = 10;
            page = Math.Max(1, page);

            var query = _db.Branches
                .AsNoTracking()
                .AsQueryable();

            // Tìm kiếm theo tên / mã / địa chỉ / Phone
            if (!string.IsNullOrWhiteSpace(q))
            {
                var kw = q.Trim();
                query = query.Where(b =>
                    EF.Functions.Like(b.Name, $"%{kw}%") ||
                    (!string.IsNullOrEmpty(b.Code) && EF.Functions.Like(b.Code!, $"%{kw}%")) ||
                    EF.Functions.Like(b.Address, $"%{kw}%") ||
                    (!string.IsNullOrEmpty(b.Phone) && b.Phone!.Contains(kw)));
            }

            // Lọc trạng thái
            if (onlyActive.HasValue)
            {
                query = query.Where(b => b.IsActive == onlyActive.Value);
            }

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(b => b.IsActive)  // chi nhánh đang hoạt động lên trước
                .ThenBy(b => b.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var vm = new BranchIndexViewModel
            {
                Search = q,
                OnlyActive = onlyActive,
                Page = page,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                TotalBranches = total,
                ActiveCount = await _db.Branches.CountAsync(b => b.IsActive),
                InactiveCount = await _db.Branches.CountAsync(b => !b.IsActive),
                Items = items
            };

            return View(vm);   // View: Areas/Admin/Views/Branch/Index.cshtml
        }

        // ================== 2. CREATE ==================
        public IActionResult Create()
        {
            // Trả về form với Branch rỗng
            return View(new Branch());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Branch branch)
        {
            if (!ModelState.IsValid)
            {
                return View(branch);
            }

            // Normalize dữ liệu text
            branch.Name = branch.Name.Trim();
            branch.Address = branch.Address.Trim();
            if (!string.IsNullOrWhiteSpace(branch.Code))
                branch.Code = branch.Code.Trim();
            if (!string.IsNullOrWhiteSpace(branch.Phone))
                branch.Phone = branch.Phone.Trim();
            if (!string.IsNullOrWhiteSpace(branch.OpeningHours))
                branch.OpeningHours = branch.OpeningHours.Trim();
            if (!string.IsNullOrWhiteSpace(branch.MapUrl))
                branch.MapUrl = branch.MapUrl.Trim();

            _db.Branches.Add(branch);
            await _db.SaveChangesAsync();

            TempData[SD.Temp_Success] = "Đã thêm chi nhánh / showroom mới.";
            return RedirectToAction(nameof(Index));
        }

        // ================== 3. EDIT ==================
        public async Task<IActionResult> Edit(int id)
        {
            var branch = await _db.Branches.FindAsync(id);
            if (branch == null) return NotFound();
            return View(branch);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Branch input)
        {
            if (id != input.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                return View(input);
            }

            var branch = await _db.Branches.FirstOrDefaultAsync(b => b.Id == id);
            if (branch == null) return NotFound();

            // Cập nhật các trường cho phép sửa
            branch.Name = input.Name.Trim();
            branch.Address = input.Address.Trim();
            branch.Code = string.IsNullOrWhiteSpace(input.Code) ? null : input.Code.Trim();
            branch.Phone = string.IsNullOrWhiteSpace(input.Phone) ? null : input.Phone.Trim();
            branch.OpeningHours = string.IsNullOrWhiteSpace(input.OpeningHours) ? null : input.OpeningHours.Trim();
            branch.MapUrl = string.IsNullOrWhiteSpace(input.MapUrl) ? null : input.MapUrl.Trim();
            branch.IsActive = input.IsActive;
            branch.Latitude = input.Latitude;
            branch.Longitude = input.Longitude;

            await _db.SaveChangesAsync();

            TempData[SD.Temp_Success] = "Đã cập nhật thông tin chi nhánh.";
            return RedirectToAction(nameof(Index));
        }

        // ================== 4. DELETE ==================
        public async Task<IActionResult> Delete(int id)
        {
            var branch = await _db.Branches
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == id);

            if (branch == null) return NotFound();
            return View(branch);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var branch = await _db.Branches
                .Include(b => b.PickupOrders)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (branch == null) return NotFound();

            // Nếu vẫn còn đơn nhận tại chi nhánh thì không cho xoá
            if (branch.PickupOrders != null && branch.PickupOrders.Any())
            {
                TempData[SD.Temp_Error] = "Không thể xoá: vẫn còn đơn hàng chọn nhận xe tại chi nhánh này.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            _db.Branches.Remove(branch);
            await _db.SaveChangesAsync();

            TempData[SD.Temp_Success] = "Đã xoá chi nhánh.";
            return RedirectToAction(nameof(Index));
        }

        // ================== 5. TOGGLE ACTIVE (Bật/Tắt nhanh) ==================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id, string? returnUrl = null)
        {
            var branch = await _db.Branches.FindAsync(id);
            if (branch == null) return NotFound();

            branch.IsActive = !branch.IsActive;
            await _db.SaveChangesAsync();

            TempData[SD.Temp_Success] = branch.IsActive
                ? "Đã kích hoạt chi nhánh."
                : "Đã tạm ẩn chi nhánh.";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction(nameof(Index));
        }
    }
}
