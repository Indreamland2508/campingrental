using BAOCAOWEBNANGCAO.Data;
using BAOCAOWEBNANGCAO.Models;
using BAOCAOWEBNANGCAO.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace BAOCAOWEBNANGCAO.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Staff")]
    public class OrdersController : Controller
    {
        private readonly CampingDbContext _context;
        private readonly OrderNotificationService _notifications;

        public OrdersController(CampingDbContext context, OrderNotificationService notifications)
        {
            _context = context;
            _notifications = notifications;
        }

        // GET: Orders
        public async Task<IActionResult> Index()
        {
            var orders = await _context.Orders.OrderByDescending(order => order.OrderDate).ToListAsync();
            ViewBag.PendingCancellations = orders.Count(order =>
                order.CancellationRequestedAt != null &&
                order.CancellationResolvedAt == null &&
                order.Status != "Cancelled" &&
                order.Status != "Completed" &&
                order.Status != "Expired");
            ViewBag.PendingRentalChanges = orders.Count(order =>
                order.RentalChangeRequestedAt != null &&
                order.RentalChangeResolvedAt == null &&
                order.Status != "Cancelled" &&
                order.Status != "Completed" &&
                order.Status != "Expired");
            return View(orders);
        }

        // GET: Orders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (order == null) return NotFound();

            ViewBag.SePayTransactions = await _context.SePayTransactions
                .Where(transaction => transaction.OrderId == order.Id)
                .OrderByDescending(transaction => transaction.ReceivedAt)
                .ToListAsync();

            return View(order);
        }

        // --- BỎ QUA CREATE VÌ ĐƠN HÀNG DO KHÁCH TẠO TỪ GIỎ HÀNG CHỨ ADMIN KHÔNG TỰ TẠO ---

        // GET: Orders/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,CustomerName,CustomerPhone,CustomerEmail,ShippingAddress,Note")] Order input)
        {
            if (id != input.Id) return NotFound();
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            if (ModelState.IsValid)
            {
                order.CustomerName = input.CustomerName;
                order.CustomerPhone = input.CustomerPhone;
                order.CustomerEmail = input.CustomerEmail;
                order.ShippingAddress = input.ShippingAddress;
                order.Note = input.Note;
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Details), new { id });
            }

            return View(order);
        }

        // GET & POST Delete (Giữ nguyên của Châu)
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var order = await _context.Orders.FirstOrDefaultAsync(m => m.Id == id);
            if (order == null) return NotFound();

            return View(order);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();
            if (order.AmountPaid > 0 ||
                await _context.SePayTransactions.AnyAsync(transaction => transaction.OrderId == order.Id))
            {
                TempData["ErrorMessage"] = "Không thể xóa đơn đã có thanh toán hoặc giao dịch SePay; hãy giữ lại lịch sử và xử lý đơn thay vì xóa.";
                return RedirectToAction(nameof(Details), new { id });
            }

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // --- CẬP NHẬT TRẠNG THÁI GIAO HÀNG & THANH TOÁN ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status, string paymentStatus)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            var oldStatus = order.Status;
            var oldPaymentStatus = order.PaymentStatus;
            if (!string.IsNullOrEmpty(status))
            {
                var allowedStatuses = new[] { "Pending", "Approved", "Delivering", "Completed", "Cancelled" };
                if (!allowedStatuses.Contains(status, StringComparer.Ordinal))
                {
                    return BadRequest();
                }
                if ((order.Status == "Cancelled" || order.Status == "Completed" || order.Status == "Expired") &&
                    status != order.Status)
                {
                    return BadRequest();
                }

                order.Status = status;
                if (status == "Cancelled")
                {
                    order.ReservationExpiresAt = null;
                    order.CancellationResolvedAt = DateTime.UtcNow;
                    order.CancellationResolution = "Đã hủy bởi nhân viên.";
                    SetRefundTerms(order);
                }
            }

            if (!string.IsNullOrEmpty(paymentStatus))
            {
                if (order.Status == "Cancelled" || order.Status == "Completed" || order.Status == "Expired")
                {
                    return BadRequest();
                }

                switch (paymentStatus)
                {
                    case "Deposited":
                        order.AmountPaid = Math.Max(order.AmountPaid, order.DepositAmount);
                        break;
                    case "Paid":
                        order.AmountPaid = order.TotalAmount;
                        break;
                    case "Partial" when order.AmountPaid > 0 && order.AmountPaid < order.DepositAmount:
                        break;
                    case "Unpaid" when order.AmountPaid == 0:
                        break;
                    default:
                        return BadRequest();
                }

                order.PaymentStatus = order.AmountPaid >= order.TotalAmount
                    ? "Paid"
                    : order.AmountPaid >= order.DepositAmount
                        ? "Deposited"
                        : order.AmountPaid > 0
                            ? "Partial"
                            : "Unpaid";
                order.RemainingAmount = Math.Max(0, order.TotalAmount - order.AmountPaid);
                if (order.PaymentStatus == "Partial")
                {
                    order.ReservationExpiresAt = DateTime.UtcNow.AddMinutes(30);
                }
                else if (order.PaymentStatus != "Unpaid")
                {
                    order.ReservationExpiresAt = null;
                }
            }

            await _context.SaveChangesAsync();
            if (oldStatus != order.Status)
            {
                await _notifications.OrderStatusUpdatedAsync(order);
                if (order.Status == "Cancelled")
                {
                    await _notifications.CancellationUpdatedAsync(order);
                }
            }
            if (oldPaymentStatus != order.PaymentStatus)
            {
                await _notifications.PaymentUpdatedAsync(order);
            }

            TempData["SuccessMessage"] = "Cập nhật trạng thái đơn hàng thành công!";
            return RedirectToAction(nameof(Details), new { id = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResolveCancellation(int id, bool approve, string? resolution)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();
            if (order.CancellationRequestedAt == null ||
                order.CancellationResolvedAt != null ||
                order.Status == "Completed" ||
                order.Status == "Cancelled" ||
                order.Status == "Expired")
            {
                return BadRequest();
            }

            order.CancellationResolvedAt = DateTime.UtcNow;
            order.CancellationResolution = string.IsNullOrWhiteSpace(resolution)
                ? (approve ? "Đã duyệt yêu cầu hủy." : "Không duyệt yêu cầu hủy.")
                : resolution.Trim();

            if (approve)
            {
                order.Status = "Cancelled";
                order.ReservationExpiresAt = null;
                SetRefundTerms(order);
            }

            await _context.SaveChangesAsync();
            await _notifications.CancellationUpdatedAsync(order);
            TempData["SuccessMessage"] = approve
                ? order.RefundStatus == "Pending"
                    ? $"Đã duyệt hủy. Cần chuyển khoản hoàn {order.RefundAmount:N0} VNĐ và ghi nhận mã giao dịch."
                    : "Đã duyệt hủy. Theo chính sách, đơn này không phát sinh khoản hoàn tiền."
                : "Đã ghi nhận kết quả xử lý yêu cầu hủy.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResolveRentalChange(int id, bool approve, string? resolution)
        {
            var order = await _context.Orders
                .Include(candidate => candidate.OrderDetails)
                .FirstOrDefaultAsync(candidate => candidate.Id == id);
            if (order == null) return NotFound();
            if (order.RentalChangeRequestedAt == null ||
                order.RentalChangeResolvedAt != null ||
                !order.RequestedRentalStartDate.HasValue ||
                !order.RequestedRentalEndDate.HasValue ||
                (order.Status != "Pending" && order.Status != "Approved"))
            {
                return BadRequest();
            }

            var requestedStart = order.RequestedRentalStartDate.Value.Date;
            var requestedEnd = order.RequestedRentalEndDate.Value.Date;
            if (approve && (requestedStart < VietnamToday() || requestedEnd < requestedStart))
            {
                TempData["ErrorMessage"] = "Lịch khách yêu cầu không còn hợp lệ.";
                return RedirectToAction(nameof(Details), new { id });
            }

            order.RentalChangeResolvedAt = DateTime.UtcNow;
            order.RentalChangeResolution = string.IsNullOrWhiteSpace(resolution)
                ? (approve ? "Đã duyệt yêu cầu đổi lịch." : "Không duyệt yêu cầu đổi lịch.")
                : resolution.Trim();

            if (approve)
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                var productIds = order.OrderDetails.Select(detail => detail.ProductId).Distinct().OrderBy(productId => productId).ToList();
                foreach (var productId in productIds)
                {
                    var product = await _context.Products
                        .FromSqlInterpolated($"SELECT * FROM \"Products\" WHERE \"Id\" = {productId} FOR UPDATE")
                        .SingleOrDefaultAsync();
                    if (product == null)
                    {
                        TempData["ErrorMessage"] = "Không tìm thấy sản phẩm trong đơn.";
                        return RedirectToAction(nameof(Details), new { id });
                    }
                }

                var endExclusive = requestedEnd == requestedStart ? requestedStart.AddDays(1) : requestedEnd;
                var now = DateTime.UtcNow;
                var overlappingOrders = await _context.Orders
                    .Where(other =>
                        other.Id != order.Id &&
                        other.Status != "Cancelled" &&
                        other.Status != "Expired" &&
                        other.RentalStartDate < endExclusive &&
                        (other.RentalEndDate > requestedStart ||
                         (other.RentalEndDate == other.RentalStartDate &&
                          other.RentalStartDate >= requestedStart &&
                          other.RentalStartDate < endExclusive)) &&
                        (other.PaymentStatus == "Paid" ||
                         other.PaymentStatus == "Deposited" ||
                         (other.ReservationExpiresAt != null && other.ReservationExpiresAt > now)))
                    .Include(other => other.OrderDetails)
                    .ToListAsync();
                var products = await _context.Products
                    .Where(product => productIds.Contains(product.Id))
                    .ToDictionaryAsync(product => product.Id);

                foreach (var detail in order.OrderDetails)
                {
                    var reserved = overlappingOrders
                        .SelectMany(other => other.OrderDetails)
                        .Where(otherDetail => otherDetail.ProductId == detail.ProductId)
                        .Sum(otherDetail => otherDetail.Quantity);
                    if (reserved + detail.Quantity > products[detail.ProductId].Quantity)
                    {
                        TempData["ErrorMessage"] =
                            $"Không đủ tồn kho cho {products[detail.ProductId].Name} trong lịch khách yêu cầu.";
                        return RedirectToAction(nameof(Details), new { id });
                    }
                }

                var rentalDays = Math.Max(1, (requestedEnd - requestedStart).Days);
                var calculatedTotal = order.OrderDetails.Sum(detail =>
                    products[detail.ProductId].PricePerDay * detail.Quantity * rentalDays);
                order.TotalAmount = Math.Max(order.TotalAmount, calculatedTotal);
                order.DepositAmount = Math.Max(
                    order.DepositAmount,
                    decimal.Round(order.TotalAmount / 2m, 0, MidpointRounding.AwayFromZero));
                order.RentalStartDate = DateTime.SpecifyKind(requestedStart, DateTimeKind.Unspecified);
                order.RentalEndDate = DateTime.SpecifyKind(requestedEnd, DateTimeKind.Unspecified);
                order.RemainingAmount = Math.Max(0, order.TotalAmount - order.AmountPaid);
                order.PaymentStatus = order.AmountPaid >= order.TotalAmount
                    ? "Paid"
                    : order.AmountPaid >= order.DepositAmount
                        ? "Deposited"
                        : order.AmountPaid > 0
                            ? "Partial"
                            : "Unpaid";
                if (order.PaymentStatus == "Partial" || order.PaymentStatus == "Unpaid")
                {
                    order.ReservationExpiresAt = now.AddMinutes(30);
                }
                else if (order.PaymentStatus == "Deposited" || order.PaymentStatus == "Paid")
                {
                    order.ReservationExpiresAt = null;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            else
            {
                await _context.SaveChangesAsync();
            }

            await _notifications.RentalChangeResolvedAsync(order, approve);
            TempData["SuccessMessage"] = approve
                ? (order.PaymentStatus == "Partial" || order.PaymentStatus == "Unpaid")
                    ? "Đã duyệt lịch mới và giữ chỗ 30 phút để khách hoàn tất tiền cọc."
                    : "Đã duyệt và cập nhật lịch thuê."
                : "Đã từ chối yêu cầu đổi lịch.";
            return RedirectToAction(nameof(Details), new { id });
        }

        private static DateTime VietnamToday()
        {
            var zoneId = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "SE Asia Standard Time"
                : "Asia/Ho_Chi_Minh";
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(zoneId)).Date;
        }

        private static void SetRefundTerms(Order order)
        {
            if (order.AmountPaid <= 0)
            {
                order.RefundAmount = 0;
                order.RefundStatus = "NotRequired";
                return;
            }

            var zoneId = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "SE Asia Standard Time"
                : "Asia/Ho_Chi_Minh";
            var vietnamZone = TimeZoneInfo.FindSystemTimeZoneById(zoneId);
            var requestedAt = order.CancellationRequestedAt ?? DateTime.UtcNow;
            var requestedUtc = requestedAt.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(requestedAt, DateTimeKind.Utc)
                : requestedAt.ToUniversalTime();
            var requestDate = TimeZoneInfo.ConvertTimeFromUtc(requestedUtc, vietnamZone).Date;
            var daysBeforeRental = (order.RentalStartDate.Date - requestDate).Days;

            if (daysBeforeRental >= 3)
            {
                order.RefundAmount = order.AmountPaid;
                order.RefundStatus = "DetailsRequired";
                return;
            }

            order.RefundAmount = 0;
            order.RefundStatus = "NoRefundDue";
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRefund(int id, string refundStatus, string? notes)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();
            if ((order.Status != "Cancelled" && order.Status != "Expired") || order.RefundAmount <= 0 ||
                order.RefundStatus != "Pending" ||
                (refundStatus != "Pending" && refundStatus != "Refunded"))
            {
                return BadRequest();
            }

            if (refundStatus == "Refunded" &&
                (string.IsNullOrWhiteSpace(notes) ||
                 string.IsNullOrWhiteSpace(order.RefundBankName) ||
                 string.IsNullOrWhiteSpace(order.RefundBankAccountNumber) ||
                 string.IsNullOrWhiteSpace(order.RefundBankAccountHolder)))
            {
                TempData["ErrorMessage"] = "Cần có thông tin nhận tiền và mã giao dịch hoàn tiền trước khi xác nhận.";
                return RedirectToAction(nameof(Details), new { id });
            }

            order.RefundStatus = refundStatus;
            order.RefundNotes = notes?.Trim();
            order.RefundProcessedAt = refundStatus == "Refunded" ? DateTime.UtcNow : null;
            if (refundStatus == "Refunded")
            {
                order.RefundBankName = null;
                order.RefundBankAccountNumber = null;
                order.RefundBankAccountHolder = null;
            }
            await _context.SaveChangesAsync();
            await _notifications.CancellationUpdatedAsync(order);
            TempData["SuccessMessage"] = "Đã cập nhật trạng thái hoàn tiền.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}