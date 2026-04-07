using BAOCAOWEBNANGCAO.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Runtime.InteropServices;
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

        private static TimeZoneInfo GetVietnamTimeZone()
        {
            var timeZoneId = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "SE Asia Standard Time"
                : "Asia/Ho_Chi_Minh";

            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }

        private static DateTime ConvertVietnamDateToUtc(DateTime vietnamDate)
        {
            return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(vietnamDate, DateTimeKind.Unspecified), GetVietnamTimeZone());
        }

        public async Task<IActionResult> ManageFeedbacks()
        {
            var feedbacks = await _context.Feedbacks
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();
            return View(feedbacks);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleApproval(int id)
        {
            var fb = await _context.Feedbacks.FindAsync(id);
            if (fb != null)
            {
                fb.IsApproved = !fb.IsApproved;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ManageFeedbacks));
        }

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
            var totalRevenue = await _context.Orders
                .Where(o => o.Status == "Completed" || o.Status == "Approved")
                .SumAsync(o => o.TotalAmount);

            var pendingOrders = await _context.Orders
                .CountAsync(o => o.Status == "Pending");

            var totalProducts = await _context.Products.CountAsync();

            var vietnamToday = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, GetVietnamTimeZone()).Date;
            var vietnamTomorrow = vietnamToday.AddDays(1);
            var todayStartUtc = ConvertVietnamDateToUtc(vietnamToday);
            var todayEndUtc = ConvertVietnamDateToUtc(vietnamTomorrow);

            var todayOrders = await _context.Orders
                .CountAsync(o => o.OrderDate >= todayStartUtc && o.OrderDate < todayEndUtc);

            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.PendingOrders = pendingOrders;
            ViewBag.TotalProducts = totalProducts;
            ViewBag.TodayOrders = todayOrders;

            var sevenDaysAgo = vietnamToday.AddDays(-6);
            var sevenDaysAgoStartUtc = ConvertVietnamDateToUtc(sevenDaysAgo);
            var tomorrowStartUtc = ConvertVietnamDateToUtc(vietnamTomorrow);

            var revenueOrders = await _context.Orders
                .Where(o => o.OrderDate >= sevenDaysAgoStartUtc && o.OrderDate < tomorrowStartUtc && (o.Status == "Completed" || o.Status == "Approved"))
                .ToListAsync();

            var revenueData = revenueOrders
                .GroupBy(o => o.OrderDate.ToVietnamTime().Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Revenue = g.Sum(o => o.TotalAmount)
                })
                .ToList();

            var labels = new List<string>();
            var data = new List<decimal>();

            for (int i = 0; i < 7; i++)
            {
                var date = sevenDaysAgo.AddDays(i);
                var record = revenueData.FirstOrDefault(r => r.Date == date);

                labels.Add(date.ToString("dd/MM"));
                data.Add(record != null ? record.Revenue : 0);
            }

            ViewBag.ChartLabels = labels;
            ViewBag.ChartData = data;
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> ExportReport()
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var vietnamToday = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, GetVietnamTimeZone());
            var startOfMonth = new DateTime(vietnamToday.Year, vietnamToday.Month, 1);
            var startOfMonthUtc = ConvertVietnamDateToUtc(startOfMonth);

            var currentMonthOrders = await _context.Orders
                .Where(o => o.OrderDate >= startOfMonthUtc)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

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
                            column.Item().Text($"Ngày lập: {vietnamToday:dd/MM/yyyy HH:mm}").FontSize(10);
                            column.Item().Text($"Kỳ báo cáo: Tháng {vietnamToday.Month}/{vietnamToday.Year}").FontSize(10);
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
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(50);
                                columns.RelativeColumn();
                                columns.ConstantColumn(100);
                                columns.ConstantColumn(90);
                                columns.ConstantColumn(100);
                                columns.ConstantColumn(120);
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
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(order.OrderDate.ToVietnamTime().ToString("dd/MM/yyyy"));
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{order.TotalAmount:N0} đ");

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
            return File(pdfBytes, "application/pdf", $"BaoCao_CampingRental_{vietnamToday:MMyyyy}.pdf");
        }
    }
}