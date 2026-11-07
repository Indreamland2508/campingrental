using BAOCAOWEBNANGCAO.Data;
using Microsoft.EntityFrameworkCore;

namespace BAOCAOWEBNANGCAO.Services
{
    public class OrderReservationCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OrderReservationCleanupService> _logger;

        public OrderReservationCleanupService(
            IServiceScopeFactory scopeFactory,
            ILogger<OrderReservationCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await ExpireReservationsAsync(stoppingToken);
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Failed to expire unpaid order reservations.");
                }
            }
        }

        private async Task ExpireReservationsAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<CampingDbContext>();
            var notifications = scope.ServiceProvider.GetRequiredService<OrderNotificationService>();
            var now = DateTime.UtcNow;

            var expiredOrderIds = await context.Orders
                .Where(order =>
                    order.Status != "Cancelled" &&
                    order.Status != "Completed" &&
                    order.Status != "Expired" &&
                    (order.PaymentStatus == "Unpaid" || order.PaymentStatus == "Partial") &&
                    order.ReservationExpiresAt != null &&
                    order.ReservationExpiresAt <= now)
                .Select(order => order.Id)
                .ToListAsync(cancellationToken);

            foreach (var orderId in expiredOrderIds)
            {
                await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
                var order = await context.Orders
                    .FromSqlInterpolated($"SELECT * FROM \"Orders\" WHERE \"Id\" = {orderId} FOR UPDATE")
                    .SingleOrDefaultAsync(cancellationToken);
                if (order == null ||
                    order.Status == "Cancelled" ||
                    order.Status == "Completed" ||
                    order.Status == "Expired" ||
                    (order.PaymentStatus != "Unpaid" && order.PaymentStatus != "Partial") ||
                    order.ReservationExpiresAt == null ||
                    order.ReservationExpiresAt > now)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    continue;
                }

                order.Status = "Expired";
                if (order.AmountPaid > 0)
                {
                    order.RefundAmount = order.AmountPaid;
                    order.RefundStatus = "DetailsRequired";
                }
                await context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                await notifications.ReservationExpiredAsync(order);
            }

            var oldChallenges = await context.OrderLookupChallenges
                .Where(challenge => challenge.ExpiresAt < now.AddDays(-1))
                .ToListAsync(cancellationToken);
            context.OrderLookupChallenges.RemoveRange(oldChallenges);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
