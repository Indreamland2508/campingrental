using BAOCAOWEBNANGCAO.Data;
using BAOCAOWEBNANGCAO.Models;
using BAOCAOWEBNANGCAO.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Runtime.InteropServices;

namespace BAOCAOWEBNANGCAO.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class CartController : Controller
    {
        private readonly CampingDbContext _context;
        private readonly OrderReservationService _reservationService;
        private readonly OrderNotificationService _notifications;
        private readonly TurnstileVerificationService _turnstile;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private const string CartSessionKey = "GioHangCuaToi";

        public CartController(
            CampingDbContext context,
            OrderReservationService reservationService,
            OrderNotificationService notifications,
            TurnstileVerificationService turnstile,
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            _context = context;
            _reservationService = reservationService;
            _notifications = notifications;
            _turnstile = turnstile;
            _configuration = configuration;
            _environment = environment;
        }

        // GET: Xem giỏ hàng
        public IActionResult Index()
        {
            var cart = GetCartFromSession();
            ViewBag.TotalAmount = cart.Sum(item => item.TotalPrice);
            return View(cart);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddToCart(int productId, int quantity)
        {
            if (productId <= 0 || quantity <= 0)
            {
                TempData["ErrorMessage"] = "Sản phẩm hoặc số lượng không hợp lệ.";
                return RedirectToAction("ProductList", "Home");
            }

            var cart = GetCartFromSession();
            var cartItem = cart.FirstOrDefault(c => c.Product.Id == productId);
            var product = _context.Products.Find(productId);

            if (product == null)
            {
                TempData["ErrorMessage"] = "Sản phẩm không còn tồn tại.";
                return RedirectToAction("ProductList", "Home");
            }

            if (cartItem != null)
            {
                if (cartItem.Quantity + quantity > product.Quantity)
                {
                    TempData["ErrorMessage"] = "Số lượng vượt quá tồn kho hiện tại.";
                    return RedirectToAction(nameof(Index));
                }
                cartItem.Quantity += quantity;
                cartItem.Product = product;
            }
            else
            {
                if (quantity > product.Quantity)
                {
                    TempData["ErrorMessage"] = "Số lượng vượt quá tồn kho hiện tại.";
                    return RedirectToAction(nameof(Index));
                }
                cart.Add(new CartItem { Product = product, Quantity = quantity });
            }

            SaveCartToSession(cart);
            HttpContext.Session.SetInt32("CartCount", GetTotalItemsInCart());

            return RedirectToAction("Index");
        }

        private int GetTotalItemsInCart()
        {
            var cart = GetCartFromSession();
            return cart.Sum(item => item.Quantity);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveFromCart(int productId)
        {
            var cart = GetCartFromSession();
            var itemToRemove = cart.FirstOrDefault(c => c.Product.Id == productId);

            if (itemToRemove != null)
            {
                cart.Remove(itemToRemove);
                SaveCartToSession(cart);
                HttpContext.Session.SetInt32("CartCount", GetTotalItemsInCart()); // Cập nhật lại số lượng trên menu
            }

            return RedirectToAction(nameof(Index));
        }

        // --- CÁC HÀM PHỤ TRỢ (HELPER) --- 
        private List<CartItem> GetCartFromSession()
        {
            var session = HttpContext.Session;
            string? json = session.GetString(CartSessionKey);

            if (!string.IsNullOrEmpty(json))
            {
                return JsonConvert.DeserializeObject<List<CartItem>>(json) ?? new List<CartItem>();
            }
            return new List<CartItem>();
        }

        private void SaveCartToSession(List<CartItem> cart)
        {
            var session = HttpContext.Session;
            string json = JsonConvert.SerializeObject(cart);
            session.SetString(CartSessionKey, json);
        }

        // GET: Hiển thị trang thanh toán
        [HttpGet]
        public IActionResult Checkout()
        {
            var cart = GetCartFromSession();
            if (cart.Count == 0) return RedirectToAction("Index");

            ViewBag.TotalAmount = cart.Sum(item => item.TotalPrice);
            ViewBag.TurnstileSiteKey = _turnstile.SiteKey;
            PopulatePaymentConfigurationStatus();
            return View(cart);
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(
            Order order,
            DateTime rentalStart,
            DateTime rentalEnd,
            bool acceptRentalPolicy,
            bool acceptPrivacyPolicy)
        {
            var cart = GetCartFromSession();

            ModelState.Remove(nameof(Order.PublicOrderCode));

            if (cart.Count > 0 && !GetPaymentAccount().HasValue)
            {
                ModelState.AddModelError(string.Empty, GetPaymentConfigurationMessage());
            }

            if (!acceptRentalPolicy)
            {
                ModelState.AddModelError(string.Empty, "Vui lòng xác nhận đã đọc chính sách thuê và hủy đơn.");
            }

            if (!acceptPrivacyPolicy)
            {
                ModelState.AddModelError(string.Empty, "Vui lòng đọc và đồng ý với chính sách bảo mật để tiếp tục.");
            }

            if (cart.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "Giỏ hàng đang trống.");
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(order.CustomerPhone ?? string.Empty, "^[0-9]{9,10}$"))
            {
                ModelState.AddModelError("CustomerPhone", "Số điện thoại phải gồm 9-10 chữ số.");
            }

            if (rentalEnd.Date < rentalStart.Date)
            {
                ModelState.AddModelError("rentalEnd", "Ngày trả phải bằng hoặc sau ngày nhận.");
            }

            if (ModelState.IsValid)
            {
                var captchaToken = Request.Form["cf-turnstile-response"].ToString();
                if (!await _turnstile.VerifyAsync(
                        captchaToken,
                        HttpContext.Connection.RemoteIpAddress?.ToString(),
                        HttpContext.RequestAborted))
                {
                    ModelState.AddModelError(string.Empty, "Không xác minh được CAPTCHA. Hãy thử lại.");
                }
            }

            if (!ModelState.IsValid)
            {
                PopulateCheckoutViewData(order, rentalStart, rentalEnd, cart);
                return View(cart);
            }

            Order orderEntity;
            try
            {
                order.PrivacyConsentAt = DateTime.UtcNow;
                order.PrivacyConsentVersion = "2026-11-07-v2";
                order.RentalPolicyAcceptedAt = DateTime.UtcNow;
                orderEntity = await _reservationService.CreateOrderAsync(order, rentalStart, rentalEnd, cart);
            }
            catch (InvalidOperationException exception)
            {
                ModelState.AddModelError(string.Empty, exception.Message);
                PopulateCheckoutViewData(order, rentalStart, rentalEnd, cart);
                return View(cart);
            }

            HttpContext.Session.Remove(CartSessionKey);
            HttpContext.Session.Remove("CartCount");
            HttpContext.Session.SetInt32("GuestOrderId", orderEntity.Id);
            await _notifications.OrderCreatedAsync(orderEntity);
            return RedirectToAction(nameof(Payment), new { code = orderEntity.PublicOrderCode });
        }

        // --- CÁC HÀM XỬ LÝ THANH TOÁN SEPAY & GIAO DIỆN ---

        // 1. GET: Trang hiển thị mã QR (Chờ quét)
        public IActionResult Payment(string code)
        {
            var order = _context.Orders.FirstOrDefault(candidate => candidate.PublicOrderCode == code);
            var id = order?.Id ?? 0;
            if (order == null ||
                !CanAccessGuestOrder(id) ||
                (order.PaymentStatus != "Unpaid" && order.PaymentStatus != "Partial") ||
                order.Status == "Expired" ||
                order.Status == "Cancelled" ||
                !order.ReservationExpiresAt.HasValue ||
                order.ReservationExpiresAt.Value <= DateTime.UtcNow)
            {
                return RedirectToAction("OrderTracking", "Home");
            }

            var amountToPay = (long)Math.Max(0, order.DepositAmount - order.AmountPaid);
            var paymentAccount = GetPaymentAccount();
            if (paymentAccount is { } account && amountToPay > 0)
            {
                var transferContent = $"DH{order.PublicOrderCode.Replace("-", string.Empty)}";
                var qrPath = $"https://img.vietqr.io/image/{Uri.EscapeDataString(account.BankId)}-{Uri.EscapeDataString(account.AccountNumber)}-compact.png";
                var query = $"amount={amountToPay}&addInfo={Uri.EscapeDataString(transferContent)}&accountName={Uri.EscapeDataString(account.AccountName)}";
                ViewBag.PaymentQrUrl = $"{qrPath}?{query}";
            }

            return View(order);
        }

        // 2. API nhỏ để giao diện liên tục hỏi thăm trạng thái thanh toán
        [HttpGet]
        public IActionResult CheckPaymentStatus(string code)
        {
            var order = _context.Orders.FirstOrDefault(candidate => candidate.PublicOrderCode == code);
            if (order == null) return NotFound();
            if (!CanAccessGuestOrder(order.Id))
            {
                return Unauthorized();
            }

            return Json(new { status = order.PaymentStatus });
        }

        // 3. GET: Trang thông báo mua thành công (Chỉ hiện khi đã thanh toán)
        public IActionResult OrderSuccess(string code)
        {
            var order = _context.Orders.FirstOrDefault(candidate => candidate.PublicOrderCode == code);
            if (order == null || !CanAccessGuestOrder(order.Id) ||
                (order.PaymentStatus != "Deposited" && order.PaymentStatus != "Paid"))
            {
                return RedirectToAction("OrderTracking", "Home");
            }

            return View(order);
        }

        private bool CanAccessGuestOrder(int orderId) =>
            HttpContext.Session.GetInt32("GuestOrderId") == orderId ||
            HttpContext.Session.GetInt32("VerifiedOrderId") == orderId;

        private void PopulateCheckoutViewData(
            Order order,
            DateTime rentalStart,
            DateTime rentalEnd,
            List<CartItem> cart)
        {
            ViewBag.TotalAmount = cart.Sum(item => item.TotalPrice);
            ViewBag.CustomerName = order.CustomerName;
            ViewBag.CustomerPhone = order.CustomerPhone;
            ViewBag.CustomerEmail = order.CustomerEmail;
            ViewBag.ShippingAddress = order.ShippingAddress;
            ViewBag.Note = order.Note;
            ViewBag.RentalStart = rentalStart.ToString("yyyy-MM-dd");
            ViewBag.RentalEnd = rentalEnd.ToString("yyyy-MM-dd");
            ViewBag.TurnstileSiteKey = _turnstile.SiteKey;
            PopulatePaymentConfigurationStatus();
        }

        private void PopulatePaymentConfigurationStatus()
        {
            ViewBag.PaymentConfigured = GetPaymentAccount().HasValue;
            ViewBag.PaymentConfigurationMessage = GetPaymentConfigurationMessage();
        }

        private string GetPaymentConfigurationMessage()
        {
            if (GetPaymentAccount().HasValue)
            {
                return string.Empty;
            }

            if (!_environment.IsDevelopment())
            {
                return $"Ứng dụng đang chạy môi trường {_environment.EnvironmentName}; User Secrets chỉ tự nạp trong Development. Hãy chạy lại với ASPNETCORE_ENVIRONMENT=Development hoặc cấu hình Payment:BankId, Payment:AccountNumber và Payment:AccountName trong secret manager của môi trường này.";
            }

            return "Thiếu hoặc sai cấu hình Payment:BankId, Payment:AccountNumber hoặc Payment:AccountName. Hãy kiểm tra User Secrets của đúng project.";
        }

        private (string BankId, string AccountNumber, string AccountName)? GetPaymentAccount()
        {
            var bankId = _configuration["Payment:BankId"]?.Trim() ?? string.Empty;
            var accountNumber = _configuration["Payment:AccountNumber"]?.Trim() ?? string.Empty;
            var accountName = _configuration["Payment:AccountName"]?.Trim() ?? string.Empty;
            if (!System.Text.RegularExpressions.Regex.IsMatch(bankId, "^[A-Za-z0-9]{2,20}$") ||
                !System.Text.RegularExpressions.Regex.IsMatch(accountNumber, "^\\d{6,20}$") ||
                string.IsNullOrWhiteSpace(accountName) ||
                accountName.Length > 100 ||
                accountName.Any(char.IsControl))
            {
                return null;
            }

            return (bankId, accountNumber, accountName);
        }

        private DateTime GetVietnamTime()
        {
            var utcNow = DateTime.UtcNow;
            var timeZoneId = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "SE Asia Standard Time"
                : "Asia/Ho_Chi_Minh";

            var vietnamZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return TimeZoneInfo.ConvertTimeFromUtc(utcNow, vietnamZone);
        }
    }
}