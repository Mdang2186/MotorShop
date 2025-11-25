using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using MotorShop.Data;
using MotorShop.Hubs;
using MotorShop.Models;
using MotorShop.Services;
using MotorShop.Utilities;

var builder = WebApplication.CreateBuilder(args);

// =============================
// 1) REGISTER SERVICES
// =============================

// Email (IEmailSender)
builder.Services.Configure<MailSettings>(
    builder.Configuration.GetSection("MailSettings"));
builder.Services.AddTransient<IEmailSender, EmailSender>();

// DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(opt =>
{
    opt.UseSqlServer(connectionString);
#if DEBUG
    opt.EnableSensitiveDataLogging();
#endif
});
builder.Services.AddAntiforgery(o => { o.HeaderName = "RequestVerificationToken"; });

// Identity
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(opt =>
    {
        opt.SignIn.RequireConfirmedEmail = true;
        opt.Lockout.AllowedForNewUsers = true;
        opt.Lockout.MaxFailedAccessAttempts = 5;
        opt.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireRole(SD.Role_Admin));
    // Có thể thêm FallbackPolicy nếu muốn toàn site yêu cầu đăng nhập:
    // options.FallbackPolicy = new AuthorizationPolicyBuilder()
    //     .RequireAuthenticatedUser()
    //     .Build();
});

// Token lifespan cho email/change-email
builder.Services.Configure<DataProtectionTokenProviderOptions>(o =>
{
    o.TokenLifespan = TimeSpan.FromHours(3);
});

// MVC
builder.Services.AddControllersWithViews();

// SignalR (cho chat)
builder.Services.AddSignalR();

// Đăng ký ChatService cho DI
builder.Services.AddScoped<IChatService, ChatService>();

// Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(opt =>
{
    opt.IdleTimeout = TimeSpan.FromMinutes(30);
    opt.Cookie.HttpOnly = true;
    opt.Cookie.IsEssential = true;
    opt.Cookie.Name = ".MotorShop.Session";
});

// Helpers/DI khác
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<DbInitializer>();

// =============================
// 2) HTTP PIPELINE
// =============================
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Thứ tự đúng
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// Areas (Admin)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

// Default route (frontend)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Map Hub cho chat
app.MapHub<ChatHub>("/chathub");

// =============================
// 3) SEED DATABASE
// =============================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var initializer = services.GetRequiredService<DbInitializer>();
    await initializer.InitializeAsync();
}

app.Run();
