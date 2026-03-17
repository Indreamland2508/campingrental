using System.ComponentModel.DataAnnotations;

namespace BAOCAOWEBNANGCAO.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        public DateTime OrderDate { get; set; }

        [Required]
        public string CustomerName { get; set; }

        [Required]
        public string CustomerPhone { get; set; }

        [Required]
        public string CustomerEmail { get; set; }

        // --- ĐÃ XÓA CitizenId ---

        public string ShippingAddress { get; set; } // Trường địa chỉ giao hàng
        public string? Note { get; set; }           // Trường Ghi chú

        public DateTime RentalStartDate { get; set; }
        public DateTime RentalEndDate { get; set; }

        public decimal TotalAmount { get; set; }

        // --- 🌟 CÁC TRƯỜNG MỚI THÊM CHO HỆ THỐNG CỌC 🌟 ---

        public decimal DepositAmount { get; set; } // Tiền khách cần cọc (VD: 50% TotalAmount)
        public decimal RemainingAmount { get; set; } // Tiền còn lại thu tiền mặt khi giao lều

        public string PaymentStatus { get; set; } // Trạng thái tiền nong: "Unpaid" (Chưa trả), "Deposited" (Đã cọc), "Paid" (Đã thanh toán đủ)

        // ------------------------------------------------

        public string Status { get; set; } // Trạng thái giao lều: "Pending", "Delivering", "Completed"

        public List<OrderDetail> OrderDetails { get; set; }
    }
}