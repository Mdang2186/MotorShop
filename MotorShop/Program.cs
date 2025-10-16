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

// Lấy chuỗi kết nối từ appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Đăng ký DbContext với SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Đăng ký ASP.NET Core Identity để quản lý người dùng và vai trò
builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Đăng ký các dịch vụ của MVC (Controllers, Views)
builder.Services.AddControllersWithViews();

// Đăng ký các dịch vụ tùy chỉnh
builder.Services.AddHttpContextAccessor(); // Cần thiết để truy cập HttpContext từ các service
builder.Services.AddScoped<CartService>();    // Đăng ký dịch vụ giỏ hàng
builder.Services.AddScoped<DbInitializer>();  // Đăng ký dịch vụ khởi tạo CSDL

// Cấu hình Session để lưu trữ giỏ hàng
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Thời gian chờ của session
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});


//================================================================//
// 2. KHU VỰC CẤU HÌNH PIPELINE XỬ LÝ HTTP REQUEST
//================================================================//

var app = builder.Build();

// Cấu hình pipeline cho môi trường Development và Production
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Cho phép sử dụng các file trong wwwroot (css, js, images)

app.UseRouting();

// Kích hoạt Session (phải đứng trước UseAuthorization)
app.UseSession();

// Kích hoạt Xác thực và Phân quyền (thứ tự này rất quan trọng)
app.UseAuthentication();
app.UseAuthorization();

// Cấu hình các route (đường dẫn URL)
// Ưu tiên route cho khu vực Admin
app.MapControllerRoute(
    name: "Admin",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

// Route mặc định cho người dùng
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Hỗ trợ các trang giao diện của Identity (đăng nhập, đăng ký,...)
app.MapRazorPages();

//================================================================//
// 3. KHỞI TẠO CƠ SỞ DỮ LIỆU (CHẠY 1 LẦN KHI ỨNG DỤNG KHỞI ĐỘNG)
//================================================================//

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var initializer = services.GetRequiredService<DbInitializer>();
    await initializer.Initialize();
}

// Chạy ứng dụng
app.Run();