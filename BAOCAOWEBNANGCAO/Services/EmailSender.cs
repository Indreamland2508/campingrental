using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace BAOCAOWEBNANGCAO.Services // Đảm bảo đúng Namespace của bạn
{
    public class EmailSender : IEmailSender
    {
        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // 1. Điền Email của dự án (Email dùng để đi gửi thư)
            var fromAddress = "chaun6536@gmail.com";

            // 2. Điền CÁI MÃ 16 CHỮ CÁI màu vàng vừa copy ở Bước 1 vào đây (viết liền không khoảng trắng)
            var appPassword = "kjflxeszzpmidtee";

            // Cấu hình kết nối tới trạm bưu điện của Google
            var smtpClient = new SmtpClient("smtp.gmail.com", 587)
            {
                EnableSsl = true,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromAddress, appPassword)
            };

            // Đóng gói bức thư
            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromAddress, "Hệ thống Quản trị Camping Rental"),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true // Chấp nhận mã HTML để trang trí thư cho đẹp
            };

            // Ghi địa chỉ người nhận
            mailMessage.To.Add(email);

            // Giao cho bưu tá đi gửi
            await smtpClient.SendMailAsync(mailMessage);
        }
    }
}