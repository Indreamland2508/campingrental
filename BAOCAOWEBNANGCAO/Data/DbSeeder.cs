using BAOCAOWEBNANGCAO.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore; // Nhớ thêm thư viện này nếu dùng đoạn code tạo sản phẩm

namespace BAOCAOWEBNANGCAO.Data
{
    public static class DbSeeder
    {
        public static async Task SeedDefaultData(IServiceProvider service)
        {
            var userManager = service.GetService<UserManager<IdentityUser>>();
            var roleManager = service.GetService<RoleManager<IdentityRole>>();

            // --- PHẦN 1: TẠO ROLE (QUYỀN) ---

            // 1. Tạo Role "Admin" (Đã có)
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            }

            // 2. Tạo Role "Staff" (MỚI THÊM VÀO ĐÂY) <--- QUAN TRỌNG
            if (!await roleManager.RoleExistsAsync("Staff"))
            {
                await roleManager.CreateAsync(new IdentityRole("Staff"));
            }

            // --- PHẦN 2: TẠO TÀI KHOẢN ADMIN ---
            var adminEmail = "admin@gmail.com";
            var existingAdmin = await userManager.FindByEmailAsync(adminEmail);

            if (existingAdmin == null)
            {
                var newAdmin = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                // Mật khẩu Admin
                await userManager.CreateAsync(newAdmin, "Admin@123");

                // Gán quyền Admin
                await userManager.AddToRoleAsync(newAdmin, "Admin");
            }

            // --- PHẦN 3: TỰ ĐỘNG THÊM SẢN PHẨM MẪU (Giữ lại đoạn này nếu bạn muốn tự sinh lều) ---
            var context = service.GetService<CampingDbContext>();
            if (context != null && !context.Products.Any())
            {
                // (Đoạn code tạo sản phẩm mẫu bạn có thể dán lại vào đây nếu muốn)
            }
        }
    }
}