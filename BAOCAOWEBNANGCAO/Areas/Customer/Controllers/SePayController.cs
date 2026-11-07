using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using BAOCAOWEBNANGCAO.Data;
using BAOCAOWEBNANGCAO.Models;
using BAOCAOWEBNANGCAO.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BAOCAOWEBNANGCAO.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Route("api/[controller]")]
    [ApiController]
    public class SePayController : ControllerBase
    {
        private static readonly Regex OrderCodeRegex = new(@"DH(CR-?[A-F0-9]{16})", RegexOptions.Compiled);
        private static readonly Regex LegacyOrderNumberRegex = new(@"DH(\d+)", RegexOptions.Compiled);
        private readonly CampingDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SePayController> _logger;
        private readonly OrderNotificationService _notifications;

        public SePayController(
            CampingDbContext context,
            IConfiguration configuration,
            ILogger<SePayController> logger,
            OrderNotificationService notifications)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _notifications = notifications;
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> ReceiveWebhook(CancellationToken cancellationToken)
        {
            var secret = _configuration["SePay:WebhookSecret"];
            if (string.IsNullOrWhiteSpace(secret))
            {
                _logger.LogError("SePay webhook secret is not configured.");
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { success = false });
            }

            await using var bodyStream = new MemoryStream();
            await Request.Body.CopyToAsync(bodyStream, cancellationToken);
            var rawBody = bodyStream.ToArray();
            if (!HasValidSignature(rawBody, secret, Request.Headers["X-SePay-Signature"], Request.Headers["X-SePay-Timestamp"]))
            {
                _logger.LogWarning("Rejected SePay webhook with invalid signature or timestamp.");
                return Unauthorized(new { success = false });
            }

            SePayWebhookData? data;
            try
            {
                data = JsonSerializer.Deserialize<SePayWebhookData>(
                    rawBody,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (JsonException exception)
            {
                _logger.LogWarning(exception, "Rejected malformed SePay webhook payload.");
                return BadRequest(new { success = false });
            }

            if (data == null || data.id <= 0 || data.transferAmount <= 0 ||
                string.IsNullOrWhiteSpace(data.transferType))
            {
                return BadRequest(new { success = false });
            }

            if (await _context.SePayTransactions.AnyAsync(
                    transaction => transaction.SePayId == data.id,
                    cancellationToken))
            {
                return Ok(new { success = true });
            }

            var now = DateTime.UtcNow;
            var transactionRecord = new SePayTransaction
            {
                SePayId = data.id,
                Gateway = data.gateway,
                TransactionDate = data.transactionDate,
                AccountNumber = data.accountNumber,
                ReferenceCode = data.referenceCode,
                TransferType = data.transferType,
                TransferAmount = data.transferAmount,
                Content = data.content,
                ReceivedAt = now
            };

            Order? changedOrder = null;
            await using var dbTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            if (string.Equals(data.transferType, "in", StringComparison.OrdinalIgnoreCase))
            {
                var orderCode = ExtractOrderCode(data.content);
                var legacyOrderId = orderCode == null ? ExtractLegacyOrderId(data.content) : null;
                if (orderCode != null || legacyOrderId.HasValue)
                {
                    changedOrder = orderCode != null
                        ? await _context.Orders
                            .FromSqlInterpolated($"SELECT * FROM \"Orders\" WHERE \"PublicOrderCode\" = {orderCode} FOR UPDATE")
                            .SingleOrDefaultAsync(cancellationToken)
                        : await _context.Orders
                            .FromSqlInterpolated($"SELECT * FROM \"Orders\" WHERE \"Id\" = {legacyOrderId!.Value} FOR UPDATE")
                            .SingleOrDefaultAsync(cancellationToken);

                    if (changedOrder != null)
                    {
                        transactionRecord.OrderId = changedOrder.Id;
                    }

                    if (changedOrder != null &&
                        changedOrder.Status != "Cancelled" &&
                        changedOrder.Status != "Expired" &&
                        changedOrder.PaymentStatus != "Paid")
                    {
                        changedOrder.AmountPaid += data.transferAmount;
                        changedOrder.RemainingAmount = Math.Max(0, changedOrder.TotalAmount - changedOrder.AmountPaid);
                        changedOrder.PaymentStatus = changedOrder.AmountPaid >= changedOrder.TotalAmount
                            ? "Paid"
                            : changedOrder.AmountPaid >= changedOrder.DepositAmount
                                ? "Deposited"
                                : "Partial";
                        changedOrder.ReservationExpiresAt =
                            changedOrder.PaymentStatus == "Partial" ? DateTime.UtcNow.AddMinutes(30) : null;
                        transactionRecord.AppliedToOrder = true;
                    }
                }
            }

            _context.SePayTransactions.Add(transactionRecord);
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                await dbTransaction.CommitAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                await dbTransaction.RollbackAsync(cancellationToken);
                _context.Entry(transactionRecord).State = EntityState.Detached;
                if (await _context.SePayTransactions.AnyAsync(
                        transaction => transaction.SePayId == data.id,
                        cancellationToken))
                {
                    return Ok(new { success = true });
                }

                throw;
            }

            _logger.LogInformation(
                "Processed SePay transaction {SePayId}; matched order {OrderId}; amount {Amount}; applied {Applied}.",
                data.id,
                transactionRecord.OrderId,
                data.transferAmount,
                transactionRecord.AppliedToOrder);

            if (transactionRecord.AppliedToOrder && changedOrder != null)
            {
                await _notifications.PaymentUpdatedAsync(changedOrder);
            }

            return Ok(new { success = true });
        }

        private static bool HasValidSignature(
            byte[] rawBody,
            string secret,
            string? signature,
            string? timestampHeader)
        {
            if (string.IsNullOrWhiteSpace(signature) ||
                !signature.StartsWith("sha256=", StringComparison.Ordinal) ||
                !long.TryParse(timestampHeader, NumberStyles.None, CultureInfo.InvariantCulture, out var timestamp))
            {
                return false;
            }

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (timestamp < now - 300 || timestamp > now + 300)
            {
                return false;
            }

            var signedPayload = Encoding.UTF8.GetBytes($"{timestamp}.");
            var message = new byte[signedPayload.Length + rawBody.Length];
            Buffer.BlockCopy(signedPayload, 0, message, 0, signedPayload.Length);
            Buffer.BlockCopy(rawBody, 0, message, signedPayload.Length, rawBody.Length);

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var expected = $"sha256={Convert.ToHexString(hmac.ComputeHash(message)).ToLowerInvariant()}";
            return CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(expected),
                Encoding.ASCII.GetBytes(signature));
        }

        private static string? ExtractOrderCode(string? content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return null;
            }

            var match = OrderCodeRegex.Match(content.Replace(" ", string.Empty).ToUpperInvariant());
            if (!match.Success)
            {
                return null;
            }

            var value = match.Groups[1].Value.Replace("-", string.Empty);
            return $"{value[..2]}-{value[2..]}";
        }

        private static int? ExtractLegacyOrderId(string? content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return null;
            }

            var match = LegacyOrderNumberRegex.Match(content.Replace(" ", string.Empty).ToUpperInvariant());
            return match.Success && int.TryParse(match.Groups[1].Value, out var id) ? id : null;
        }
    }
}
