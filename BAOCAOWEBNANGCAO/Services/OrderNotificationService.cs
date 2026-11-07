using System.Text.Encodings.Web;
using BAOCAOWEBNANGCAO.Models;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace BAOCAOWEBNANGCAO.Services
{
    public class OrderNotificationService
    {
        private readonly IEmailSender _emailSender;
        private readonly ILogger<OrderNotificationService> _logger;

        public OrderNotificationService(
            IEmailSender emailSender,
            ILogger<OrderNotificationService> logger)
        {
            _emailSender = emailSender;
            _logger = logger;
        }

        public Task OrderCreatedAsync(Order order) =>
            SendAsync(order, "Đã tiếp nhận đơn thuê",
                $"<p>Xin chào {Encode(order.CustomerName)},</p><p>Chúng tôi đã tiếp nhận yêu cầu thuê thiết bị của bạn.</p>{OrderSummary(order)}<p>Vui lòng hoàn tất tiền cọc trong thời gian giữ chỗ để xác nhận lịch thuê.</p>");

        public Task PaymentUpdatedAsync(Order order) =>
            SendAsync(order, "Cập nhật thanh toán",
                $"<p>Thanh toán cho đơn <strong>{Encode(order.PublicOrderCode)}</strong> đã được cập nhật.</p><p>Trạng thái: <strong>{Encode(order.PaymentStatus)}</strong></p><p>Đã thanh toán: <strong>{order.AmountPaid:N0} VNĐ</strong><br>Còn lại: <strong>{order.RemainingAmount:N0} VNĐ</strong></p>");

        public Task OrderStatusUpdatedAsync(Order order) =>
            SendAsync(order, "Cập nhật trạng thái đơn thuê",
                $"<p>Trạng thái đơn <strong>{Encode(order.PublicOrderCode)}</strong> hiện là <strong>{Encode(order.Status)}</strong>.</p>");

        public Task CancellationUpdatedAsync(Order order) =>
            SendAsync(order, "Cập nhật yêu cầu hủy đơn",
                $"<p>Yêu cầu hủy đơn <strong>{Encode(order.PublicOrderCode)}</strong> đã được nhân viên xử lý.</p><p>Trạng thái đơn: <strong>{Encode(order.Status)}</strong><br>Trạng thái hoàn tiền: <strong>{Encode(order.RefundStatus)}</strong><br>Số tiền dự kiến hoàn: <strong>{order.RefundAmount:N0} VNĐ</strong></p>{(order.RefundStatus == "DetailsRequired" ? "<p>Để nhận khoản hoàn, vui lòng vào website, mở mục Tra cứu đơn hàng, xác minh bằng mã gửi tới email đặt đơn, rồi nhập ngân hàng, số tài khoản và tên chủ tài khoản trong biểu mẫu bảo mật. Không gửi thông tin tài khoản qua email.</p>" : order.RefundStatus == "Pending" ? "<p>Đã nhận thông tin nhận tiền. Nhân viên sẽ chuyển khoản thủ công và cập nhật mã giao dịch.</p>" : order.RefundStatus == "Refunded" ? $"<p>Khoản hoàn đã được chuyển. Mã giao dịch: <strong>{Encode(order.RefundNotes ?? "Đã xác nhận")}</strong></p>" : "<p>Khoản hoàn (nếu có) được nhân viên chuyển khoản thủ công.</p>")}");

        public Task RefundDetailsReceivedAsync(Order order) =>
            SendAsync(order, "Đã nhận thông tin nhận hoàn tiền",
                $"<p>Chúng tôi đã nhận thông tin nhận hoàn tiền cho đơn <strong>{Encode(order.PublicOrderCode)}</strong>.</p><p>Số tiền dự kiến hoàn: <strong>{order.RefundAmount:N0} VNĐ</strong>. Nhân viên sẽ chuyển khoản thủ công; vui lòng không gửi lại số tài khoản qua email.</p>");

        public Task CancellationRequestedAsync(Order order) =>
            SendAsync(order, "Đã nhận yêu cầu hủy đơn",
                $"<p>Chúng tôi đã nhận yêu cầu hủy đơn <strong>{Encode(order.PublicOrderCode)}</strong>. Nhân viên sẽ kiểm tra và phản hồi qua email.</p>");

        public Task RentalChangeRequestedAsync(Order order) =>
            SendAsync(order, "Đã nhận yêu cầu đổi lịch thuê",
                $"<p>Chúng tôi đã nhận yêu cầu đổi lịch cho đơn <strong>{Encode(order.PublicOrderCode)}</strong>.</p><p>Lịch yêu cầu: {order.RequestedRentalStartDate:dd/MM/yyyy} – {order.RequestedRentalEndDate:dd/MM/yyyy}</p><p>Lịch hiện tại chưa thay đổi. Nhân viên sẽ kiểm tra tình trạng thiết bị và phản hồi.</p>");

        public Task RentalChangeResolvedAsync(Order order, bool approved)
        {
            var message =
                $"<p>Yêu cầu đổi lịch đơn <strong>{Encode(order.PublicOrderCode)}</strong> đã được xử lý.</p>" +
                $"<p>Kết quả: {Encode(order.RentalChangeResolution ?? "Đã cập nhật")}.</p>" +
                $"<p>Lịch thuê hiện tại: <strong>{order.RentalStartDate:dd/MM/yyyy} – {order.RentalEndDate:dd/MM/yyyy}</strong><br>" +
                $"Tổng tiền: <strong>{order.TotalAmount:N0} VNĐ</strong><br>" +
                $"Tiền cọc cần có: <strong>{order.DepositAmount:N0} VNĐ</strong><br>" +
                $"Đã thanh toán: <strong>{order.AmountPaid:N0} VNĐ</strong></p>";

            if (approved)
            {
                var additionalDeposit = Math.Max(0, order.DepositAmount - order.AmountPaid);
                if (additionalDeposit > 0)
                {
                    var transferReference = $"DH{order.PublicOrderCode.Replace("-", string.Empty)}";
                    message +=
                        $"<div style=\"margin:20px 0;padding:18px;background:#fff8e6;border-left:4px solid #d59b23;border-radius:8px\">" +
                        $"<p><strong>Cần thanh toán bổ sung tiền cọc: {additionalDeposit:N0} VNĐ</strong></p>" +
                        $"<p>Nội dung chuyển khoản: <strong>{Encode(transferReference)}</strong></p>" +
                        "<p>Để thanh toán, mở website Camping Rental → Tra cứu đơn hàng, nhập mã đơn và email đặt hàng, xác minh OTP gửi qua email, rồi chọn “Mở mã QR thanh toán bổ sung”. Kiểm tra đúng số tiền và nội dung chuyển khoản trước khi xác nhận.</p>" +
                        "<p>Đơn giữ chỗ bổ sung trong 30 phút kể từ lúc duyệt. Nếu không hoàn tất trong thời hạn này, đơn có thể hết hạn.</p></div>";
                }
                else
                {
                    message += "<p><strong>Bạn không cần thanh toán thêm tiền cọc.</strong> Số tiền còn lại (nếu có) thanh toán theo hướng dẫn khi nhận thiết bị.</p>";
                }
            }

            return SendAsync(order, "Kết quả yêu cầu đổi lịch thuê", message);
        }

        public Task OrderLookupCodeAsync(Order order, string code) =>
            SendAsync(order, "Mã xác minh tra cứu đơn",
                $"<p>Chúng tôi nhận được yêu cầu tra cứu đơn <strong>{Encode(order.PublicOrderCode)}</strong>.</p><p style=\"margin:24px 0;text-align:center\"><span style=\"display:inline-block;padding:14px 24px;background:#f1f5f2;border:1px dashed #1a3a28;border-radius:10px;font-size:30px;font-weight:700;letter-spacing:8px;color:#1a3a28\">{Encode(code)}</span></p><p>Mã có hiệu lực trong <strong>10 phút</strong>, chỉ sử dụng được một lần và tối đa 5 lần nhập.</p><p>Nếu bạn không yêu cầu mã này, vui lòng bỏ qua email.</p>");

        public Task ReservationExpiredAsync(Order order) =>
            SendAsync(order, "Đã hết thời gian giữ chỗ",
                $"<p>Thời gian hoàn tất tiền cọc cho đơn <strong>{Encode(order.PublicOrderCode)}</strong> đã hết. Đơn đã được đánh dấu hết hạn.</p>{(order.RefundAmount > 0 ? $"<p>Đã ghi nhận {order.RefundAmount:N0} VNĐ cần được nhân viên hoàn thủ công. Vui lòng vào website, mở mục Tra cứu đơn hàng, xác minh bằng mã gửi tới email đặt đơn, rồi nhập thông tin tài khoản nhận tiền trong biểu mẫu bảo mật. Không gửi thông tin tài khoản qua email.</p>" : string.Empty)}");

        private async Task SendAsync(Order order, string subject, string message)
        {
            try
            {
                var html = $"""
                    <!doctype html>
                    <html lang="vi">
                    <head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head>
                    <body style="margin:0;padding:24px;background:#f3f5f2;font-family:Arial,Helvetica,sans-serif;color:#26352c">
                      <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:620px;margin:0 auto;background:#ffffff;border-radius:16px;overflow:hidden">
                        <tr><td style="padding:24px 32px;background:#1a3a28;color:#ffffff">
                          <div style="font-size:12px;letter-spacing:2px;text-transform:uppercase;color:#c9d8cd">Camping Rental</div>
                          <div style="margin-top:8px;font-size:22px;font-weight:700">{Encode(subject)}</div>
                        </td></tr>
                        <tr><td style="padding:30px 32px;font-size:15px;line-height:1.75">{message}</td></tr>
                        <tr><td style="padding:18px 32px;background:#f7f8f6;color:#67736b;font-size:12px;line-height:1.6">
                          Email tự động từ Camping Rental. Nếu cần hỗ trợ, vui lòng gọi <a href="tel:0982412005" style="color:#1a3a28">0982 412 005</a>.<br>
                          Vui lòng không trả lời email này.
                        </td></tr>
                      </table>
                    </body>
                    </html>
                    """;
                await _emailSender.SendEmailAsync(
                    order.CustomerEmail,
                    $"[Camping Rental] {subject}",
                    html);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Could not send order notification for order {OrderId}.", order.Id);
            }
        }

        private static string OrderSummary(Order order) =>
            $"<div style=\"margin:20px 0;padding:18px;background:#f7f8f6;border-radius:10px\"><p style=\"margin:0 0 8px\">Mã đơn: <strong style=\"color:#1a3a28\">{Encode(order.PublicOrderCode)}</strong></p><p style=\"margin:0 0 8px\">Lịch thuê: {order.RentalStartDate:dd/MM/yyyy} – {order.RentalEndDate:dd/MM/yyyy}</p><p style=\"margin:0 0 8px\">Tổng tiền: <strong>{order.TotalAmount:N0} VNĐ</strong></p><p style=\"margin:0\">Tiền cọc cần thanh toán: <strong>{order.DepositAmount:N0} VNĐ</strong></p></div>";

        private static string Encode(string value) => HtmlEncoder.Default.Encode(value);
    }
}
