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

        // 3. POST: Xử lý khi nhân viên bấm "Lưu Combo"
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Combo combo, int[] SelectedProductIds, IFormFile? ImageUpload)
        {
            if (ModelState.IsValid)
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
                    combo.ImageUrl = "/images/combos/" + uniqueFileName; // Lưu đường dẫn vào DB
                }

                // B. Lưu thông tin Combo vào DB trước để lấy được cái ID của nó
                _context.Add(combo);
                await _context.SaveChangesAsync();

                // C. Xử lý lưu các món đồ (Sản phẩm) mà nhân viên đã tick chọn vào Combo
                if (SelectedProductIds != null && SelectedProductIds.Length > 0)
                {
                    foreach (var productId in SelectedProductIds)
                    {
                        var comboDetail = new ComboDetail
                        {
                            ComboId = combo.Id, // Nối với ID Combo vừa tạo
                            ProductId = productId, // Nối với ID Sản phẩm được tick
                            Quantity = 1 // Tạm thời để mặc định mỗi món 1 cái
                        };
                        _context.ComboDetails.Add(comboDetail);
                    }
                    await _context.SaveChangesAsync(); // Lưu tất cả chi tiết vào DB
                }

                return RedirectToAction(nameof(Index)); // Quay về trang danh sách
            }

            ViewBag.Products = _context.Products.ToList();
            return View(combo);
        }
    }
}