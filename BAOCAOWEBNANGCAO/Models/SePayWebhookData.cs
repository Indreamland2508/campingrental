namespace BAOCAOWEBNANGCAO.Models
{
    public class SePayWebhookData
    {
        public int id { get; set; }
        public string gateway { get; set; } // Tên ngân hàng (MB, VCB...)
        public string transactionDate { get; set; } // Thời gian giao dịch
        public string accountNumber { get; set; } // Số tài khoản ngân hàng của bạn
        public string subAccount { get; set; } // Tài khoản phụ (nếu có)
        public decimal amount { get; set; } // Số tiền giao dịch
        public string transferType { get; set; } // "in" là tiền vào, "out" là tiền ra
        public string transferContent { get; set; } // Nội dung chuyển khoản
        public string referenceCode { get; set; } // Mã tham chiếu của ngân hàng
        public string body { get; set; } // Nội dung SMS nguyên bản
    }
}