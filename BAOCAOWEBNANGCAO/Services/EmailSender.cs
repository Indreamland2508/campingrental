using Microsoft.AspNetCore.Identity.UI.Services;
using System; // Nhớ thêm dòng này
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace BAOCAOWEBNANGCAO.Services
{
    public class EmailSender : IEmailSender
    {
        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            try
            {
                // Thay bằng Email thật và Mã ứng dụng 16 số thật của bạn nhé
                var fromAddress = "chaun6536@gmail.com";


                var appPassword = "kjflxeszzpmidtee";

                var smtpClient = new SmtpClient("smtp.gmail.com", 587)
                {
                    EnableSsl = true,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromAddress, appPassword)
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromAddress, "Hệ thống Quản trị Camping Rental"),
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(email);

                await smtpClient.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                // NẾU RENDER CHẶN, NÓ SẼ NHẢY VÀO ĐÂY THAY VÌ LÀM SẬP WEB
                // Ghi lỗi ra màn hình Console (log) của Render để mình biết
                Console.WriteLine("LỖI GỬI MAIL (RENDER CHẶN): " + ex.Message);

                // Trả về Task hoàn thành để hệ thống cứ tưởng là gửi được rồi, 
                // giúp khách hàng vẫn thấy giao diện chuyển trang mượt mà.
                await Task.CompletedTask;
            }
        }
    }
}