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
            // 1. Chỉ xử lý khi có tiền vào
            if (data == null || data.transferType != "in")
            {
                return Ok(new { success = true, message = "Không phải giao dịch nhận tiền." });
            }

            // 2. Lấy mã đơn hàng từ nội dung (Khách ghi: "DH" + Mã số)
            int? orderId = ExtractOrderId(data.transferContent);

            if (orderId.HasValue)
            {
                // 3. Tìm đơn hàng
                var order = await _context.Orders.FindAsync(orderId.Value);

                // Chỉ xử lý nếu đơn tồn tại và đang ở trạng thái chưa thanh toán
                if (order != null && order.PaymentStatus == "Unpaid")
                {
                    // Trường hợp 1: Khách hào phóng chuyển thẳng 100% tổng tiền
                    if (data.amount >= order.TotalAmount)
                    {
                        order.PaymentStatus = "Paid";
                        order.RemainingAmount = 0;
                    }
                    // Trường hợp 2: Khách chuyển đúng bằng hoặc lớn hơn số tiền cọc
                    else if (data.amount >= order.DepositAmount)
                    {
                        order.PaymentStatus = "Deposited";
                        order.RemainingAmount = order.TotalAmount - data.amount; // Tính lại tiền khách còn nợ
                    }
                    else
                    {
                        // Khách chuyển thiếu cả tiền cọc
                        return Ok(new { success = true, message = "Số tiền chuyển chưa đủ mức cọc tối thiểu." });
                    }

                    _context.Update(order);
                    await _context.SaveChangesAsync();

                    return Ok(new { success = true, message = "Đã cập nhật trạng thái đơn hàng thành công." });
                }
            }

            return Ok(new { success = true, message = "Đã nhận được thông báo nhưng không xử lý đơn nào." });
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