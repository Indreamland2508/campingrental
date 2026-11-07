using System.Text.Json.Serialization;

namespace BAOCAOWEBNANGCAO.Services
{
    public class TurnstileVerificationService
    {
        private const string DevelopmentSiteKey = "1x00000000000000000000AA";
        private const string DevelopmentSecretKey = "1x0000000000000000000000000000000AA";
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IHostEnvironment _environment;
        private readonly ILogger<TurnstileVerificationService> _logger;

        public TurnstileVerificationService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IHostEnvironment environment,
            ILogger<TurnstileVerificationService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _environment = environment;
            _logger = logger;
        }

        public string SiteKey =>
            _configuration["Turnstile:SiteKey"] ??
            (_environment.IsDevelopment() ? DevelopmentSiteKey : string.Empty);

        public async Task<bool> VerifyAsync(string? token, string? remoteIp, CancellationToken cancellationToken)
        {
            var secretKey = _configuration["Turnstile:SecretKey"] ??
                (_environment.IsDevelopment() ? DevelopmentSecretKey : null);
            if (string.IsNullOrWhiteSpace(secretKey) || string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("Turnstile verification cannot proceed because its key or token is missing.");
                return false;
            }

            var formValues = new Dictionary<string, string>
            {
                ["secret"] = secretKey,
                ["response"] = token
            };
            if (!string.IsNullOrWhiteSpace(remoteIp))
            {
                formValues["remoteip"] = remoteIp;
            }

            using var content = new FormUrlEncodedContent(formValues);
            using var response = await _httpClientFactory.CreateClient(nameof(TurnstileVerificationService))
                .PostAsync("https://challenges.cloudflare.com/turnstile/v0/siteverify", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<VerificationResult>(cancellationToken);
            return result?.Success == true;
        }

        private sealed class VerificationResult
        {
            [JsonPropertyName("success")]
            public bool Success { get; set; }
        }
    }
}
