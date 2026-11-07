namespace BAOCAOWEBNANGCAO.Models
{
    public class OrderLookupChallenge
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string ProtectedCode { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? ConsumedAt { get; set; }
        public int AttemptCount { get; set; }
    }
}
