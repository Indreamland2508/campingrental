using BAOCAOWEBNANGCAO.Data;
using BAOCAOWEBNANGCAO.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BAOCAOWEBNANGCAO.Areas.Customer.Controllers
{
    [Area("Customer")]
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
        // GET: /Home/ProductList
        public async Task<IActionResult> ProductList(int? categoryId, decimal? minPrice, decimal? maxPrice, string sortOrder, int page = 1)
        {
            int pageSize = 9; // Số lượng lều/đồ hiển thị trên 1 trang (Châu tự chỉnh nhé)

            // 1. Lấy toàn bộ sản phẩm lên để chuẩn bị "sàng lọc"
            var query = _context.Products.Include(p => p.Category).AsQueryable();

            // 2. Lọc theo Danh mục (Nếu khách có click vào Menu bên trái)
            if (categoryId.HasValue && categoryId > 0)
            {
                query = query.Where(p => p.CategoryId == categoryId);
            }

            // 3. Lọc theo Khoảng giá (Khi khách nhập Từ... Đến...)
            if (minPrice.HasValue)
            {
                query = query.Where(p => p.PricePerDay >= minPrice.Value);
            }
            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.PricePerDay <= maxPrice.Value);
            }

            // 4. Sắp xếp giá (Cao đến Thấp / Thấp đến Cao)
            switch (sortOrder)
            {
                case "desc":
                    query = query.OrderByDescending(p => p.PricePerDay);
                    break;
                case "asc":
                    query = query.OrderBy(p => p.PricePerDay);
                    break;
                default:
                    query = query.OrderByDescending(p => p.Id); // Mặc định: Sản phẩm mới nhất lên đầu
                    break;
            }

            // 5. Phân trang (Trang 1, 2, 3...)
            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Ép số trang không được vượt quá giới hạn
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // 6. Lưu lại các trạng thái hiện tại để đẩy ra View (nhằm giữ nguyên ô Text đang gõ)
            ViewBag.CurrentCategory = categoryId;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.SortOrder = sortOrder;

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages == 0 ? 1 : totalPages;

            // Gửi cả danh sách Category ra để hiển thị cái cột bên trái
            ViewBag.Categories = await _context.Categories.ToListAsync();

            return View(products);
        }

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
        // ==========================================
        // TRA CỨU ĐƠN HÀNG DÀNH CHO KHÁCH KHÔNG CẦN LOGIN
        // ==========================================

        [HttpGet]
        public IActionResult OrderTracking()
        {
            return View(); // Chỉ hiện cái Form nhập liệu
        }

        [HttpPost]
        public async Task<IActionResult> OrderTracking(int? orderId, string phone)
        {
            if (orderId == null || string.IsNullOrEmpty(phone))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ Mã đơn hàng và Số điện thoại.";
                return View();
            }

            // Truy vấn đơn hàng: Phải khớp CẢ ID LẪN SỐ ĐIỆN THOẠI mới cho xem
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product) // Kéo theo món đồ để hiển thị tên
                .FirstOrDefaultAsync(o => o.Id == orderId && o.CustomerPhone == phone.Trim());

            if (order == null)
            {
                ViewBag.Error = "Không tìm thấy đơn hàng! Vui lòng kiểm tra lại Mã đơn hoặc Số điện thoại.";
                return View();
            }

            // Nếu tìm thấy, ném nguyên cái đơn hàng ra View để hiển thị
            return View(order);
        }
        // GET: /Home/FAQ
        public IActionResult FAQ()
        {
            return View();
        }
        // GET: /Home/CamNang
        public IActionResult CamNang()
        {
            return View();
        }
        // GET: /Home/DiaDiem
        public IActionResult DiaDiem()
        {
            return View();
        }
    }
}