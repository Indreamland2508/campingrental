using Microsoft.AspNetCore.Identity.UI.Services;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BAOCAOWEBNANGCAO.Services
{
    public class EmailSender : IEmailSender
    {
        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            try
            {
                // DÁN CÁI LINK SCRIPT MÀ BẠN VỪA COPY Ở BƯỚC 1 VÀO ĐÂY
                var scriptUrl = "https://script.google.com/macros/s/AKfycbxw91rFj1M1KHaR4ox8vOMG3zqEVBs6cQBeVb9NyGNhpzaOS67SvsP966sJ8n4E-RaaNw/exec";

                using var client = new HttpClient();

                // Đóng gói bức thư
                var payload = new
                {
                    to = email,
                    subject = subject,
                    htmlBody = htmlMessage
                };

                var jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                // Bắn qua đường lướt web HTTPS (Render KHÔNG THỂ CHẶN)
                var response = await client.PostAsync(scriptUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine("LỖI TỪ GOOGLE SCRIPT: " + await response.Content.ReadAsStringAsync());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("LỖI GỬI MAIL (HTTP): " + ex.Message);
            }
        }
    }
}