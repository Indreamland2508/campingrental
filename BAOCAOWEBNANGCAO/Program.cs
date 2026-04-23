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

builder.Services.AddDbContext<CampingDbContext>(options =>
    options.UseNpgsql(connectionString));

// Gmail
builder.Services.AddTransient<IEmailSender, EmailSender>();

// 2. Cấu hình Identity & Bảo mật
builder.Services.Configure<IdentityOptions>(options =>
{
    // Lớp giáp 1: Khóa tài khoản (Lockout)
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15); // Bị khóa trong 15 phút
    options.Lockout.MaxFailedAccessAttempts = 5; // Gõ sai pass 5 lần là ăn ban
    options.Lockout.AllowedForNewUsers = true; // Áp dụng cho mọi tài khoản mới tạo
});

builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
{
    // Thiết lập thời gian sống của Token là 2 giờ (Tự động hết hạn)
    options.TokenLifespan = TimeSpan.FromHours(2);
});

builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false; // Tắt xác thực email để Login được ngay
    options.Password.RequireDigit = false; // Pass dễ (cho test)
    options.Password.RequireNonAlphanumeric = false; // Không cần ký tự đặc biệt
    options.Password.RequireUppercase = false; // Không cần chữ hoa
    options.Password.RequiredLength = 1; // Độ dài tối thiểu
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

// Seeder: Tạo dữ liệu mẫu (Admin)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<CampingDbContext>();

        context.Database.Migrate();

        // 2. ĐỔ DỮ LIỆU VÀO SAU (Khi bảng đã tồn tại chắc chắn)
        await DbSeeder.SeedDefaultData(services);
    }
    catch (Exception ex)
    {
        // Có thể in ra console để dễ debug trên Render
        Console.WriteLine("Lỗi khi khởi tạo Database: " + ex.Message);
    }
}

app.Run();