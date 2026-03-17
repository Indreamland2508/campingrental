using BAOCAOWEBNANGCAO.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BAOCAOWEBNANGCAO.Controllers
{
    [Authorize(Roles = "Admin,Staff")] 
    public class AdminController : Controller
    {
        private readonly CampingDbContext _context;

        public AdminController(CampingDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> ManageFeedbacks()
        {
            var feedbacks = await _context.Feedbacks
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();
            return View(feedbacks);
        }

        // Action xử lý Duyệt/Hủy duyệt
        [HttpPost]
        public async Task<IActionResult> ToggleApproval(int id)
        {
            var fb = await _context.Feedbacks.FindAsync(id);
            if (fb != null)
            {
                fb.IsApproved = !fb.IsApproved; // Đảo ngược trạng thái
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ManageFeedbacks));
        }

        // Action xử lý Xóa Feedback rác
        [HttpPost]
        public async Task<IActionResult> DeleteFeedback(int id)
        {
            var fb = await _context.Feedbacks.FindAsync(id);
            if (fb != null)
            {
                _context.Feedbacks.Remove(fb);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ManageFeedbacks));
        }
        public async Task<IActionResult> Index()
        {
            // 1. Tính tổng doanh thu (chỉ tính đơn đã hoàn thành hoặc đã duyệt)
            // Lưu ý: Nếu Database chưa có đơn nào thì Sum sẽ trả về 0
            var totalRevenue = await _context.Orders
                .Where(o => o.Status == "Completed" || o.Status == "Approved")
                .SumAsync(o => o.TotalAmount);

            // 2. Đếm số đơn hàng chưa duyệt (Pending)
            var pendingOrders = await _context.Orders
                .CountAsync(o => o.Status == "Pending");

            // 3. Đếm tổng số sản phẩm đang có
            var totalProducts = await _context.Products.CountAsync();

            // 4. Đếm tổng số đơn hàng hôm nay
            var todayOrders = await _context.Orders
                .CountAsync(o => o.OrderDate.Date == DateTime.Today);

            // Gửi số liệu sang View bằng ViewBag (cách nhanh nhất)
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.PendingOrders = pendingOrders;
            ViewBag.TotalProducts = totalProducts;
            ViewBag.TodayOrders = todayOrders;
            // --- PHẦN MỚI: Chuẩn bị dữ liệu cho Biểu đồ (7 ngày qua) ---
            var sevenDaysAgo = DateTime.Today.AddDays(-6);

            // Lấy dữ liệu từ Database, nhóm theo Ngày
            var revenueData = await _context.Orders
                .Where(o => o.OrderDate >= sevenDaysAgo && (o.Status == "Completed" || o.Status == "Approved"))
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new {
                    Date = g.Key,
                    Revenue = g.Sum(o => o.TotalAmount)
                })
                .ToListAsync();

            // Tạo 2 mảng dữ liệu để gửi sang View (Labels: Ngày, Data: Tiền)
            var labels = new List<string>();
            var data = new List<decimal>();

            for (int i = 0; i < 7; i++)
            {
                var date = sevenDaysAgo.AddDays(i);
                var record = revenueData.FirstOrDefault(r => r.Date == date);

                labels.Add(date.ToString("dd/MM")); // Nhãn ngày (VD: 31/01)
                data.Add(record != null ? record.Revenue : 0); // Nếu ngày đó không bán được thì là 0đ
            }

            // Đóng gói gửi sang View
            ViewBag.ChartLabels = labels;
            ViewBag.ChartData = data;
            return View();

        }
    }
}