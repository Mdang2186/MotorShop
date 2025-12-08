using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MotorShop.Data;
using MotorShop.Models;
using MotorShop.Utilities;

namespace MotorShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ShopBankAccountsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<ShopBankAccountsController> _logger;

        public ShopBankAccountsController(
            ApplicationDbContext db,
            ILogger<ShopBankAccountsController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // GET: Admin/ShopBankAccounts
        public async Task<IActionResult> Index()
        {
            var list = await _db.ShopBankAccounts
                .Include(x => x.Bank)
                .OrderByDescending(x => x.IsDefault)        // mặc định lên đầu
                .ThenByDescending(x => x.IsActive)          // đang hoạt động tiếp theo
                .ThenBy(x => x.Bank!.ShortName)             // Bank không null vì FK
                .AsNoTracking()
                .ToListAsync();

            return View(list);
        }

        // GET: Admin/ShopBankAccounts/Create
        public async Task<IActionResult> Create()
        {
            await LoadBanksAsync();

            // nếu chưa có TK nào thì TK đầu tiên tự là mặc định + active
            var hasAny = await _db.ShopBankAccounts.AnyAsync();
            return View(new ShopBankAccount
            {
                IsActive = true,
                IsDefault = !hasAny
            });
        }

        // POST: Admin/ShopBankAccounts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("BankId,AccountNumber,AccountName,Branch,Note,IsDefault,IsActive")]
            ShopBankAccount model)
        {
            // nếu trong entity có navigation Bank với [Required],
            // thì bỏ validation phần đó đi (form không post Bank)
            ModelState.Remove(nameof(ShopBankAccount.Bank));

            // 1. Clean dữ liệu đầu vào
            model.AccountNumber = model.AccountNumber?.Trim();
            model.AccountName = model.AccountName?.Trim().ToUpper(); // TÊN IN HOA
            model.Branch = model.Branch?.Trim();
            model.Note = model.Note?.Trim();

            // 2. Kiểm tra BankId
            if (model.BankId <= 0)
            {
                ModelState.AddModelError(nameof(model.BankId), "Vui lòng chọn ngân hàng.");
            }

            // 3. Một số rule đơn giản để tránh dữ liệu bậy
            if (string.IsNullOrWhiteSpace(model.AccountNumber))
            {
                ModelState.AddModelError(nameof(model.AccountNumber), "Số tài khoản không được để trống.");
            }
            if (string.IsNullOrWhiteSpace(model.AccountName))
            {
                ModelState.AddModelError(nameof(model.AccountName), "Tên chủ tài khoản không được để trống.");
            }

            if (!ModelState.IsValid)
            {
                await LoadBanksAsync();
                return View(model);
            }

            try
            {
                // 4. Logic mặc định
                var totalCount = await _db.ShopBankAccounts.CountAsync();

                if (totalCount == 0)
                {
                    // tài khoản đầu tiên: luôn là mặc định + đang active
                    model.IsDefault = true;
                    model.IsActive = true;
                }
                else if (model.IsDefault)
                {
                    // nếu đánh dấu mặc định -> bỏ mặc định các TK khác
                    var existingDefaults = await _db.ShopBankAccounts
                        .Where(x => x.IsDefault)
                        .ToListAsync();

                    foreach (var item in existingDefaults)
                    {
                        item.IsDefault = false;
                    }
                }

                // 5. Insert
                _db.ShopBankAccounts.Add(model);
                await _db.SaveChangesAsync();

                TempData[SD.Temp_Success] = "Đã thêm tài khoản ngân hàng thành công.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Lỗi khi thêm ShopBankAccount. BankId={BankId}, Account={Acc}",
                    model.BankId, model.AccountNumber);

                // hiển thị message ngắn để debug
                ModelState.AddModelError(string.Empty,
                    "Có lỗi xảy ra khi lưu dữ liệu: " + ex.Message);

                await LoadBanksAsync();
                return View(model);
            }
        }

        // GET: Admin/ShopBankAccounts/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var acc = await _db.ShopBankAccounts.FindAsync(id);
            if (acc == null) return NotFound();

            await LoadBanksAsync();
            return View(acc);
        }

        // POST: Admin/ShopBankAccounts/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,
            [Bind("Id,BankId,AccountNumber,AccountName,Branch,Note,IsDefault,IsActive")]
            ShopBankAccount model)
        {
            if (id != model.Id) return NotFound();

            ModelState.Remove(nameof(ShopBankAccount.Bank));

            model.AccountNumber = model.AccountNumber?.Trim();
            model.AccountName = model.AccountName?.Trim().ToUpper();
            model.Branch = model.Branch?.Trim();
            model.Note = model.Note?.Trim();

            if (model.BankId <= 0)
            {
                ModelState.AddModelError(nameof(model.BankId), "Vui lòng chọn ngân hàng.");
            }

            if (!ModelState.IsValid)
            {
                await LoadBanksAsync();
                return View(model);
            }

            try
            {
                var acc = await _db.ShopBankAccounts
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (acc == null)
                {
                    TempData[SD.Temp_Error] = "Tài khoản không tồn tại.";
                    return RedirectToAction(nameof(Index));
                }

                acc.BankId = model.BankId;
                acc.AccountNumber = model.AccountNumber;
                acc.AccountName = model.AccountName;
                acc.Branch = model.Branch;
                acc.Note = model.Note;
                acc.IsActive = model.IsActive;

                // xử lý mặc định
                if (model.IsDefault)
                {
                    var others = await _db.ShopBankAccounts
                        .Where(x => x.IsDefault && x.Id != id)
                        .ToListAsync();

                    foreach (var o in others)
                        o.IsDefault = false;

                    acc.IsDefault = true;
                }
                else
                {
                    acc.IsDefault = false;
                }

                await _db.SaveChangesAsync();

                TempData[SD.Temp_Success] = "Đã cập nhật tài khoản ngân hàng.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi update ShopBankAccount ID: {Id}", id);
                ModelState.AddModelError(string.Empty,
                    "Có lỗi xảy ra khi cập nhật: " + ex.Message);

                await LoadBanksAsync();
                return View(model);
            }
        }

        // GET: Admin/ShopBankAccounts/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var acc = await _db.ShopBankAccounts
                .Include(x => x.Bank)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (acc == null) return NotFound();
            return View(acc);
        }

        // POST: Admin/ShopBankAccounts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var acc = await _db.ShopBankAccounts.FindAsync(id);
            if (acc == null) return NotFound();

            if (acc.IsDefault)
            {
                TempData[SD.Temp_Error] =
                    "Không thể xóa tài khoản Mặc định. Hãy đặt tài khoản khác làm mặc định trước.";
                return RedirectToAction(nameof(Index));
            }

            _db.ShopBankAccounts.Remove(acc);
            await _db.SaveChangesAsync();

            TempData[SD.Temp_Success] = "Đã xóa tài khoản ngân hàng.";
            return RedirectToAction(nameof(Index));
        }

        // helper: load danh sách ngân hàng cho dropdown
        private async Task LoadBanksAsync()
        {
            var banks = await _db.Banks
                .Where(b => b.IsActive)
                .OrderBy(b => b.ShortName)
                .AsNoTracking()
                .ToListAsync();

            ViewBag.Banks = banks;
        }
    }
}
