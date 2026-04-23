using BAOCAOWEBNANGCAO.Data;
using BAOCAOWEBNANGCAO.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Runtime.InteropServices;

namespace BAOCAOWEBNANGCAO.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class CartController : Controller
    {
        private readonly CampingDbContext _context;
        private const string CartSessionKey = "GioHangCuaToi";

        public CartController(CampingDbContext context)
        {
            _context = context;
        }

        // GET: Xem giỏ hàng
        public IActionResult Index()
        {
            var cart = GetCartFromSession();
            ViewBag.TotalAmount = cart.Sum(item => item.TotalPrice);
            return View(cart);
        }

        [HttpPost]
        public IActionResult AddToCart(int productId, int quantity)
        {
            var cart = GetCartFromSession();
            var cartItem = cart.FirstOrDefault(c => c.Product.Id == productId);

            if (cartItem != null)
            {
                cartItem.Quantity += quantity;
            }
            else
            {
                var product = _context.Products.Find(productId);
                if (product != null)
                {
                    cart.Add(new CartItem
                    {
                        Product = product,
                        Quantity = quantity
                    });
                }
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

        // GET: Xóa 1 sản phẩm khỏi giỏ
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
            string json = session.GetString(CartSessionKey);

            if (!string.IsNullOrEmpty(json))
            {
                return JsonConvert.DeserializeObject<List<CartItem>>(json);
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
            return View(cart);
        }

        
        [HttpPost]
        public async Task<IActionResult> Checkout(Order order, DateTime rentalStart, DateTime rentalEnd)
        {
            try
            {
                var cart = GetCartFromSession();

                // 1. CHỐNG BẤM ĐÚP VÀ MẤT SESSION
                if (cart == null || cart.Count == 0)
                {
                    return Content("ÚI CHÀ! Giỏ hàng đang trống. Khả năng cao là đơn hàng CỦA BẠN ĐÃ ĐƯỢC ĐẶT THÀNH CÔNG ở cú click trước đó rồi. Bạn hãy vào trang chủ, chọn 'Tra cứu đơn hàng' xem đơn đã được tạo chưa nhé!");
                }

                if (string.IsNullOrWhiteSpace(order.CustomerPhone))
                {
                    ModelState.AddModelError("CustomerPhone", "Số điện thoại là bắt buộc.");
                }
                else if (!System.Text.RegularExpressions.Regex.IsMatch(order.CustomerPhone, "^[0-9]{9,10}$"))
                {
                    ModelState.AddModelError("CustomerPhone", "Số điện thoại phải gồm 9-10 chữ số.");
                }

                if (string.IsNullOrWhiteSpace(order.CustomerEmail))
                {
                    ModelState.AddModelError("CustomerEmail", "Email liên hệ là bắt buộc.");
                }
                else if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(order.CustomerEmail))
                {
                    ModelState.AddModelError("CustomerEmail", "Email không đúng định dạng.");
                }

                if (string.IsNullOrWhiteSpace(order.ShippingAddress))
                {
                    ModelState.AddModelError("ShippingAddress", "Địa chỉ nhận lều là bắt buộc.");
                }

                if (rentalEnd < rentalStart)
                {
                    ModelState.AddModelError("rentalEnd", "Ngày trả phải sau hoặc bằng ngày nhận.");
                }

                if (!ModelState.IsValid)
                {
                    ViewBag.TotalAmount = cart.Sum(item => item.TotalPrice);
                    ViewBag.CustomerName = order.CustomerName;
                    ViewBag.CustomerPhone = order.CustomerPhone;
                    ViewBag.CustomerEmail = order.CustomerEmail;
                    ViewBag.ShippingAddress = order.ShippingAddress;
                    ViewBag.Note = order.Note;
                    ViewBag.RentalStart = rentalStart.ToString("yyyy-MM-dd");
                    ViewBag.RentalEnd = rentalEnd.ToString("yyyy-MM-dd");
                    return View(cart);
                }

                TimeSpan duration = rentalEnd - rentalStart;
                int rentalDays = duration.Days > 0 ? duration.Days : 1;

                decimal cartTotal = cart.Sum(item => item.TotalPrice);
                decimal finalTotal = cartTotal * rentalDays;
                decimal deposit = finalTotal / 2;
                decimal remaining = finalTotal - deposit;

                var orderEntity = new Order
                {
                    CustomerName = order.CustomerName,
                    CustomerPhone = order.CustomerPhone,
                    CustomerEmail = order.CustomerEmail,
                    ShippingAddress = order.ShippingAddress,
                    Note = order.Note,
                    RentalStartDate = rentalStart.ToUniversalTime(),
                    RentalEndDate = rentalEnd.ToUniversalTime(),
                    OrderDate = DateTime.UtcNow, // Lưu UTC để database xử lý đúng
                    TotalAmount = finalTotal,
                    DepositAmount = deposit,
                    RemainingAmount = remaining,
                    Status = "Pending",
                    PaymentStatus = "Unpaid"
                };

                _context.Orders.Add(orderEntity);
                await _context.SaveChangesAsync();

                foreach (var item in cart)
                {
                    var orderDetail = new OrderDetail
                    {
                        OrderId = orderEntity.Id,
                        ProductId = item.Product.Id, // Nếu ProductId lỗi, sẽ bị bắt ngay
                        Quantity = item.Quantity,
                        PricePerUnit = item.Product.PricePerDay
                    };
                    _context.OrderDetails.Add(orderDetail);
                }
                await _context.SaveChangesAsync();

                // Dọn dẹp giỏ hàng
                HttpContext.Session.Remove(CartSessionKey);
                HttpContext.Session.Remove("CartCount");

                return RedirectToAction("Payment", new { id = orderEntity.Id });
            }
            catch (Exception ex)
            {
                // BẮT LỖI TẠI TRẬN: Sẽ in thẳng lỗi ra màn hình trắng để xem nó kẹt cái gì
                var errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return Content("LỖI HỆ THỐNG RỒI CHÂU ƠI! Chi tiết lỗi: " + errorMsg);
            }
        }

        // --- CÁC HÀM XỬ LÝ THANH TOÁN SEPAY & GIAO DIỆN ---

        // 1. GET: Trang hiển thị mã QR (Chờ quét)
        public IActionResult Payment(int id)
        {
            var order = _context.Orders.Find(id);
            // Nếu không có đơn hoặc đơn đã thanh toán rồi thì đá về trang chủ
            if (order == null || order.PaymentStatus != "Unpaid") return RedirectToAction("Index", "Home");

            return View(order);
        }

        // 2. API nhỏ để giao diện liên tục hỏi thăm trạng thái thanh toán
        [HttpGet]
        public IActionResult CheckPaymentStatus(int id)
        {
            var order = _context.Orders.Find(id);
            if (order == null) return NotFound();

            // Trả về dữ liệu dạng JSON cho JavaScript đọc
            return Json(new { status = order.PaymentStatus });
        }

        // 3. GET: Trang thông báo mua thành công (Chỉ hiện khi đã thanh toán)
        public IActionResult OrderSuccess(int id)
        {
            var order = _context.Orders.Find(id);
            if (order == null) return RedirectToAction("Index", "Home");

            return View(order);
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