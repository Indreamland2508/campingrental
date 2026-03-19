using BAOCAOWEBNANGCAO.Data;
using BAOCAOWEBNANGCAO.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BAOCAOWEBNANGCAO.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly CampingDbContext _context;

        public HomeController(ILogger<HomeController> logger, CampingDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // 1. Lấy danh sách sản phẩm
            var products = await _context.Products.Include(p => p.Category).ToListAsync();

            // 2. Lấy Top 6 sản phẩm Trending (Logic cũ của bạn rất hay, giữ nguyên!)
            var trendingProducts = await _context.OrderDetails
                .GroupBy(od => od.ProductId)
                .OrderByDescending(g => g.Count())
                .Take(6)
                .Select(g => g.First().Product)
                .ToListAsync();

            if (!trendingProducts.Any())
            {
                trendingProducts = products.Take(6).ToList();
            }

            // 3. Lấy Feedback của khách hàng
            var feedbacks = await _context.Feedbacks
                .Where(f => f.IsApproved)
                .OrderByDescending(f => f.CreatedAt)
                .Take(3)
                .ToListAsync();

            // 4. LẤY DANH SÁCH COMBO (Mới thêm)
            var activeCombos = await _context.Combos
                .Include(c => c.ComboDetails) // Kéo theo chi tiết
                .ThenInclude(cd => cd.Product) // Kéo theo món đồ để lấy tên
                .Where(c => c.IsActive == true) // Chỉ lấy combo đang bật
                .Take(3) // Lấy tối đa 3 combo
                .ToListAsync();

            // 5. Gom tất cả đồ nghề ném ra ViewBag cho View sử dụng
            ViewBag.TrendingProducts = trendingProducts;
            ViewBag.Feedbacks = feedbacks;
            ViewBag.Combos = activeCombos;

            // 6. Chỉ có ĐÚNG 1 lệnh return ở cuối cùng này thôi nhé!
            return View(products);
        }

        // --- GỘP CHUNG PRODUCTLIST ĐỂ TRÁNH XUNG ĐỘT ---
        public async Task<IActionResult> ProductList(string category, string searchString)
        {
            var query = _context.Products.Include(p => p.Category).AsQueryable();

            // 1. Lọc theo danh mục
            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(p => p.Category.Name == category);
            }

            // 2. Lọc theo từ khóa (Sửa lỗi tìm gì cũng ra tất cả)
            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.Trim().ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(searchString));
            }

            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.CurrentCategory = category;
            ViewBag.SearchString = searchString; // Để giữ lại chữ trong ô tìm kiếm

            return View(await query.ToListAsync());
        }

        // --- HÀM LẤY GỢI Ý NHANH (API CHO AUTOCOMPLETE) ---
        [HttpGet]
        public async Task<JsonResult> GetSuggestions(string term)
        {
            if (string.IsNullOrEmpty(term) || term.Length < 2) return Json(new { });

            var suggestions = await _context.Products
                .Where(p => p.Name.ToLower().Contains(term.ToLower()))
                .Select(p => new {
                    id = p.Id,
                    name = p.Name,
                    image = p.ImageUrl ?? "/images/no-image.png"
                })
                .Take(10) // Chỉ lấy 5 kết quả 
                .ToListAsync();

            return Json(suggestions);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null) return NotFound();

            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> SendFeedback(string CustomerName, string Content, int Rating)
        {
            if (string.IsNullOrEmpty(CustomerName) || string.IsNullOrEmpty(Content))
            {
                return RedirectToAction("Index");
            }

            var feedback = new Feedback
            {
                CustomerName = CustomerName,
                Content = Content,
                Rating = Rating,
                CreatedAt = DateTime.Now,
                IsApproved = false
            };

            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cảm ơn bạn! Đánh giá đang chờ Châu xét duyệt.";

            return RedirectToAction("Index");
        }
    }
}