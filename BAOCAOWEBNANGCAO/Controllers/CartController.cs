using BAOCAOWEBNANGCAO.Data;
using BAOCAOWEBNANGCAO.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace BAOCAOWEBNANGCAO.Controllers
{
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

        // POST: Thêm sản phẩm vào giỏ
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

        // POST: Xử lý đặt hàng & TÍNH TIỀN CỌC
        [HttpPost]
        public async Task<IActionResult> Checkout(string customerName, string customerPhone, string customerEmail, string shippingAddress, string? note, DateTime rentalStart, DateTime rentalEnd)
        {
            var cart = GetCartFromSession();
            if (cart.Count == 0) return RedirectToAction("Index");

            // 1. Tính số ngày thuê
            TimeSpan duration = rentalEnd - rentalStart;
            int rentalDays = duration.Days;
            if (rentalDays <= 0) rentalDays = 1;

            // 2. Tính tổng tiền
            decimal cartTotal = cart.Sum(item => item.TotalPrice);
            decimal finalTotal = cartTotal * rentalDays;

            // 3. TÍNH TIỀN CỌC (50%) VÀ CÒN LẠI
            decimal deposit = finalTotal / 2;
            decimal remaining = finalTotal - deposit;

            // 4. Tạo Order với các trường mới
            var order = new Order
            {
                CustomerName = customerName,
                CustomerPhone = customerPhone,
                CustomerEmail = customerEmail,
                ShippingAddress = shippingAddress,
                Note = note,
                RentalStartDate = rentalStart,
                RentalEndDate = rentalEnd,
                OrderDate = DateTime.Now,

                TotalAmount = finalTotal,
                DepositAmount = deposit,             // Lưu tiền cọc
                RemainingAmount = remaining,         // Lưu tiền thu lúc nhận lều

                Status = "Pending",                  // Trạng thái giao nhận: Đang chờ
                PaymentStatus = "Unpaid"             // Trạng thái tiền: Chưa thanh toán cọc
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // 5. Lưu chi tiết đơn hàng
            foreach (var item in cart)
            {
                var orderDetail = new OrderDetail
                {
                    OrderId = order.Id,
                    ProductId = item.Product.Id,
                    Quantity = item.Quantity,
                    PricePerUnit = item.Product.PricePerDay
                };
                _context.OrderDetails.Add(orderDetail);
            }

            await _context.SaveChangesAsync();

            // Xóa giỏ hàng sau khi đặt thành công
            HttpContext.Session.Remove("GioHangCuaToi");
            HttpContext.Session.Remove("CartCount");

            // 6. CHUYỂN HƯỚNG SANG TRANG THANH TOÁN QR
            return RedirectToAction("Payment", new { id = order.Id });
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
    }
}