namespace BAOCAOWEBNANGCAO.Models
{
    public class CartItem
    {
        public Product Product { get; set; }
        public int Quantity { get; set; }

        // SỬA: Đổi double thành decimal
        public decimal TotalPrice => Product.PricePerDay * Quantity;
    }
}