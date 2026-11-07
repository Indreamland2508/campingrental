using Microsoft.AspNetCore.Identity.UI.Services;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BAOCAOWEBNANGCAO.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailSender> _logger;
        private const string DefaultScriptUrl = "https://script.google.com/macros/s/AKfycbxw91rFj1M1KHaR4ox8vOMG3zqEVBs6cQBeVb9NyGNhpzaOS67SvsP966sJ8n4E-RaaNw/exec";

        public EmailSender(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<EmailSender> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var scriptUrl = _configuration["Email:GatewayUrl"] ?? DefaultScriptUrl;
            if (!Uri.TryCreate(scriptUrl, UriKind.Absolute, out var gatewayUri) ||
                gatewayUri.Scheme != Uri.UriSchemeHttps)
            {
                throw new InvalidOperationException("The email gateway URL must be a valid HTTPS URL.");
            }

            var payload = new
            {
                to = email,
                subject,
                htmlBody = htmlMessage
            };

            using var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");
            using var response = await _httpClientFactory.CreateClient(nameof(EmailSender))
                .PostAsync(gatewayUri, content);
            response.EnsureSuccessStatusCode();
            _logger.LogInformation("Email gateway accepted a message.");
        }
    }
}