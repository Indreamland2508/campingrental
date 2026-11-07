namespace BAOCAOWEBNANGCAO.Models
{
    public class SePayWebhookData
    {
        public long id { get; set; }
        public string? gateway { get; set; }
        public string? transactionDate { get; set; }
        public string? accountNumber { get; set; }
        public string? subAccount { get; set; }
        public string? code { get; set; }
        public string? content { get; set; }
        public string? transferType { get; set; }
        public decimal transferAmount { get; set; }
        public string? referenceCode { get; set; }
        public string? description { get; set; }
        public decimal? accumulated { get; set; }
    }
}