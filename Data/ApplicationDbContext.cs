using Microsoft.EntityFrameworkCore;
using ClothingWebApp.Models;

namespace ClothingWebApp.Data
{
    
    /// Main database context for the application
    /// Defines database tables and their relationships
  
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Database tables
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; } // ADD THIS LINE
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Cart> Carts { get; set; }


        //SCHEMA LA BAZA DE DATE SE CREAZA !
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            modelBuilder.Entity<Category>()
                .Property(c => c.CategoryId)
                .ValueGeneratedNever();


            modelBuilder.Entity<Product>()
                .Property(p => p.ProductId)
                .ValueGeneratedNever();


            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId);


            modelBuilder.Entity<Order>()
                .HasOne(o => o.Customer)
                .WithMany()
                .HasForeignKey(o => o.CustomerId);

            modelBuilder.Entity<Order>()
                .Property(o => o.OrderId)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Order)
                .WithMany()
                .HasForeignKey(p => p.OrderId);


            modelBuilder.Entity<Cart>()
                .HasOne(c => c.Customer)
                .WithMany()
                .HasForeignKey(c => c.CustomerId);

            // Configure CartId as auto-generated
            modelBuilder.Entity<Cart>()
                .Property(c => c.CartId)
                .ValueGeneratedOnAdd();

            // Add CartItemsJson column to store serialized cart items
            modelBuilder.Entity<Cart>()
                .Property<string>("CartItemsJson")
                .HasColumnType("nvarchar(max)");

            // Ignore CartItems navigation property 
            modelBuilder.Entity<Cart>()
                .Ignore(c => c.CartItems);

            // Fix decimal precision for Price in Products SA AM DOUA DECIMALE
            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasColumnType("decimal(18,2)");

            // Fix decimal precision for TotalAmount in Orders
            modelBuilder.Entity<Order>()
                .Property(o => o.TotalAmount)
                .HasColumnType("decimal(18,2)");


            modelBuilder.Entity<Customer>()
                .Property(c => c.CustomerId)
                .ValueGeneratedOnAdd();
                
            // NEW: OrderItem relationships
            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Product)
                .WithMany()
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

// Configure decimal precision for OrderItem prices
            modelBuilder.Entity<OrderItem>()
                .Property(oi => oi.UnitPrice)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<OrderItem>()
                .Property(oi => oi.TotalPrice)
                .HasColumnType("decimal(18,2)");
        }
    }
}