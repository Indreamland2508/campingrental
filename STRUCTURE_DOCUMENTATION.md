# Cấu trúc MVC Areas - ADMIN & CUSTOMER

## 📋 Tổng quan
Project đã được tổ chức lại theo mô hình **MVC Areas** với 2 khu vực chính: **ADMIN** (Quản lý) và **CUSTOMER** (Khách hàng).

---

## 📁 Cấu trúc thư mục

```
BAOCAOWEBNANGCAO/
├── Areas/
│   ├── Admin/                           # 🔐 AREA ADMIN (Quản lý)
│   │   ├── Controllers/                 # 6 Controllers
│   │   │   ├── AdminController.cs       # Dashboard admin
│   │   │   ├── AccountsController.cs    # Quản lý tài khoản người dùng
│   │   │   ├── CategoriesController.cs  # Quản lý danh mục
│   │   │   ├── ProductsController.cs    # Quản lý sản phẩm
│   │   │   ├── CombosController.cs      # Quản lý combo
│   │   │   └── OrdersController.cs      # Quản lý đơn hàng
│   │   ├── Views/                       # Views cho Admin
│   │   │   ├── Admin/                   # Dashboard views
│   │   │   ├── Accounts/                # Account management views
│   │   │   ├── Categories/              # Category views
│   │   │   ├── Products/                # Product views
│   │   │   ├── Combos/                  # Combo views
│   │   │   ├── Orders/                  # Order views
│   │   │   ├── _ViewImports.cshtml
│   │   │   └── _ViewStart.cshtml
│   │   ├── Models/                      # (Optional) Admin-specific models
│   │   └── Services/                    # (Optional) Admin-specific services
│   │
│   ├── Customer/                        # 👥 AREA CUSTOMER (Khách hàng)
│   │   ├── Controllers/                 # 3 Controllers
│   │   │   ├── HomeController.cs        # Trang chủ & danh sách sản phẩm
│   │   │   ├── CartController.cs        # Giỏ hàng & thanh toán
│   │   │   └── SePayController.cs       # Xử lý webhook thanh toán
│   │   ├── Views/                       # Views cho Customer
│   │   │   ├── Home/                    # Trang chủ, danh sách, chi tiết sản phẩm
│   │   │   ├── Cart/                    # Giỏ hàng, checkout
│   │   │   ├── _ViewImports.cshtml
│   │   │   └── _ViewStart.cshtml
│   │   ├── Models/                      # (Optional) Customer-specific models
│   │   └── Services/                    # (Optional) Customer-specific services
│   │
│   └── Identity/                        # Identity pages (Login, Register)
│
├── Data/                                # 🗄️ Database context & seeders
│   ├── CampingDbContext.cs
│   └── DbSeeder.cs
├── Models/                              # 📊 Models (dùng chung)
├── Services/                            # 🔧 Services (dùng chung)
├── Controllers/                         # ❌ KHÔNG CÒN (đã di chuyển sang Areas)
├── Views/                               # Root views (chỉ Shared, _ViewImports, _ViewStart)
│   ├── Shared/                          # Layout & shared components
│   ├── _ViewImports.cshtml
│   └── _ViewStart.cshtml
├── wwwroot/                             # Static files (CSS, JS, images)
├── Program.cs                           # ✅ Cấu hình Areas routing
└── BAOCAOWEBNANGCAO.csproj
```

---

## 🔗 URL Routing

### Admin Area
- Dashboard: `/Admin/Admin/Index`
- Quản lý tài khoản: `/Admin/Accounts/Index`
- Quản lý danh mục: `/Admin/Categories/Index`
- Quản lý sản phẩm: `/Admin/Products/Index`
- Quản lý combo: `/Admin/Combos/Index`
- Quản lý đơn hàng: `/Admin/Orders/Index`

### Customer Area (Route mặc định)
- Trang chủ: `/` hoặc `/Customer/Home/Index`
- Danh sách sản phẩm: `/Home/ProductList`
- Chi tiết sản phẩm: `/Home/Details/{id}`
- Giỏ hàng: `/Cart/Index`
- Thanh toán: `/Cart/Checkout`
- API Webhook: `/api/SePay/webhook`

---

## 🔐 Authorization

Các Controllers đã được cấu hình với Roles:
- **Admin Controllers**: `[Authorize(Roles = "Admin,Staff")]`
- **Customer Controllers**: Không yêu cầu authorization (trừ khi có logic riêng)

---

## 📝 Namespace

| Area | Namespace |
|------|-----------|
| Admin Controllers | `BAOCAOWEBNANGCAO.Areas.Admin.Controllers` |
| Customer Controllers | `BAOCAOWEBNANGCAO.Areas.Customer.Controllers` |
| Models (dùng chung) | `BAOCAOWEBNANGCAO.Models` |
| Data Context | `BAOCAOWEBNANGCAO.Data` |

---

## 🔧 Cấu hình Program.cs

Route configuration cho Areas:
```csharp
// Route cho Areas (Admin & Customer)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

// Route mặc định cho Customer
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
```

---

## ✅ Các thay đổi đã thực hiện

1. ✅ Tạo cấu trúc thư mục Models & Services trong mỗi Area
2. ✅ Di chuyển 6 Admin Controllers vào `Areas/Admin/Controllers`
3. ✅ Di chuyển 3 Customer Controllers vào `Areas/Customer/Controllers`
4. ✅ Di chuyển tất cả Views vào thư mục tương ứng
5. ✅ Cập nhật namespaces cho tất cả Controllers
6. ✅ Thêm `[Area("Admin")]` / `[Area("Customer")]` attributes
7. ✅ Cấu hình Areas routing trong Program.cs
8. ✅ Tạo `_ViewImports.cshtml` & `_ViewStart.cshtml` cho mỗi Area
9. ✅ Dọn dẹp root Controllers folder
10. ✅ Build thành công (0 errors, 48 warnings - nullable reference)

---

## 🚀 Tiếp theo

- Kiểm tra và test ứng dụng
- Cập nhật các navigation links trong Views nếu cần
- Thêm Models & Services riêng cho mỗi Area nếu cần (optional)
