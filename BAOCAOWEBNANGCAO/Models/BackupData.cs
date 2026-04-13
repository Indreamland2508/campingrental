using System;
using System.Collections.Generic;

namespace BAOCAOWEBNANGCAO.Models
{
    public class BackupData
    {
        public DateTime CreatedAt { get; set; }
        public List<CategoryBackup> Categories { get; set; } = new();
        public List<ProductBackup> Products { get; set; } = new();
        public List<ComboBackup> Combos { get; set; } = new();
        public List<ComboDetailBackup> ComboDetails { get; set; } = new();
        public List<OrderBackup> Orders { get; set; } = new();
        public List<OrderDetailBackup> OrderDetails { get; set; } = new();
        public List<FeedbackBackup> Feedbacks { get; set; } = new();
    }

    public class CategoryBackup
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
    }

    public class ProductBackup
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public decimal PricePerDay { get; set; }
        public string? ImageUrl { get; set; }
        public int Quantity { get; set; }
        public int CategoryId { get; set; }
    }

    public class ComboBackup
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public decimal Price { get; set; }
        public string? Badge { get; set; }
        public bool IsActive { get; set; }
    }

    public class ComboDetailBackup
    {
        public int Id { get; set; }
        public int ComboId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class OrderBackup
    {
        public int Id { get; set; }
        public DateTime OrderDate { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string CustomerEmail { get; set; }
        public string ShippingAddress { get; set; }
        public string? Note { get; set; }
        public DateTime RentalStartDate { get; set; }
        public DateTime RentalEndDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DepositAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public string PaymentStatus { get; set; }
        public string Status { get; set; }
    }

    public class OrderDetailBackup
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal PricePerUnit { get; set; }
    }

    public class FeedbackBackup
    {
        public int Id { get; set; }
        public string CustomerName { get; set; }
        public string Content { get; set; }
        public int Rating { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsApproved { get; set; }
    }
}
