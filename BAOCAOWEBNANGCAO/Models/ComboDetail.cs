using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BAOCAOWEBNANGCAO.Models
{
    public class ComboDetail
    {
        [Key]
        public int Id { get; set; }

        public int ComboId { get; set; }
        [ForeignKey("ComboId")]
        public virtual Combo Combo { get; set; }

        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }

        public int Quantity { get; set; } = 1; // Số lượng món đồ này trong Combo
    }
}