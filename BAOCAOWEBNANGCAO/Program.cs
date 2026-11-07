using BAOCAOWEBNANGCAO.Data;
using BAOCAOWEBNANGCAO.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
var builder = WebApplication.CreateBuilder(args);

// 1. Kết nối Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
if (builder.Environment.IsDevelopment())
{
    var hosts = new Npgsql.NpgsqlConnectionStringBuilder(connectionString).Host
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    if (hosts.Length == 0 || hosts.Any(host =>
            host != "localhost" &&
            host != "127.0.0.1" &&
            host != "::1"))
    {
        throw new InvalidOperationException(
            "Development startup requires a PostgreSQL connection hosted on this machine.");
    }
}

builder.Services.AddDbContext<CampingDbContext>(options =>
    options.UseNpgsql(connectionString));

// Gmail
builder.Services.AddHttpClient();
builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<OrderReservationService>();
builder.Services.AddScoped<OrderNotificationService>();
builder.Services.AddScoped<TurnstileVerificationService>();
builder.Services.AddHostedService<OrderReservationCleanupService>();

builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
{
    options.TokenLifespan = TimeSpan.FromHours(2);
});

builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 12;
    options.Password.RequiredUniqueChars = 4;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<CampingDbContext>();

// 3. Thêm dịch vụ MVC
builder.Services.AddControllersWithViews();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Giỏ hàng tồn tại trong 30 phút nếu không thao tác
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ==========================================================
var app = builder.Build();
// ==========================================================


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication(); // Xác thực (Ai đó?)
app.UseAuthorization();  // Phân quyền (Làm gì?)

app.UseSession();

// Định tuyến
// Route cho Areas (Admin & Customer)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

// Route mặc định cho Customer Area
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}",
    defaults: new { area = "Customer" });

app.MapRazorPages(); // Để chạy các trang Login/Register mặc định

// Apply schema changes and seed required data.
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<CampingDbContext>();
    context.Database.Migrate();
    await DbSeeder.SeedDefaultData(services);
}

app.Run();