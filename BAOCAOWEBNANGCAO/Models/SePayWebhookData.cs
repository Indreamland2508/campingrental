namespace BAOCAOWEBNANGCAO.Models
{
    public class SePayWebhookData
    {
        public int id { get; set; }
        public string gateway { get; set; }
        public string transactionDate { get; set; }
        public string accountNumber { get; set; }
        public string? subAccount { get; set; }
        public string? code { get; set; }
        public string content { get; set; } // <--- Đổi thành content
        public string transferType { get; set; }
        public int transferAmount { get; set; } // <--- Đổi thành transferAmount, để kiểu int
        public string referenceCode { get; set; }
        public string? description { get; set; }
        public int? accumulated { get; set; }
    }
}