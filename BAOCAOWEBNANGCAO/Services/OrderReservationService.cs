using System.Data;
using BAOCAOWEBNANGCAO.Data;
using BAOCAOWEBNANGCAO.Models;
using Microsoft.EntityFrameworkCore;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace BAOCAOWEBNANGCAO.Services
{
    public class OrderReservationService
    {
        private readonly CampingDbContext _context;

        public OrderReservationService(CampingDbContext context)
        {
            _context = context;
        }

        public async Task<Order> CreateOrderAsync(
            Order customerInput,
            DateTime rentalStart,
            DateTime rentalEnd,
            IReadOnlyCollection<CartItem> cart,
            CancellationToken cancellationToken = default)
        {
            if (cart.Count == 0)
            {
                throw new InvalidOperationException("Giỏ hàng đang trống.");
            }

            var startDate = rentalStart.Date;
            var endDate = rentalEnd.Date;
            if (endDate < startDate)
            {
                throw new InvalidOperationException("Ngày trả phải bằng hoặc sau ngày nhận.");
            }

            var timeZoneId = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "SE Asia Standard Time"
                : "Asia/Ho_Chi_Minh";
            var todayInVietnam = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(timeZoneId)).Date;
            if (startDate < todayInVietnam)
            {
                throw new InvalidOperationException("Ngày nhận không thể ở trong quá khứ.");
            }

            var endExclusive = endDate == startDate ? startDate.AddDays(1) : endDate;
            var requestedItems = cart
                .GroupBy(item => item.Product.Id)
                .Select(group => new { ProductId = group.Key, Quantity = group.Sum(item => item.Quantity) })
                .OrderBy(item => item.ProductId)
                .ToList();

            if (requestedItems.Any(item => item.ProductId <= 0 || item.Quantity <= 0))
            {
                throw new InvalidOperationException("Số lượng sản phẩm không hợp lệ.");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);

            var phone = Regex.Replace(customerInput.CustomerPhone ?? string.Empty, @"\D", string.Empty);
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtextextended({phone}, 0))",
                cancellationToken);

            var now = DateTime.UtcNow;
            var outstandingOrderCount = await _context.Orders.CountAsync(order =>
                order.CustomerPhone.Replace(" ", string.Empty).Replace("-", string.Empty) == phone &&
                order.Status != "Cancelled" &&
                order.Status != "Completed" &&
                order.Status != "Expired" &&
                (order.PaymentStatus == "Unpaid" || order.PaymentStatus == "Partial") &&
                order.ReservationExpiresAt != null &&
                order.ReservationExpiresAt > now,
                cancellationToken);
            if (outstandingOrderCount >= 2)
            {
                throw new InvalidOperationException(
                    "Số điện thoại này đang có 2 đơn chưa hoàn tất tiền cọc. Vui lòng thanh toán hoặc chờ đơn hết hạn trước khi tạo đơn mới.");
            }

            foreach (var item in requestedItems)
            {
                var lockedProduct = await _context.Products
                    .FromSqlInterpolated($"SELECT * FROM \"Products\" WHERE \"Id\" = {item.ProductId} FOR UPDATE")
                    .SingleOrDefaultAsync(cancellationToken);

                if (lockedProduct == null)
                {
                    throw new InvalidOperationException("Một sản phẩm trong giỏ hàng không còn tồn tại.");
                }
            }

            var products = await _context.Products
                .Where(product => requestedItems.Select(item => item.ProductId).Contains(product.Id))
                .ToDictionaryAsync(product => product.Id, cancellationToken);

            var overlappingOrders = await _context.Orders
                .Where(order =>
                    order.Status != "Cancelled" &&
                    order.Status != "Expired" &&
                    order.RentalStartDate < endExclusive &&
                    (order.RentalEndDate > startDate ||
                     (order.RentalEndDate == order.RentalStartDate &&
                      order.RentalStartDate >= startDate &&
                      order.RentalStartDate < endExclusive)) &&
                    (order.PaymentStatus == "Paid" ||
                     order.PaymentStatus == "Deposited" ||
                     (order.ReservationExpiresAt != null && order.ReservationExpiresAt > now)))
                .Include(order => order.OrderDetails)
                .ToListAsync(cancellationToken);

            foreach (var item in requestedItems)
            {
                if (!products.TryGetValue(item.ProductId, out var product))
                {
                    throw new InvalidOperationException("Một sản phẩm trong giỏ hàng không còn tồn tại.");
                }

                var reservedQuantity = overlappingOrders
                    .SelectMany(order => order.OrderDetails)
                    .Where(detail => detail.ProductId == item.ProductId)
                    .Sum(detail => detail.Quantity);

                if (item.Quantity + reservedQuantity > product.Quantity)
                {
                    throw new InvalidOperationException(
                        $"Sản phẩm \"{product.Name}\" không đủ số lượng trong khoảng ngày thuê đã chọn.");
                }
            }

            var rentalDays = Math.Max(1, (endDate - startDate).Days);
            var subtotalPerDay = requestedItems.Sum(item => products[item.ProductId].PricePerDay * item.Quantity);
            var totalAmount = subtotalPerDay * rentalDays;
            var depositAmount = decimal.Round(totalAmount / 2m, 0, MidpointRounding.AwayFromZero);

            var order = new Order
            {
                PublicOrderCode = OrderCodeGenerator.Create(),
                CustomerName = customerInput.CustomerName,
                CustomerPhone = customerInput.CustomerPhone,
                CustomerEmail = customerInput.CustomerEmail.Trim(),
                ShippingAddress = customerInput.ShippingAddress,
                Note = customerInput.Note,
                RentalStartDate = DateTime.SpecifyKind(startDate, DateTimeKind.Unspecified),
                RentalEndDate = DateTime.SpecifyKind(endDate, DateTimeKind.Unspecified),
                OrderDate = DateTime.UtcNow,
                TotalAmount = totalAmount,
                DepositAmount = depositAmount,
                AmountPaid = 0,
                RemainingAmount = totalAmount,
                ReservationExpiresAt = now.AddMinutes(30),
                Status = "Pending",
                PaymentStatus = "Unpaid",
                RefundStatus = "NotRequired",
                PrivacyConsentAt = customerInput.PrivacyConsentAt,
                PrivacyConsentVersion = customerInput.PrivacyConsentVersion,
                RentalPolicyAcceptedAt = customerInput.RentalPolicyAcceptedAt,
                OrderDetails = requestedItems.Select(item => new OrderDetail
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    PricePerUnit = products[item.ProductId].PricePerDay
                }).ToList()
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return order;
        }
    }
}
