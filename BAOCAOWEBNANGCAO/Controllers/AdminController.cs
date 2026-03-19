using BAOCAOWEBNANGCAO.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
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
        [HttpGet]
        [HttpGet]
        public async Task<IActionResult> ExportReport()
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var today = DateTime.Now;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);

            var currentMonthOrders = await _context.Orders
                .Where(o => o.OrderDate >= startOfMonth)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            // SỬA LẠI LOGIC TÍNH TOÁN (Chỉ cộng tiền khách ĐÃ THỰC SỰ TRẢ)
            // Nếu khách đã trả đủ (Paid) -> Cộng TotalAmount
            // Nếu khách mới cọc (Deposited) -> Chỉ cộng DepositAmount
            // Nếu khách chưa trả (Unpaid) -> Không cộng đồng nào
            var totalRevenue = currentMonthOrders.Sum(o =>
                o.PaymentStatus == "Paid" ? o.TotalAmount :
                (o.PaymentStatus == "Deposited" ? o.DepositAmount : 0)
            );

            var totalOrders = currentMonthOrders.Count;
            var completedOrders = currentMonthOrders.Count(o => o.Status == "Completed");

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    // Lật ngang khổ giấy (Landscape) để có thêm chỗ hiển thị cột Trạng thái
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(column =>
                        {
                            column.Item().Text("CAMPING RENTAL").FontSize(24).SemiBold().FontColor(Colors.Green.Darken3);
                            column.Item().Text("Đại học Điện Lực (EPU), Hà Nội").FontSize(10).FontColor(Colors.Grey.Medium);
                            column.Item().Text("Email: contact@campingrental.vn | SĐT: 0982.412.005").FontSize(10).FontColor(Colors.Grey.Medium);
                        });

                        row.ConstantItem(250).AlignRight().Column(column =>
                        {
                            column.Item().Text("BÁO CÁO DOANH THU").FontSize(16).Bold().FontColor(Colors.Black);
                            column.Item().Text($"Ngày lập: {today:dd/MM/yyyy HH:mm}").FontSize(10);
                            column.Item().Text($"Kỳ báo cáo: Tháng {today.Month}/{today.Year}").FontSize(10);
                        });
                    });

                    page.Content().PaddingVertical(1, Unit.Centimetre).Column(column =>
                    {
                        column.Item().PaddingBottom(15).Background(Colors.Grey.Lighten4).Padding(10).Row(row =>
                        {
                            row.RelativeItem().AlignCenter().Column(c =>
                            {
                                c.Item().Text("TỔNG ĐƠN HÀNG").FontSize(10).FontColor(Colors.Grey.Darken1);
                                c.Item().Text($"{totalOrders}").FontSize(16).Bold().FontColor(Colors.Black);
                            });
                            row.RelativeItem().AlignCenter().Column(c =>
                            {
                                c.Item().Text("ĐÃ HOÀN THÀNH").FontSize(10).FontColor(Colors.Grey.Darken1);
                                c.Item().Text($"{completedOrders}").FontSize(16).Bold().FontColor(Colors.Green.Darken2);
                            });
                            row.RelativeItem().AlignCenter().Column(c =>
                            {
                                c.Item().Text("DOANH THU THỰC NHẬN").FontSize(10).FontColor(Colors.Grey.Darken1);
                                c.Item().Text($"{totalRevenue:N0} VNĐ").FontSize(16).Bold().FontColor(Colors.Red.Medium);
                            });
                        });

                        column.Item().PaddingBottom(10).Text("CHI TIẾT CÁC GIAO DỊCH TRONG THÁNG").Bold().FontSize(12).FontColor(Colors.Green.Darken3);

                        column.Item().Table(table =>
                        {
                            // THÊM CỘT TRẠNG THÁI VÀ ĐIỀU CHỈNH KÍCH THƯỚC
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(50);  // Mã
                                columns.RelativeColumn();    // Khách hàng
                                columns.ConstantColumn(100); // SĐT
                                columns.ConstantColumn(90);  // Ngày đặt
                                columns.ConstantColumn(100); // Tổng giá trị đơn
                                columns.ConstantColumn(120); // Trạng thái Thanh toán (MỚI)
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Green.Darken3).Padding(5).Text("Mã ĐH").FontColor(Colors.White).SemiBold();
                                header.Cell().Background(Colors.Green.Darken3).Padding(5).Text("Khách hàng").FontColor(Colors.White).SemiBold();
                                header.Cell().Background(Colors.Green.Darken3).Padding(5).Text("Số ĐT").FontColor(Colors.White).SemiBold();
                                header.Cell().Background(Colors.Green.Darken3).Padding(5).Text("Ngày đặt").FontColor(Colors.White).SemiBold();
                                header.Cell().Background(Colors.Green.Darken3).Padding(5).AlignRight().Text("Tổng đơn").FontColor(Colors.White).SemiBold();
                                header.Cell().Background(Colors.Green.Darken3).Padding(5).AlignCenter().Text("Tài chính").FontColor(Colors.White).SemiBold();
                            });

                            foreach (var order in currentMonthOrders)
                            {
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"#{order.Id}");
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(order.CustomerName);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(order.CustomerPhone);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(order.OrderDate.ToString("dd/MM/yyyy"));
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{order.TotalAmount:N0} đ");

                                // XỬ LÝ TEXT VÀ MÀU CHO CỘT TRẠNG THÁI
                                var statusCell = table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignCenter();

                                if (order.PaymentStatus == "Paid")
                                {
                                    statusCell.Text("Đã thu đủ").FontColor(Colors.Green.Darken2).SemiBold();
                                }
                                else if (order.PaymentStatus == "Deposited")
                                {
                                    statusCell.Text($"Đã cọc: {order.DepositAmount:N0}đ").FontColor(Colors.Orange.Darken2).SemiBold();
                                }
                                else
                                {
                                    statusCell.Text("Chưa cọc").FontColor(Colors.Red.Medium).Italic();
                                }
                            }
                        });

                        // Thêm ghi chú nhỏ ở dưới bảng
                        column.Item().PaddingTop(10).Text("* Doanh thu thực nhận = Tiền khách đã cọc + Tiền những đơn đã thanh toán đủ.").FontSize(9).Italic().FontColor(Colors.Grey.Medium);
                    });

                    page.Footer().Column(column =>
                    {
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().AlignCenter().Text("Người lập báo cáo\n\n\n\nHoàng Châu").FontSize(11).Italic();
                            row.RelativeItem().AlignCenter().Text("Giám đốc\n\n\n\nXuân Trường").FontSize(11).Italic();
                        });
                        column.Item().PaddingTop(20).AlignCenter().Text(x =>
                        {
                            x.Span("Trang ");
                            x.CurrentPageNumber();
                            x.Span(" / ");
                            x.TotalPages();
                        });
                    });
                });
            });

            var pdfBytes = document.GeneratePdf();
            return File(pdfBytes, "application/pdf", $"BaoCao_CampingRental_{today:MMyyyy}.pdf");
        }
    }
}