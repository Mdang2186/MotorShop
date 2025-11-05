// File: Utilities/SD.cs
using System.Linq; // cho MaskCard()

namespace MotorShop.Utilities
{
    public static class SD
    {
        // ===== Roles =====
        public const string Role_Admin = "Admin";
        public const string Role_User = "User";

        // ===== Areas =====
        public const string AdminAreaName = "Admin";

        // ===== Session Keys =====
        public const string SessionCart = "SESSION_CART";
        public const string SessionCheckout = "SESSION_CHECKOUT";
        public const string SessionReturnUrl = "SESSION_RETURN_URL";

        // ===== TempData Keys =====
        public const string Temp_Success = "Temp_Success";
        public const string Temp_Error = "Temp_Error";
        public const string Temp_Warning = "Temp_Warning";
        public const string Temp_Info = "Temp_Info";

        // ===== Cookies =====
        public const string CookieCart = "cart";

        // ===== Pagination =====
        public const int DefaultPageSize = 12;
        public static readonly int[] PageSizeOptions = { 6, 12, 24, 48 };

        // ===== Uploads (wwwroot/) =====
        public const string ProductImageFolder = "images/products";
        public static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        public const long MaxUploadBytes = 2L * 1024L * 1024L; // 2MB

        // ===== Cache Keys (optional) =====
        public const string Cache_Brands = "CACHE_BRANDS";
        public const string Cache_Categories = "CACHE_CATEGORIES";

        // ===== Formatting =====
        public const string DateTimeDisplayFormat = "dd/MM/yyyy HH:mm";

        // ===== Email subjects =====
        public const string EmailSubject_OtpRegister = "Mã xác nhận tài khoản MotorShop";
        public const string EmailSubject_OtpResend = "Mã xác nhận tài khoản MotorShop (gửi lại)";
        public const string EmailSubject_ChangeEmailConfirm = "Xác nhận thay đổi Email tại MotorShop";
        public const string EmailSubject_PaymentConfirmation = "Xác nhận đơn hàng & thanh toán tại MotorShop";

        // ===== Helpers =====
        public static string MaskCard(string? pan)
        {
            if (string.IsNullOrWhiteSpace(pan)) return "**** **** **** ****";
            var digits = new string(pan.Where(char.IsDigit).ToArray());
            var last4 = digits.Length >= 4 ? digits[^4..] : digits;
            return $"**** **** **** {last4}";
        }

        // LƯU Ý:
        // - Không còn SD.Banks/Bank record mô phỏng. Banks được lấy từ DB:
        //   vm.Banks = await _db.Banks.Where(b => b.IsActive).OrderBy(b => b.SortOrder).ToListAsync();
        // - View dùng Model.Banks (List<Bank>) để render logo/tên/mã code.
        // - Muốn hiển thị logo/tên theo code ở Controller/View:
        //   var bankName = await _db.Banks.Where(b => b.Code == vm.SelectedBankCode).Select(b => b.Name).FirstOrDefaultAsync();
    }
}
