using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BAOCAOWEBNANGCAO.Data;
using BAOCAOWEBNANGCAO.Models;
using System.Text.RegularExpressions;

namespace BAOCAOWEBNANGCAO.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SePayController : ControllerBase
    {
        private readonly CampingDbContext _context;

        public SePayController(CampingDbContext context)
        {
            _context = context;
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> ReceiveWebhook([FromBody] SePayWebhookData data)
        {
            Console.WriteLine("=== CÓ TIN NHẮN TỪ SEPAY GỬI ĐẾN ===");

            if (data == null)
            {
                Console.WriteLine("LỖI: Dữ liệu gửi đến bị NULL");
                return Ok(new { success = false, message = "Data null" });
            }

            Console.WriteLine($"Số tiền: {data.amount} | Nội dung: {data.transferContent} | Loại: {data.transferType}");

            if (data.transferType != "in")
            {
                Console.WriteLine("Bỏ qua vì đây không phải giao dịch nhận tiền.");
                return Ok(new { success = true });
            }

            int? orderId = ExtractOrderId(data.transferContent);
            Console.WriteLine($"Mã đơn hàng bóc tách được: {(orderId.HasValue ? orderId.Value.ToString() : "KHÔNG TÌM THẤY MÃ DH")}");

            if (orderId.HasValue)
            {
                var order = await _context.Orders.FindAsync(orderId.Value);

                if (order == null)
                {
                    Console.WriteLine($"LỖI: Không tìm thấy đơn hàng số {orderId.Value} trong Database.");
                }
                else
                {
                    Console.WriteLine($"Đã thấy đơn #{order.Id}. Trạng thái hiện tại: {order.PaymentStatus}. Cần cọc: {order.DepositAmount}. Tổng: {order.TotalAmount}");

                    if (order.PaymentStatus == "Unpaid")
                    {
                        if (data.amount >= order.TotalAmount)
                        {
                            order.PaymentStatus = "Paid";
                            order.RemainingAmount = 0;
                            Console.WriteLine("=> Đã cập nhật thành PAID (Thanh toán đủ)");
                        }
                        else if (data.amount >= order.DepositAmount)
                        {
                            order.PaymentStatus = "Deposited";
                            order.RemainingAmount = order.TotalAmount - data.amount;
                            Console.WriteLine($"=> Đã cập nhật thành DEPOSITED (Đã cọc). Khách còn nợ: {order.RemainingAmount}");
                        }
                        else
                        {
                            Console.WriteLine($"LỖI: Khách chuyển {data.amount} NHỎ HƠN tiền cọc yêu cầu {order.DepositAmount}");
                            return Ok(new { success = true, message = "Thiếu tiền cọc" });
                        }

                        _context.Update(order);
                        await _context.SaveChangesAsync();
                        Console.WriteLine("=== LƯU DATABASE THÀNH CÔNG ===");
                    }
                    else
                    {
                        Console.WriteLine($"Đơn hàng này đã được xử lý trước đó rồi (Trạng thái: {order.PaymentStatus})");
                    }
                }
            }

            return Ok(new { success = true });
        }

        private int? ExtractOrderId(string content)
        {
            if (string.IsNullOrEmpty(content)) return null;

            var cleanContent = content.ToUpper().Replace(" ", "");
            var match = Regex.Match(cleanContent, @"DH(\d+)");

            if (match.Success && int.TryParse(match.Groups[1].Value, out int id))
            {
                return id;
            }
            return null;
        }
    }
}