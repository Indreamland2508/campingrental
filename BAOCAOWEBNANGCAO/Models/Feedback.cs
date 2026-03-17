namespace BAOCAOWEBNANGCAO.Models
{
    public class Feedback
    {
        public int Id { get; set; }
        public string CustomerName { get; set; }
        public string Content { get; set; }
        public int Rating { get; set; } // 1-5 sao
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsApproved { get; set; } = false; // Admin duyệt mới hiện
    }
}