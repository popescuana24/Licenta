using Microsoft.EntityFrameworkCore;
using ClothingWebApp.Models;

namespace ClothingWebApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Cart> Carts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Categories to accept explicit ID values
            modelBuilder.Entity<Category>()
                .Property(c => c.CategoryId)
                .ValueGeneratedNever();  // This tells EF that the ID is not auto-generated

            // Configure Products to accept explicit ID values
            modelBuilder.Entity<Product>()
                .Property(p => p.ProductId)
                .ValueGeneratedNever();  // Also make ProductId not auto-generated

            // Configure relationships
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Customer)
                .WithMany()
                .HasForeignKey(o => o.CustomerId);
            
            // In ApplicationDbContext.OnModelCreating method
modelBuilder.Entity<Order>()
    .Property(o => o.OrderId)
    .ValueGeneratedNever(); // Allow explicit setting of OrderId

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Order)
                .WithMany()
                .HasForeignKey(p => p.OrderId);

            modelBuilder.Entity<Cart>()
                .HasOne(c => c.Customer)
                .WithMany()
                .HasForeignKey(c => c.CustomerId);

            // Fix decimal precision warnings
            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Order>()
                .Property(o => o.TotalAmount)
                .HasColumnType("decimal(18,2)");

                // Configure Customer's CustomerId as an identity column
modelBuilder.Entity<Customer>()
    .Property(c => c.CustomerId)
    .ValueGeneratedOnAdd();

     modelBuilder.Entity<Cart>()
        .HasMany(c => c.Products)
        .WithMany()
        .UsingEntity(j => j.ToTable("CartProducts"));
        }
    }
}