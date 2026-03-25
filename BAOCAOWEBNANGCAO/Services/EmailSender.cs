using Microsoft.AspNetCore.Identity.UI.Services;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System;
using System.Threading.Tasks;

namespace BAOCAOWEBNANGCAO.Services
{
    public class EmailSender : IEmailSender
    {
        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            try
            {
                // 1. Điền địa chỉ Gmail thật của bạn
                var senderEmail = "chaun6536@gmail.com";

                // 2. Điền MẬT KHẨU ỨNG DỤNG (16 CHỮ CÁI) lấy từ tài khoản Google
                // LƯU Ý: Tuyệt đối KHÔNG DÙNG mật khẩu đăng nhập Gmail bình thường!
                var appPassword = "kjflxeszzpmidtee";

                // Đóng gói bức thư bằng MimeKit
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Hệ thống Camping Rental", senderEmail));
                message.To.Add(new MailboxAddress("Thành viên Camping", email));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder { HtmlBody = htmlMessage };
                message.Body = bodyBuilder.ToMessageBody();

                // Dùng MailKit để kết nối an toàn với Google
                using var client = new SmtpClient();

                // Bỏ qua kiểm tra chứng chỉ SSL khắt khe của Render
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                // Kết nối cổng 587 chuẩn TLS
                await client.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);

                // Đăng nhập bằng Mật khẩu ứng dụng 16 số
                await client.AuthenticateAsync(senderEmail, appPassword);

                // Gửi thư đi và ngắt kết nối
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                // Ghi lỗi ra hệ thống để biết nếu Google chặn
                Console.WriteLine("LỖI GỬI MAIL (MAILKIT): " + ex.Message);
            }
        }
    }
}