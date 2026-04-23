using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BAOCAOWEBNANGCAO.Data;
using BAOCAOWEBNANGCAO.Models;
using System.Text.RegularExpressions;

namespace BAOCAOWEBNANGCAO.Areas.Customer.Controllers
{
    [Area("Customer")]
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
                Console.WriteLine("LỖI: Dữ liệu gửi đến bị NULL do không khớp JSON.");
                return BadRequest();
            }

            Console.WriteLine($"Số tiền: {data.transferAmount} | Nội dung: {data.content} | Loại: {data.transferType}");

            if (data.transferType != "in")
            {
                return Ok(new { success = true });
            }

            // Lấy mã đơn hàng từ biến 'content'
            int? orderId = ExtractOrderId(data.content);
            Console.WriteLine($"Mã đơn hàng bóc tách được: {(orderId.HasValue ? orderId.Value.ToString() : "KHÔNG TÌM THẤY MÃ DH")}");

            if (orderId.HasValue)
            {
                var order = await _context.Orders.FindAsync(orderId.Value);

                if (order != null && order.PaymentStatus == "Unpaid")
                {
                    // So sánh với biến 'transferAmount'
                    if (data.transferAmount >= order.TotalAmount)
                    {
                        order.PaymentStatus = "Paid";
                        order.RemainingAmount = 0;
                    }
                    else if (data.transferAmount >= order.DepositAmount)
                    {
                        order.PaymentStatus = "Deposited";
                        order.RemainingAmount = order.TotalAmount - data.transferAmount;
                    }

                    _context.Update(order);
                    await _context.SaveChangesAsync();
                    Console.WriteLine("=== LƯU DATABASE THÀNH CÔNG ===");
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