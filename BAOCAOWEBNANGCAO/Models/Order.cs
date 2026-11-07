using System.ComponentModel.DataAnnotations;

namespace BAOCAOWEBNANGCAO.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(19)]
        public string PublicOrderCode { get; set; } = string.Empty;

        public DateTime OrderDate { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "Tên khách hàng không được vượt quá 100 ký tự.")]
        public string CustomerName { get; set; }

        [Required(ErrorMessage = "Số điện thoại là bắt buộc.")]
        [Phone(ErrorMessage = "Số điện thoại không đúng định dạng.")]
        public string CustomerPhone { get; set; }

        [Required(ErrorMessage = "Email liên hệ là bắt buộc.")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        public string CustomerEmail { get; set; }

        // --- ĐÃ XÓA CitizenId ---

        [Required(ErrorMessage = "Địa chỉ nhận lều là bắt buộc.")]
        public string ShippingAddress { get; set; } // Trường địa chỉ giao hàng
        public string? Note { get; set; }           // Trường Ghi chú

        public DateTime RentalStartDate { get; set; }
        public DateTime RentalEndDate { get; set; }

        public decimal TotalAmount { get; set; }

        // --- 🌟 CÁC TRƯỜNG MỚI THÊM CHO HỆ THỐNG CỌC 🌟 ---

        public decimal DepositAmount { get; set; } // Tiền khách cần cọc (VD: 50% TotalAmount)
        public decimal RemainingAmount { get; set; } // Tiền còn lại thu tiền mặt khi giao lều
        public decimal AmountPaid { get; set; }
        public DateTime? ReservationExpiresAt { get; set; }

        public string PaymentStatus { get; set; } = "Unpaid"; // Trạng thái tiền nong: "Unpaid" (Chưa trả), "Deposited" (Đã cọc), "Paid" (Đã thanh toán đủ)
        public string RefundStatus { get; set; } = "NotRequired";
        public decimal RefundAmount { get; set; }
        public DateTime? RefundProcessedAt { get; set; }
        public string? RefundNotes { get; set; }
        [StringLength(100)]
        public string? RefundBankName { get; set; }
        [StringLength(34)]
        public string? RefundBankAccountNumber { get; set; }
        [StringLength(100)]
        public string? RefundBankAccountHolder { get; set; }
        public DateTime? CancellationRequestedAt { get; set; }
        public string? CancellationReason { get; set; }
        public DateTime? CancellationResolvedAt { get; set; }
        public string? CancellationResolution { get; set; }
        public DateTime? RentalChangeRequestedAt { get; set; }
        public DateTime? RequestedRentalStartDate { get; set; }
        public DateTime? RequestedRentalEndDate { get; set; }
        public string? RentalChangeReason { get; set; }
        public DateTime? RentalChangeResolvedAt { get; set; }
        public string? RentalChangeResolution { get; set; }
        public DateTime? PrivacyConsentAt { get; set; }
        [StringLength(32)]
        public string? PrivacyConsentVersion { get; set; }
        public DateTime? RentalPolicyAcceptedAt { get; set; }

        // ------------------------------------------------

        public string Status { get; set; } = "Pending"; // Trạng thái giao lều: "Pending", "Delivering", "Completed"

        public List<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}