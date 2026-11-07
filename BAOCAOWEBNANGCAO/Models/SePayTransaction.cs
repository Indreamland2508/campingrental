namespace BAOCAOWEBNANGCAO.Models
{
    public class SePayTransaction
    {
        public int Id { get; set; }
        public long SePayId { get; set; }
        public string? Gateway { get; set; }
        public string? TransactionDate { get; set; }
        public string? AccountNumber { get; set; }
        public string? ReferenceCode { get; set; }
        public string? TransferType { get; set; }
        public decimal TransferAmount { get; set; }
        public string? Content { get; set; }
        public int? OrderId { get; set; }
        public bool AppliedToOrder { get; set; }
        public DateTime ReceivedAt { get; set; }
    }
}
