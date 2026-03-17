using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BAOCAOWEBNANGCAO.Models
{
    public class OrderDetail
    {
        [Key]
        public int Id { get; set; }

        public int OrderId { get; set; }
        [ForeignKey("OrderId")]
        public Order Order { get; set; }

        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public Product Product { get; set; }

        public int Quantity { get; set; }

        // Cột bị thiếu đây:
        [Column(TypeName = "decimal(18,2)")]
        public decimal PricePerUnit { get; set; }
    }
}