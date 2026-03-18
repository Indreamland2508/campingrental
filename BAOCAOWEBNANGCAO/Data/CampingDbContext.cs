using BAOCAOWEBNANGCAO.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace BAOCAOWEBNANGCAO.Data
{
    public class CampingDbContext : IdentityDbContext // Kế thừa Identity để có sẵn bảng User Admin
    {
        public CampingDbContext(DbContextOptions<CampingDbContext> options)
            : base(options)
        {
        }
        public DbSet<Combo> Combos { get; set; }
        public DbSet<ComboDetail> ComboDetails { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // BẮT BUỘC để Identity hoạt động

            // Seed dữ liệu mẫu cho Category để test nhanh
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Lều cắm trại", Description = "Các loại lều 2-10 người" },
                new Category { Id = 2, Name = "Dụng cụ nấu ăn", Description = "Bếp, nồi, vỉ nướng" },
                new Category { Id = 3, Name = "Phụ kiện dã ngoại", Description = "Đèn, túi ngủ, ghế xếp" }
            );
        }
        public DbSet<Feedback> Feedbacks { get; set; }
        
    }
}