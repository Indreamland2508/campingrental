using System.ComponentModel.DataAnnotations;

namespace BAOCAOWEBNANGCAO.Models
{
    public class Combo
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên combo không được để trống")]
        [StringLength(100)]
        public string Name { get; set; }

        public string? Description { get; set; }

        public string? ImageUrl { get; set; }

        public decimal Price { get; set; } // Giá thuê cả gói

        public string? Badge { get; set; } // Nhãn dán (ví dụ: "Best Seller", "Cao Cấp")

        public bool IsActive { get; set; } = true; // Bật/Tắt hiển thị trên web

        // Mối quan hệ: 1 Combo có nhiều Chi tiết Combo (Sản phẩm)
        public virtual ICollection<ComboDetail> ComboDetails { get; set; }
    }
}