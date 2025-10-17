// using statements giữ nguyên...
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MotorShop.Data;
using MotorShop.Models;
using MotorShop.Services;
using MotorShop.Utilities;

var builder = WebApplication.CreateBuilder(args);

//================================================================//
// 1. KHU VỰC ĐĂNG KÝ CÁC DỊCH VỤ (SERVICE REGISTRATION)
//================================================================//

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// THAY ĐỔI 1: Sử dụng AddIdentity thay cho AddDefaultIdentity để có toàn quyền kiểm soát
builder.Services.AddIdentity<ApplicationUser, IdentityRole>() // Bỏ (options => options.SignIn.RequireConfirmedAccount = false) nếu không cần
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders(); // Thêm AddDefaultTokenProviders để hỗ trợ các chức năng như reset password

// THÊM MỚI: Cấu hình đường dẫn cho Identity để trỏ tới AccountController của chúng ta
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<DbInitializer>();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

//================================================================//
// 2. KHU VỰC CẤU HÌNH PIPELINE XỬ LÝ HTTP REQUEST
//================================================================//

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

// Route cho khu vực Admin (đã chính xác)
app.MapControllerRoute(
    name: "Admin",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

// Route mặc định cho người dùng (đã chính xác)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// BỎ ĐI: Không cần MapRazorPages() nữa vì chúng ta đã dùng MVC Controller
// app.MapRazorPages();

//================================================================//
// 3. KHỞI TẠO CƠ SỞ DỮ LIỆU
//================================================================//

// Đổi tên phương thức thành InitializeAsync() để tuân thủ quy tắc đặt tên
async Task SeedDatabaseAsync()
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var initializer = services.GetRequiredService<DbInitializer>();
        await initializer.Initialize(); 
    }
}

// Gọi hàm khởi tạo CSDL
await SeedDatabaseAsync();

app.Run();