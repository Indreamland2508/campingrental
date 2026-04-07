using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BAOCAOWEBNANGCAO.Data;
using BAOCAOWEBNANGCAO.Models;
using Microsoft.AspNetCore.Hosting;

namespace BAOCAOWEBNANGCAO.Controllers
{
    // [Area("Admin")] // Mở comment dòng này nếu Controller này nằm trong thư mục Areas/Admin
    public class CombosController : Controller
    {
        private readonly CampingDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public CombosController(CampingDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // 1. GET: Hiển thị danh sách Combo
        public async Task<IActionResult> Index()
        {
            // Lấy danh sách combo kèm theo số lượng đồ bên trong
            var combos = await _context.Combos
                .Include(c => c.ComboDetails)
                .ThenInclude(cd => cd.Product)
                .ToListAsync();
            return View(combos);
        }

        // 2. GET: Form thêm mới Combo
        public IActionResult Create()
        {
            // Kéo toàn bộ sản phẩm đang có lên View để làm danh sách Checkbox
            ViewBag.Products = _context.Products.ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Combo combo, int[] SelectedProductIds, IFormCollection form, IFormFile? ImageUpload)
        {
            // BỎ QUA KIỂM TRA MODELSTATE NGUYÊN BẢN (TRÁNH LỖI NGẦM)
            if (string.IsNullOrEmpty(combo.Name))
            {
                ModelState.AddModelError("Name", "Tên Combo là bắt buộc.");
                ViewBag.Products = _context.Products.ToList();
                return View(combo);
            }

            try
            {
                // A. Xử lý lưu ảnh Combo
                if (ImageUpload != null && ImageUpload.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "combos");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + ImageUpload.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await ImageUpload.CopyToAsync(fileStream);
                    }
                    combo.ImageUrl = "/images/combos/" + uniqueFileName;
                }

                // B. LƯU COMBO VÀO DATABASE (Lấy ID)
                _context.Combos.Add(combo);
                await _context.SaveChangesAsync();

                // C. LƯU CÁC SẢN PHẨM (ĐỒ LỀ) VÀO COMBO ĐÓ
                if (SelectedProductIds != null && SelectedProductIds.Length > 0)
                {
                    foreach (var productId in SelectedProductIds)
                    {
                        int quantity = 1;
                        // Tìm số lượng tương ứng với ID sản phẩm này
                        if (int.TryParse(form[$"Quantities_{productId}"], out int parsedQty))
                        {
                            quantity = parsedQty > 0 ? parsedQty : 1;
                        }

                        var comboDetail = new ComboDetail
                        {
                            ComboId = combo.Id,
                            ProductId = productId,
                            Quantity = quantity // Đưa số lượng vừa hứng được vào DB
                        };
                        _context.ComboDetails.Add(comboDetail);
                    }
                }

                // Thành công thì nhảy về trang danh sách
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Nếu có lỗi lúc lưu vào DB (ví dụ rớt mạng), in ra màn hình hoặc log
                Console.WriteLine("LỖI KHI LƯU COMBO: " + ex.Message);
                ModelState.AddModelError("", "Đã xảy ra lỗi khi lưu vào Database. Vui lòng thử lại.");
            }

            ViewBag.Products = _context.Products.ToList();
            return View(combo);
        }
        // ==========================================
        // 4. GET: Form Chỉnh sửa Combo
        // ==========================================
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            // Kéo Combo lên, nhớ Include thêm ComboDetails để biết nó đang chứa món đồ nào
            var combo = await _context.Combos
                .Include(c => c.ComboDetails)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (combo == null) return NotFound();

            // Kéo danh sách đồ nghề ra để làm Checkbox
            ViewBag.Products = _context.Products.ToList();
            return View(combo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Combo comboData, int[] SelectedProductIds, IFormCollection form, IFormFile? ImageUpload)
        {
            if (id != comboData.Id) return NotFound();

            var comboToUpdate = await _context.Combos
                .Include(c => c.ComboDetails)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (comboToUpdate == null) return NotFound();

            if (string.IsNullOrEmpty(comboData.Name))
            {
                ModelState.AddModelError("Name", "Tên Combo là bắt buộc.");
                ViewBag.Products = _context.Products.ToList();
                return View(comboData);
            }

            try
            {
                comboToUpdate.Name = comboData.Name;
                comboToUpdate.Price = comboData.Price;
                comboToUpdate.Badge = comboData.Badge;
                comboToUpdate.Description = comboData.Description;
                comboToUpdate.IsActive = comboData.IsActive;

                if (ImageUpload != null && ImageUpload.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "combos");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + ImageUpload.FileName;
                    using (var fileStream = new FileStream(Path.Combine(uploadsFolder, uniqueFileName), FileMode.Create))
                    {
                        await ImageUpload.CopyToAsync(fileStream);
                    }
                    comboToUpdate.ImageUrl = "/images/combos/" + uniqueFileName;
                }

                // Xóa chi tiết cũ đi
                _context.ComboDetails.RemoveRange(comboToUpdate.ComboDetails);

                // Thêm chi tiết mới kèm theo SỐ LƯỢNG
                if (SelectedProductIds != null && SelectedProductIds.Length > 0)
                {
                    foreach (var productId in SelectedProductIds)
                    {
                        int quantity = 1; // Mặc định là 1

                        // Cố gắng tìm ô nhập số lượng Quantities_ID từ View gửi lên
                        if (int.TryParse(form[$"Quantities_{productId}"], out int parsedQty))
                        {
                            quantity = parsedQty > 0 ? parsedQty : 1;
                        }

                        _context.ComboDetails.Add(new ComboDetail
                        {
                            ComboId = comboToUpdate.Id,
                            ProductId = productId,
                            Quantity = quantity // Đã lưu số lượng thực tế
                        });
                    }
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật gói Combo thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                Console.WriteLine("LỖI SỬA COMBO: " + ex.Message);
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi lưu vào Database.";
            }

            ViewBag.Products = _context.Products.ToList();
            return View(comboData);
        }

        // ==========================================
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var combo = await _context.Combos
                .Include(c => c.ComboDetails) // Kéo theo chi tiết để xóa sạch không để rác
                .FirstOrDefaultAsync(c => c.Id == id);

            if (combo != null)
            {
                // Xóa chi tiết trước (đồ bên trong), xóa cái giỏ (combo) sau
                _context.ComboDetails.RemoveRange(combo.ComboDetails);
                _context.Combos.Remove(combo);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}