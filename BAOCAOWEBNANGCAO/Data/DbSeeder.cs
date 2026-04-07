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


            // 1. Tạo Role "Admin" (Đã có)
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            }

            if (!await roleManager.RoleExistsAsync("Staff"))
            {
                await roleManager.CreateAsync(new IdentityRole("Staff"));
            }

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

            var context = service.GetService<CampingDbContext>();
            if (context != null && !context.Products.Any())
            {
            }
            // Kiểm tra xem bảng Products đã có dữ liệu chưa. Nếu trống thì mới thêm vào.
            if (!context.Products.Any())
            {
                var sanPhamMau = new List<Product>
    {
        // --- NHÓM BÀN GHẾ ---
        new Product {
            Name = "BÀN DÃ NGOẠI HỢP KIM NHÔM NATUREHIKE NH16Z016-L",
            Description = "Bàn dã ngoại cao cấp làm từ hợp kim nhôm chắc chắn... (như cũ)",
            PricePerDay = 50000, Quantity = 10, ImageUrl = "/images/products/BÀN DÃ NGOẠI HỢP KIM NHÔM NATUREHIKE NH16Z016-L.jpg", CategoryId = 3
        },
        new Product {
            Name = "BÀN NHÔM CUỘN GẤP GỌN CẮM TRẠI NATUREHIKE CNK2300JU010",
            Description = "Bàn nhôm cuộn gấp gọn chuyên dụng cho hoạt động cắm trại và dã ngoại. Thiết kế mặt bàn dạng cuộn giúp dễ dàng tháo lắp và cất giữ trong balo hoặc cốp xe. Khung nhôm bền bỉ, chịu lực tốt, thích hợp sử dụng cho các chuyến camping, picnic hoặc trekking.",
            PricePerDay = 40000, Quantity = 15, ImageUrl = "/images/products/BÀN NHÔM CUỘN GẤP GỌN CẮM TRẠI NATUREHIKE CNK2300JU010.jpg", CategoryId = 3
        },
        new Product {
            Name = "BỘ BÀN GHẾ DÃ NGOẠI GẤP GỌN KHUNG NHÔM CAO CẤP",
            Description = "Bộ bàn ghế dã ngoại khung nhôm cao cấp...",
            PricePerDay = 100000, Quantity = 5, ImageUrl = "/images/products/BỘ BÀN GHẾ DÃ NGOẠI GẤP GỌN KHUNG NHÔM CAO CẤP.jpg", CategoryId = 3
        },
        new Product {
            Name = "BỘ BÀN GHẾ DÃ NGOẠI VINTAGE KHUNG NHÔM - TRẮNG",
            Description = "Bộ bàn ghế phong cách vintage trang nhã...",
            PricePerDay = 120000, Quantity = 5, ImageUrl = "/images/products/BỘ BÀN GHẾ DÃ NGOẠI VINTAGE KHUNG NHÔM - TRẮNG.jpg", CategoryId = 3
        },
        new Product {
            Name = "GHẾ GẤP DÃ NGOẠI VINTAGE KHUNG SẮT TAY GỖ NATUREHIKE",
            Description = "Ghế gấp dã ngoại phong cách vintage tay vịn gỗ...",
            PricePerDay = 40000, Quantity = 20, ImageUrl = "/images/products/GHẾ GẤP DÃ NGOẠI VINTAGE KHUNG SẮT TAY GỖ NATUREHIKE CNK2300JU012.jpg", CategoryId = 3
        },
        new Product {
            Name = "GHẾ XẾP DÃ NGOẠI THƯ GIÃN NATUREHIKE NH18X004-Y",
            Description = "Ghế xếp dã ngoại thiết kế chắc chắn thoải mái...",
            PricePerDay = 35000, Quantity = 20, ImageUrl = "/images/products/GHẾ XẾP DÃ NGOẠI THƯ GIÃN NATUREHIKE NH18X004 - Y.jpg", CategoryId = 3
        },

        // --- NHÓM BẾP & NƯỚNG ---
        new Product {
            Name = "BẾP CỦI DÃ NGOẠI GẤP GỌN NATUREHIKE NH20SK001",
            Description = "Bếp củi dã ngoại gấp gọn tiện lợi...",
            PricePerDay = 40000, Quantity = 15, ImageUrl = "/images/products/naturehike-h-l-a-ng-di-ng-l-a-t-ch-y-gi-c-m-tr-jpg-720x720-2.png", CategoryId = 2
        },
        new Product {
            Name = "BẾP ĐỨNG NƯỚNG THAN INOX TO CAO CẤP DÀNH CHO 12-15 NGƯỜI",
            Description = "Bếp nướng than inox kích thước lớn cho nhóm đông...",
            PricePerDay = 150000, Quantity = 5, ImageUrl = "/images/products/BẾP ĐỨNG NƯỚNG THAN INOX TO CAO CẤP DÀNH CHO 12-15 NGƯỜI.jpg", CategoryId = 2
        },
        new Product {
            Name = "BẾP NƯỚNG THAN DÃ NGOẠI CÓ CHÂN LARTISAN LTS01",
            Description = "Bếp nướng than dã ngoại có chân chắc chắn...",
            PricePerDay = 60000, Quantity = 10, ImageUrl = "/images/products/BẾP NƯỚNG THAN DÃ NGOẠI CÓ CHÂN LARTISAN LTS01.jpg", CategoryId = 2
        },
        new Product {
            Name = "BẾP SƯỞI THAN HOA MINI ĐỂ BÀN ĐA NĂNG NATUREHIKE",
            Description = "Bếp sưởi than hoa mini đa năng nhỏ gọn...",
            PricePerDay = 40000, Quantity = 10, ImageUrl = "/images/products/BẾP SƯỞI THAN HOA MINI ĐỂ BÀN ĐA NĂNG NATUREHIKE CNK2300CF010.jpg", CategoryId = 2
        },
        new Product {
            Name = "THAN VUÔNG SINH HỌC KHÔNG KHÓI NƯỚNG BBQ",
            Description = "Than vuông sinh học không khói (Giá bán 1 hộp)...",
            PricePerDay = 25000, Quantity = 50, ImageUrl = "/images/products/THAN VUÔNG SINH HỌC KHÔNG KHÓI NƯỚNG BBQ.jpg", CategoryId = 2
        },

        // --- NHÓM ĐÈN ---
        new Product {
            Name = "ĐÈN DÃ NGOẠI CHỐNG CÔN TRÙNG NATUREHIKE NH20ZM003",
            Description = "Đèn dã ngoại tích hợp chiếu sáng và chống côn trùng...",
            PricePerDay = 40000, Quantity = 20, ImageUrl = "/images/products/ĐÈN DÃ NGOẠI CHỐNG CÔN TRÙNG NATUREHIKE NH20ZM003.jpg", CategoryId = 3
        },
        new Product {
            Name = "ĐÈN DÂY LED TRANG TRÍ DÃ NGOẠI BÓNG TRÒN MÀU VÀNG ẤM",
            Description = "Đèn dây LED trang trí ánh sáng vàng ấm...",
            PricePerDay = 20000, Quantity = 30, ImageUrl = "/images/products/ĐÈN DÂY LED TRANG TRÍ DÃ NGOẠI BÓNG TRÒN MÀU VÀNG ẤM.jpg", CategoryId = 3
        },
        new Product {
            Name = "ĐÈN LED RETRO WILD LAND HEMP ROPE LANTERN",
            Description = "Đèn LED phong cách retro dây thừng độc đáo...",
            PricePerDay = 40000, Quantity = 15, ImageUrl = "/images/products/ĐÈN LED RETRO WILD LAND HEMP ROPE LANTERN.jpg", CategoryId = 3
        },
        new Product {
            Name = "ĐÈN LED RETRO WILD LAND THE HARMONY LANTERN",
            Description = "Đèn LED retro sang trọng ánh sáng ấm áp...",
            PricePerDay = 50000, Quantity = 10, ImageUrl = "/images/products/ĐÈN LED RETRO WILD LAND THE HARMONY LANTERN.jpg", CategoryId = 3
        },
        new Product {
            Name = "Đèn Lều, Đèn Dã Ngoại Đa Năng sử dụng pin AAA NatureHike",
            Description = "Đèn lều đa năng sử dụng pin AAA tiện lợi...",
            PricePerDay = 20000, Quantity = 30, ImageUrl = "/images/products/Đèn Lều, Đèn Dã Ngoại Đa Năng sử dụng pin AAA NatureHike NH15A003-I.jpg", CategoryId = 3
        },
        new Product {
            Name = "ĐÈN TREO LỀU VINTAGE GLAMPING NATUREHIKE CNH22DQ007",
            Description = "Đèn treo lều phong cách vintage cho glamping...",
            PricePerDay = 35000, Quantity = 15, ImageUrl = "/images/products/ĐÈN TREO LỀU VINTAGE GLAMPING NATUREHIKE CNH22DQ007.jpg", CategoryId = 3
        },
        new Product {
            Name = "Đèn bão Vintage",
            Description = "Đèn bão phong cách vintage ánh sáng ấm áp...",
            PricePerDay = 30000, Quantity = 20, ImageUrl = "/images/products/den-bao.jpg", CategoryId = 3
        },

        // --- NHÓM LỀU & TĂNG CHE ---
        new Product {
            Name = "COMBO LỀU 2 NGƯỜI",
            Description = "Combo lều 2 người bao gồm phụ kiện cơ bản...",
            PricePerDay = 80000, Quantity = 15, ImageUrl = "/images/products/COMBO LỀU 2 NGƯỜI.jpg", CategoryId = 1
        },
        new Product {
            Name = "Lều cắm trại đôi gọn nhẹ",
            Description = "Lều 2 người nhỏ gọn, tiện lợi mang theo trekking...",
            PricePerDay = 60000, Quantity = 20, ImageUrl = "/images/products/hainguoi.jpg", CategoryId = 1
        },
        new Product {
            Name = "COMBO LỀU PHÒNG 16-18 NGƯỜI",
            Description = "Lều phòng kích thước cực lớn cho team building...",
            PricePerDay = 350000, Quantity = 3, ImageUrl = "/images/products/COMBO LỀU PHÒNG 16-18 NGƯỜI.jpg", CategoryId = 1
        },
        new Product {
            Name = "LỀU TRUNG TÂM, LỀU CHE DÃ NGOẠI",
            Description = "Lều trung tâm kích thước lớn dùng che nắng mưa...",
            PricePerDay = 150000, Quantity = 5, ImageUrl = "/images/products/LỀU TRUNG TÂM, LỀU CHE DÃ NGOẠI.jpg", CategoryId = 1
        },
        new Product {
            Name = "TĂNG CHE 3 ĐỈNH (3 GIAN)",
            Description = "Tăng che thiết kế 3 đỉnh rộng rãi...",
            PricePerDay = 120000, Quantity = 8, ImageUrl = "/images/products/TĂNG CHE 3 ĐỈNH (3 GIAN).jpg", CategoryId = 1
        },

        // --- NHÓM PHỤ KIỆN NGỦ (THẢM, ĐỆM, TÚI NGỦ) ---
        new Product {
            Name = "ĐỆM XỐP, THẢM NGỦ CAO CẤP CHỐNG THẤM",
            Description = "Đệm xốp thảm ngủ cách nhiệt chống thấm...",
            PricePerDay = 15000, Quantity = 30, ImageUrl = "/images/products/ĐỆM XỐP, THẢM NGỦ CAO CẤP CHỐNG THẤM VĂN PHÒNG, CẮM TRẠI.jpg", CategoryId = 3
        },
        new Product {
            Name = "TẤM CÁCH NHIỆT, CHỐNG NÓNG LẠNH CHỐNG THẤM",
            Description = "Tấm cách nhiệt lót lều đa năng...",
            PricePerDay = 15000, Quantity = 30, ImageUrl = "/images/products/TẤM CÁCH NHIỆT, CHỐNG NÓNG LẠNH, CHỐNG THẤM THÔNG MINH.jpg", CategoryId = 3
        },
        new Product {
            Name = "TẤM LÓT SÀN LỀU 2 NGƯỜI MADFOX FOOTPRINT",
            Description = "Tấm lót đáy lều 2 người chống thấm...",
            PricePerDay = 10000, Quantity = 20, ImageUrl = "/images/products/TẤM LÓT SÀN LỀU 2 NGƯỜI MADFOX FOOTPRIN.jpg", CategoryId = 3
        },
        new Product {
            Name = "TẤM LÓT SÀN LỀU 4 NGƯỜI MADFOX FOOTPRINT",
            Description = "Tấm lót đáy lều 4 người chống thấm bùn đất...",
            PricePerDay = 15000, Quantity = 20, ImageUrl = "/images/products/TẤM LÓT SÀN LỀU 4 NGƯỜI MADFOX FOOTPRINT.jpg", CategoryId = 3
        },
        new Product {
            Name = "THẢM DÃ NGOẠI CHỐNG THẤM NATUREHIKE NH20FCD04",
            Description = "Thảm ngồi picnic chống thấm gấp gọn...",
            PricePerDay = 30000, Quantity = 25, ImageUrl = "/images/products/THẢM DÃ NGOẠI CHỐNG THẤM NATUREHIKE NH20FCD04.jpg", CategoryId = 3
        },
        new Product {
            Name = "THẢM DÃ NGOẠI OXFORD NATUREHIKE NH21FCD01",
            Description = "Thảm ngồi vải Oxford cao cấp bền bỉ...",
            PricePerDay = 40000, Quantity = 15, ImageUrl = "/images/products/THẢM DÃ NGOẠI OXFORD NATUREHIKE NH21FCD01.jpg", CategoryId = 3
        },
        new Product {
            Name = "TÚI NGỦ COTTON CÓ MŨ TRÙM ĐẦU NATUREHIKE",
            Description = "Túi ngủ cotton dày dặn có mũ trùm giữ ấm...",
            PricePerDay = 40000, Quantity = 20, ImageUrl = "/images/products/TÚI NGỦ COTTON CÓ MŨ TRÙM ĐẦU NATUREHIKE NH21MSD07.jpg", CategoryId = 3
        },
        new Product {
            Name = "TÚI NGỦ COTTON MỎNG NATUREHIKE NH15S012-E",
            Description = "Túi ngủ mỏng nhẹ cho thời tiết mát mẻ...",
            PricePerDay = 25000, Quantity = 30, ImageUrl = "/images/products/TÚI NGỦ COTTON MỎNG NATUREHIKE NH15S012-E.jpg", CategoryId = 3
        },
        new Product {
            Name = "TÚI NGỦ COTTON NATUREHIKE NH19S015-D",
            Description = "Túi ngủ cao cấp thoáng khí...",
            PricePerDay = 40000, Quantity = 20, ImageUrl = "/images/products/TÚI NGỦ COTTON NATUREHIKE NH19S015-D.jpg", CategoryId = 3
        },
        new Product {
            Name = "TÚI NGỦ ĐÔI KÈM GỐI NATUREHIKE HOẠ TIẾT 3 CON GẤU",
            Description = "Túi ngủ đôi rộng rãi họa tiết dễ thương...",
            PricePerDay = 60000, Quantity = 10, ImageUrl = "/images/products/TÚI NGỦ ĐÔI KÈM GỐI NATUREHIKE NH19S016-D HOẠ TIẾT 3 CON GẤU.jpg", CategoryId = 3
        },
        new Product {
            Name = "TÚI NGỦ ĐÔI NATUREHIKE TẶNG KÈM GỐI",
            Description = "Túi ngủ đôi thoải mái cho 2 người...",
            PricePerDay = 60000, Quantity = 10, ImageUrl = "/images/products/TÚI NGỦ ĐÔI NATUREHIKE SD15M030J TẶNG KÈM GỐI.jpg", CategoryId = 3
        },
        new Product {
            Name = "TÚI NGỦ DU LỊCH, LEO NÚI NATUREHIKE NH15S003-D",
            Description = "Túi ngủ chuyên dụng leo núi siêu giữ ấm...",
            PricePerDay = 50000, Quantity = 15, ImageUrl = "/images/products/TÚI NGỦ DU LỊCH, LEO NÚI NATUREHIKE NH15S003-D.jpg", CategoryId = 3
        }
    };

                context.Products.AddRange(sanPhamMau);
                await context.SaveChangesAsync();
            }
        }
    }
}