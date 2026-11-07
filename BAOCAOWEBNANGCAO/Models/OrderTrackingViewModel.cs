using System.ComponentModel.DataAnnotations;

namespace BAOCAOWEBNANGCAO.Models
{
    public class OrderTrackingViewModel
    {
        [Required]
        [StringLength(19, MinimumLength = 19, ErrorMessage = "Mã đơn hàng không hợp lệ.")]
        public string PublicOrderCode { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string? Code { get; set; }
        public string? Message { get; set; }
        public Order? Order { get; set; }
        public bool CodeSent { get; set; }
        public bool CanRequestCancellation { get; set; }
        public bool CanRequestRentalChange { get; set; }
    }
}
