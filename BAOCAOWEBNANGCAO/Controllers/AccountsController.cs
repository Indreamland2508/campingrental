using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
namespace BAOCAOWEBNANGCAO.Controllers
{
    public class AccountsController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        public AccountsController(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            SignInManager<IdentityUser> signInManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            // Sau khi thoát thì quay về trang chủ khách hàng
            return RedirectToAction("Index", "Home");
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            return View(users);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(string email, string password, string phoneNumber)
        {
            var strictEmailRegex = @"^[a-zA-Z0-9._%+-]+@(gmail\.com|yahoo\.com|outlook\.com|campingrental\.vn|epu\.edu\.vn)$";

            if (string.IsNullOrEmpty(email) || !Regex.IsMatch(email, strictEmailRegex, RegexOptions.IgnoreCase))
            {
                ModelState.AddModelError("", "Hệ thống từ chối: Chỉ chấp nhận email từ @gmail.com, @yahoo.com, @outlook.com hoặc email nội bộ.");
                return View();
            }
            if (string.IsNullOrEmpty(phoneNumber) || !Regex.IsMatch(phoneNumber, @"^0\d{9}$"))
            {
                ModelState.AddModelError("", "Hệ thống từ chối: Số điện thoại phải gồm đúng 10 chữ số và bắt đầu bằng số 0.");
                return View();
            }

            if (ModelState.IsValid)
            {
                var user = new IdentityUser
                {
                    UserName = email,
                    Email = email,
                    PhoneNumber = phoneNumber,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, password);

                if (result.Succeeded)
                {
                    if (!await _roleManager.RoleExistsAsync("Staff"))
                    {
                        await _roleManager.CreateAsync(new IdentityRole("Staff"));
                    }

                    await _userManager.AddToRoleAsync(user, "Staff");
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            return View();
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                if (user.Email.ToLower() == "admin@gmail.com" || user.UserName == User.Identity.Name)
                {
                    TempData["Error"] = "Không thể xóa tài khoản Quản trị viên!";
                    return RedirectToAction(nameof(Index));
                }

                await _userManager.DeleteAsync(user);
            }
            return RedirectToAction(nameof(Index));
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> MyProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Accounts");
            }

            var roles = await _userManager.GetRolesAsync(user);
            ViewBag.RoleName = roles.FirstOrDefault() ?? "Chưa gán quyền";

            return View(user);
        }
        [HttpPost]
        public async Task<IActionResult> UpdateAvatar(IFormFile avatarFile)
        {
            var user = await _userManager.GetUserAsync(User);

            // Nếu chưa đăng nhập hoặc không có file gửi lên thì quay về
            if (user == null || avatarFile == null || avatarFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn một bức ảnh hợp lệ!";
                return RedirectToAction("MyProfile");
            }

            // 1. Tạo thư mục "avatars" trong thư mục wwwroot/images nếu chưa có
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "avatars");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // 2. Tên file CỐ ĐỊNH là ID của user + đuôi .jpg
            var uniqueFileName = user.Id + ".jpg";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // 3. Xóa ảnh cũ (nếu có) để không bị đầy rác hệ thống
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            // 4. Lưu ảnh mới vào thư mục
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await avatarFile.CopyToAsync(fileStream);
            }

            TempData["SuccessMessage"] = "Đã cập nhật ảnh đại diện thành công!";
            return RedirectToAction("MyProfile");
        }
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(string phoneNumber)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Accounts");
            }

            if (string.IsNullOrEmpty(phoneNumber) || !Regex.IsMatch(phoneNumber, @"^0\d{9}$"))
            {
                TempData["ErrorMessage"] = "Cập nhật thất bại: Số điện thoại không đúng định dạng!";
                return RedirectToAction(nameof(MyProfile));
            }

            user.PhoneNumber = phoneNumber;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Cập nhật hồ sơ thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi lưu thay đổi.";
            }

            return RedirectToAction(nameof(MyProfile));
        }

    }
}