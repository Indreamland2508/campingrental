using BAOCAOWEBNANGCAO.Data;
using BAOCAOWEBNANGCAO.Models;
using BAOCAOWEBNANGCAO.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;
using System.Text;

namespace BAOCAOWEBNANGCAO.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly CampingDbContext _context;
        private readonly IMemoryCache _memoryCache;
        private readonly OrderNotificationService _notifications;
        private readonly IDataProtector _orderLookupProtector;

        public HomeController(
            ILogger<HomeController> logger,
            CampingDbContext context,
            IMemoryCache memoryCache,
            OrderNotificationService notifications,
            IDataProtectionProvider dataProtectionProvider)
        {
            _logger = logger;
            _context = context;
            _memoryCache = memoryCache;
            _notifications = notifications;
            _orderLookupProtector = dataProtectionProvider.CreateProtector("CampingRental.OrderLookup.v1");
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
        public async Task<IActionResult> ProductList(
            int? categoryId,
            decimal? minPrice,
            decimal? maxPrice,
            int? guestCount,
            bool availableOnly,
            DateTime? rentalStart,
            DateTime? rentalEnd,
            string? searchString,
            string? sortOrder,
            int page = 1)
        {
            const int pageSize = 9;
            var query = _context.Products.Include(p => p.Category).AsQueryable();

            if (categoryId.HasValue && categoryId > 0)
            {
                query = query.Where(p => p.CategoryId == categoryId);
            }

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var keyword = searchString.Trim().ToLower();
                query = query.Where(product =>
                    product.Name.ToLower().Contains(keyword) ||
                    (product.Description != null && product.Description.ToLower().Contains(keyword)));
            }

            if (minPrice.HasValue)
            {
                query = query.Where(p => p.PricePerDay >= minPrice.Value);
            }
            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.PricePerDay <= maxPrice.Value);
            }

            if (guestCount is > 0)
            {
                query = query.Where(product => product.MaxGuests >= guestCount);
            }

            var filterError = string.Empty;
            Dictionary<int, int> reservedQuantities = new();
            if (availableOnly)
            {
                if (!rentalStart.HasValue || !rentalEnd.HasValue || rentalEnd.Value.Date < rentalStart.Value.Date)
                {
                    filterError = "Để lọc hàng còn theo lịch, hãy chọn ngày nhận và ngày trả hợp lệ.";
                    query = query.Where(product => product.Quantity > 0);
                }
                else
                {
                    var startDate = rentalStart.Value.Date;
                    var endDate = rentalEnd.Value.Date == startDate
                        ? startDate.AddDays(1)
                        : rentalEnd.Value.Date;
                    var now = DateTime.UtcNow;
                    reservedQuantities = await _context.OrderDetails
                        .Where(detail => _context.Orders.Any(order =>
                            order.Id == detail.OrderId &&
                            order.Status != "Cancelled" &&
                            order.Status != "Expired" &&
                            order.RentalStartDate < endDate &&
                            (order.RentalEndDate > startDate ||
                             (order.RentalEndDate == order.RentalStartDate &&
                              order.RentalStartDate >= startDate &&
                              order.RentalStartDate < endDate)) &&
                            (order.PaymentStatus == "Paid" ||
                             order.PaymentStatus == "Deposited" ||
                             (order.ReservationExpiresAt != null && order.ReservationExpiresAt > now))))
                        .GroupBy(detail => detail.ProductId)
                        .Select(group => new { ProductId = group.Key, Quantity = group.Sum(detail => detail.Quantity) })
                        .ToDictionaryAsync(item => item.ProductId, item => item.Quantity);
                }
            }

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

            var matchingProducts = await query.ToListAsync();
            if (availableOnly && string.IsNullOrEmpty(filterError))
            {
                matchingProducts = matchingProducts
                    .Where(product => product.Quantity > reservedQuantities.GetValueOrDefault(product.Id))
                    .ToList();
            }

            var totalItems = matchingProducts.Count;
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            var products = matchingProducts
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentCategory = categoryId;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.GuestCount = guestCount;
            ViewBag.AvailableOnly = availableOnly;
            ViewBag.RentalStart = rentalStart?.ToString("yyyy-MM-dd");
            ViewBag.RentalEnd = rentalEnd?.ToString("yyyy-MM-dd");
            ViewBag.SearchString = searchString;
            ViewBag.SortOrder = sortOrder;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages == 0 ? 1 : totalPages;
            ViewBag.FilterError = filterError;
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
            return View(new OrderTrackingViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OrderTracking(string publicOrderCode, string email)
        {
            var model = new OrderTrackingViewModel
            {
                PublicOrderCode = publicOrderCode?.Trim().ToUpperInvariant() ?? string.Empty,
                Email = email?.Trim() ?? string.Empty
            };
            if (!IsValidOrderReference(model.PublicOrderCode) ||
                !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(model.Email))
            {
                ModelState.AddModelError(string.Empty, "Vui lòng nhập mã đơn hàng và email hợp lệ.");
                return View(model);
            }

            var normalizedEmail = model.Email.ToUpperInvariant();
            var clientKey = $"{HttpContext.Connection.RemoteIpAddress}:{model.PublicOrderCode}:{normalizedEmail}";
            var cacheKey = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(clientKey)));
            if (_memoryCache.TryGetValue($"order-lookup:{cacheKey}", out _))
            {
                model.CodeSent = true;
                model.Message = "Nếu thông tin khớp đơn hàng, mã xác minh sẽ được gửi đến email đã đăng ký. Vui lòng chờ trước khi yêu cầu mã mới.";
                return View(model);
            }

            _memoryCache.Set($"order-lookup:{cacheKey}", true, TimeSpan.FromSeconds(60));
            var order = await FindOrderForLookupAsync(model.PublicOrderCode, normalizedEmail);

            if (order != null)
            {
                var now = DateTime.UtcNow;
                var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
                var previousChallenges = await _context.OrderLookupChallenges
                    .Where(challenge => challenge.OrderId == order.Id && challenge.Email == normalizedEmail)
                    .ToListAsync();
                _context.OrderLookupChallenges.RemoveRange(previousChallenges);
                var challenge = new OrderLookupChallenge
                {
                    OrderId = order.Id,
                    Email = normalizedEmail,
                    ProtectedCode = _orderLookupProtector.Protect(code),
                    CreatedAt = now,
                    ExpiresAt = now.AddMinutes(10)
                };
                _context.OrderLookupChallenges.Add(challenge);
                await _context.SaveChangesAsync();

                try
                {
                    await _notifications.OrderLookupCodeAsync(order, code);
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Could not send order lookup code for order {OrderId}.", order.Id);
                }
            }

            model.CodeSent = true;
            model.Message = "Nếu thông tin khớp đơn hàng, mã xác minh sẽ được gửi đến email đã đăng ký.";
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOrderTrackingCode(OrderTrackingViewModel model)
        {
            model.PublicOrderCode = model.PublicOrderCode?.Trim().ToUpperInvariant() ?? string.Empty;
            model.Email = model.Email?.Trim() ?? string.Empty;
            if (!IsValidOrderReference(model.PublicOrderCode) ||
                string.IsNullOrWhiteSpace(model.Code) ||
                !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(model.Email))
            {
                ModelState.AddModelError(string.Empty, "Vui lòng kiểm tra lại mã đơn, email và mã xác minh.");
                model.CodeSent = true;
                return View("OrderTracking", model);
            }

            var normalizedEmail = model.Email.ToUpperInvariant();
            var now = DateTime.UtcNow;
            var order = await FindOrderForLookupAsync(model.PublicOrderCode, normalizedEmail);
            var challenge = order == null
                ? null
                : await _context.OrderLookupChallenges
                    .Where(candidate =>
                        candidate.OrderId == order.Id &&
                        candidate.Email == normalizedEmail &&
                        candidate.ConsumedAt == null &&
                        candidate.ExpiresAt > now &&
                        candidate.AttemptCount < 5)
                    .OrderByDescending(candidate => candidate.CreatedAt)
                    .FirstOrDefaultAsync();

            if (order == null || challenge == null || !MatchesProtectedCode(challenge, model.Code))
            {
                if (challenge != null)
                {
                    challenge.AttemptCount++;
                    if (challenge.AttemptCount >= 5)
                    {
                        challenge.ConsumedAt = now;
                    }
                    await _context.SaveChangesAsync();
                }

                ModelState.AddModelError(string.Empty, "Mã xác minh không hợp lệ hoặc đã hết hạn.");
                model.CodeSent = true;
                return View("OrderTracking", model);
            }

            challenge.ConsumedAt = now;
            await _context.SaveChangesAsync();

            HttpContext.Session.SetInt32("VerifiedOrderId", order.Id);
            HttpContext.Session.SetString("VerifiedOrderEmail", normalizedEmail);
            await _context.Entry(order).Collection(candidate => candidate.OrderDetails).Query()
                .Include(detail => detail.Product)
                .LoadAsync();

            return View("OrderTracking", new OrderTrackingViewModel
            {
                PublicOrderCode = order.PublicOrderCode,
                Email = model.Email,
                Order = order,
                CanRequestCancellation = order.Status != "Cancelled" &&
                    order.Status != "Expired" &&
                    order.Status != "Completed" &&
                    order.CancellationRequestedAt == null,
                CanRequestRentalChange = (order.Status == "Pending" || order.Status == "Approved") &&
                    order.RentalStartDate.Date > VietnamToday() &&
                    (order.RentalChangeRequestedAt == null || order.RentalChangeResolvedAt != null) &&
                    order.CancellationRequestedAt == null
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestCancellation(string publicOrderCode, string reason)
        {
            var verifiedOrderId = HttpContext.Session.GetInt32("VerifiedOrderId");
            var verifiedEmail = HttpContext.Session.GetString("VerifiedOrderEmail");
            if (!verifiedOrderId.HasValue || string.IsNullOrEmpty(verifiedEmail))
            {
                return Unauthorized();
            }

            var order = await _context.Orders.FirstOrDefaultAsync(candidate =>
                candidate.PublicOrderCode == publicOrderCode &&
                candidate.Id == verifiedOrderId.Value &&
                candidate.CustomerEmail.ToUpper() == verifiedEmail);
            if (order == null)
            {
                return NotFound();
            }

            if (order.Status == "Cancelled" || order.Status == "Completed" || order.Status == "Expired" ||
                order.CancellationRequestedAt != null)
            {
                TempData["ErrorMessage"] = "Đơn hàng không thể gửi yêu cầu hủy ở trạng thái hiện tại.";
                return RedirectToAction(nameof(OrderTracking));
            }

            if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 500)
            {
                TempData["ErrorMessage"] = "Vui lòng nhập lý do hủy (tối đa 500 ký tự).";
                return RedirectToAction(nameof(OrderTracking));
            }

            order.CancellationRequestedAt = DateTime.UtcNow;
            order.CancellationReason = reason.Trim();
            await _context.SaveChangesAsync();
            await _notifications.CancellationRequestedAsync(order);
            TempData["SuccessMessage"] = "Yêu cầu hủy đã được gửi để nhân viên xử lý. Việc hoàn cọc (nếu có) cần được nhân viên xác nhận.";
            return RedirectToAction(nameof(OrderTracking));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitRefundBankDetails(
            string publicOrderCode,
            string bankName,
            string accountNumber,
            string accountHolder)
        {
            var verifiedOrderId = HttpContext.Session.GetInt32("VerifiedOrderId");
            var verifiedEmail = HttpContext.Session.GetString("VerifiedOrderEmail");
            if (!verifiedOrderId.HasValue || string.IsNullOrEmpty(verifiedEmail))
            {
                return Unauthorized();
            }

            var order = await _context.Orders.FirstOrDefaultAsync(candidate =>
                candidate.PublicOrderCode == publicOrderCode &&
                candidate.Id == verifiedOrderId.Value &&
                candidate.CustomerEmail.ToUpper() == verifiedEmail);
            if (order == null)
            {
                return NotFound();
            }

            bankName = bankName?.Trim() ?? string.Empty;
            accountNumber = accountNumber?.Trim() ?? string.Empty;
            accountHolder = accountHolder?.Trim() ?? string.Empty;

            if ((order.Status != "Cancelled" && order.Status != "Expired") ||
                order.RefundAmount <= 0 ||
                (order.RefundStatus != "DetailsRequired" && order.RefundStatus != "Pending"))
            {
                TempData["ErrorMessage"] = "Đơn này hiện không yêu cầu thông tin nhận hoàn tiền.";
                return RedirectToAction(nameof(OrderTracking));
            }

            if (bankName.Length is < 2 or > 100 ||
                !System.Text.RegularExpressions.Regex.IsMatch(accountNumber, "^[0-9]{6,34}$") ||
                accountHolder.Length is < 2 or > 100)
            {
                TempData["ErrorMessage"] = "Vui lòng nhập tên ngân hàng, số tài khoản gồm 6-34 chữ số và tên chủ tài khoản hợp lệ.";
                return RedirectToAction(nameof(OrderTracking));
            }

            order.RefundBankName = bankName;
            order.RefundBankAccountNumber = accountNumber;
            order.RefundBankAccountHolder = accountHolder;
            order.RefundStatus = "Pending";
            await _context.SaveChangesAsync();
            await _notifications.RefundDetailsReceivedAsync(order);
            TempData["SuccessMessage"] = "Đã nhận thông tin tài khoản. Nhân viên sẽ chuyển khoản hoàn tiền và cập nhật mã giao dịch.";
            return RedirectToAction(nameof(OrderTracking));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestRentalChange(
            string publicOrderCode,
            DateTime rentalStart,
            DateTime rentalEnd,
            string reason)
        {
            var verifiedOrderId = HttpContext.Session.GetInt32("VerifiedOrderId");
            var verifiedEmail = HttpContext.Session.GetString("VerifiedOrderEmail");
            if (!verifiedOrderId.HasValue || string.IsNullOrEmpty(verifiedEmail))
            {
                return Unauthorized();
            }

            var order = await _context.Orders.FirstOrDefaultAsync(candidate =>
                candidate.PublicOrderCode == publicOrderCode &&
                candidate.Id == verifiedOrderId.Value &&
                candidate.CustomerEmail.ToUpper() == verifiedEmail);
            if (order == null)
            {
                return NotFound();
            }

            if ((order.Status != "Pending" && order.Status != "Approved") ||
                order.RentalStartDate.Date <= VietnamToday() ||
                order.CancellationRequestedAt != null ||
                (order.RentalChangeRequestedAt != null && order.RentalChangeResolvedAt == null))
            {
                TempData["ErrorMessage"] = "Đơn hàng hiện không thể gửi yêu cầu đổi lịch.";
                return RedirectToAction(nameof(OrderTracking));
            }

            if (rentalStart.Date < VietnamToday() ||
                rentalEnd.Date < rentalStart.Date ||
                string.IsNullOrWhiteSpace(reason) ||
                reason.Trim().Length > 500)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn ngày hợp lệ và nhập lý do đổi lịch (tối đa 500 ký tự).";
                return RedirectToAction(nameof(OrderTracking));
            }

            if (rentalStart.Date == order.RentalStartDate.Date &&
                rentalEnd.Date == order.RentalEndDate.Date)
            {
                TempData["ErrorMessage"] = "Lịch mới trùng với lịch hiện tại.";
                return RedirectToAction(nameof(OrderTracking));
            }

            order.RentalChangeRequestedAt = DateTime.UtcNow;
            order.RequestedRentalStartDate = DateTime.SpecifyKind(rentalStart.Date, DateTimeKind.Unspecified);
            order.RequestedRentalEndDate = DateTime.SpecifyKind(rentalEnd.Date, DateTimeKind.Unspecified);
            order.RentalChangeReason = reason.Trim();
            order.RentalChangeResolvedAt = null;
            order.RentalChangeResolution = null;
            await _context.SaveChangesAsync();
            await _notifications.RentalChangeRequestedAsync(order);
            TempData["SuccessMessage"] = "Đã gửi yêu cầu đổi lịch. Lịch hiện tại chỉ thay đổi sau khi nhân viên kiểm tra và duyệt.";
            return RedirectToAction(nameof(OrderTracking));
        }

        private bool MatchesProtectedCode(OrderLookupChallenge challenge, string code)
        {
            try
            {
                var expected = _orderLookupProtector.Unprotect(challenge.ProtectedCode);
                return CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(expected),
                    Encoding.UTF8.GetBytes(code.Trim()));
            }
            catch (CryptographicException)
            {
                return false;
            }
        }

        private Task<Order?> FindOrderForLookupAsync(string reference, string normalizedEmail)
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(reference, "^CR-[A-F0-9]{16}$"))
            {
                return _context.Orders.FirstOrDefaultAsync(candidate =>
                    candidate.PublicOrderCode == reference &&
                    candidate.CustomerEmail.ToUpper() == normalizedEmail);
            }

            return int.TryParse(reference, out var legacyId)
                ? _context.Orders.FirstOrDefaultAsync(candidate =>
                    candidate.Id == legacyId &&
                    candidate.CustomerEmail.ToUpper() == normalizedEmail)
                : Task.FromResult<Order?>(null);
        }

        private static bool IsValidOrderReference(string value) =>
            System.Text.RegularExpressions.Regex.IsMatch(value, "^CR-[A-F0-9]{16}$") ||
            System.Text.RegularExpressions.Regex.IsMatch(value, "^[0-9]{1,10}$");

        private static DateTime VietnamToday()
        {
            var zoneId = OperatingSystem.IsWindows() ? "SE Asia Standard Time" : "Asia/Ho_Chi_Minh";
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(zoneId)).Date;
        }

        // GET: /Home/FAQ
        public IActionResult FAQ()
        {
            return View();
        }
        public IActionResult RentalPolicy()
        {
            return View();
        }
        public IActionResult PrivacyPolicy()
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