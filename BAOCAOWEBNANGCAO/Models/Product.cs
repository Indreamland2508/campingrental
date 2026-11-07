using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BAOCAOWEBNANGCAO.Models
{
    public class Product
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public string? Description { get; set; }
        [Column(TypeName = "decimal(18,2)")] // Định dạng tiền tệ chuẩn SQL
        public decimal PricePerDay { get; set; }
        public string? ImageUrl { get; set; }
        public int Quantity { get; set; } // Tồn kho
        [Range(1, 100)]
        public int? MaxGuests { get; set; }

        public int CategoryId { get; set; }
        public Category? Category { get; set; }
    }
}