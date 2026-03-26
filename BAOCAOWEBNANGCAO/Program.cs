using BAOCAOWEBNANGCAO.Data;
using BAOCAOWEBNANGCAO.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
var builder = WebApplication.CreateBuilder(args);

// --- PHẦN 1: CẤU HÌNH SERVICES (Trước khi Build) ---

// 1. Kết nối Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<CampingDbContext>(options =>
    options.UseNpgsql(connectionString));
//Gmail
builder.Services.AddTransient<IEmailSender, EmailSender>();
// 2. Cấu hình Identity
// Tìm đoạn AddDbContext và thay thế/kiểm tra đoạn Identity bên dưới:
builder.Services.Configure<IdentityOptions>(options =>
{
    // Cấu hình khóa tài khoản (Lockout)
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15); // Bị khóa trong 15 phút
    options.Lockout.MaxFailedAccessAttempts = 5; // Gõ sai pass 5 lần là ăn ban
    options.Lockout.AllowedForNewUsers = true; // Áp dụng cho mọi tài khoản mới tạo
});
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    // CẤU HÌNH QUAN TRỌNG
    options.SignIn.RequireConfirmedAccount = false; // Tắt xác thực email để Login được ngay
    options.Password.RequireDigit = false; // Pass dễ (cho test)
    options.Password.RequireNonAlphanumeric = false; // Không cần ký tự đặc biệt
    options.Password.RequireUppercase = false; // Không cần chữ hoa
    options.Password.RequiredLength = 1; // Độ dài tối thiểu
})
.AddRoles<IdentityRole>() // <--- BẮT BUỘC ĐỂ DÙNG ROLE
.AddEntityFrameworkStores<CampingDbContext>();
// 3. Thêm dịch vụ MVC
builder.Services.AddControllersWithViews();

// 4. [MỚI] Thêm dịch vụ Session (Giỏ hàng)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Giỏ hàng tồn tại trong 30 phút nếu không thao tác
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// --- PHẦN 2: CẤU HÌNH PIPELINE (Sau khi Build) ---

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

// 5. [MỚI] Kích hoạt Session (Bắt buộc đặt sau UseRouting và trước MapControllerRoute)
app.UseSession();

// Định tuyến
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages(); // Để chạy các trang Login/Register mặc định

// Seeder: Tạo dữ liệu mẫu (Admin)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<CampingDbContext>();

        // 1. TẠO BẢNG TRƯỚC (Bắt buộc phải chạy đầu tiên)
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
builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
{
    // Thiết lập thời gian sống của Token là 2 giờ (Tự động hết hạn)
    options.TokenLifespan = TimeSpan.FromHours(2);

    // Mẹo: Giám đốc có thể đổi thành TimeSpan.FromMinutes(15) 
    // nếu muốn ép bảo mật cao như App Ngân hàng (hết hạn sau 15 phút)
});
app.Run();