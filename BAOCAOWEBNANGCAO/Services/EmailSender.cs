using Microsoft.AspNetCore.Identity.UI.Services;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace BAOCAOWEBNANGCAO.Services
{
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            try
            {
                // Thay bằng Username và Password bạn vừa copy trên Mailtrap
                var mailtrapUsername = "21280a2dee2a67";
                var mailtrapPassword = "f01e41ff5da4a6";

                var client = new SmtpClient("sandbox.smtp.mailtrap.io", 2525)
                {
                    Credentials = new NetworkCredential(mailtrapUsername, mailtrapPassword),
                    EnableSsl = true
                };

                // Email người gửi (Khai báo bừa cũng được vì Mailtrap chấp nhận hết)
                var mailMessage = new MailMessage
                {
                    From = new MailAddress("hethong@campingrental.vn", "Hệ thống Camping Rental"),
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(email);

                client.Send(mailMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine("LỖI GỬI MAIL (MAILTRAP): " + ex.Message);
            }

            return Task.CompletedTask;
        }
    }
}