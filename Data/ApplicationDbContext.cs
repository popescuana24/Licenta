using Microsoft.EntityFrameworkCore;
using ClothingWebApp.Models;

namespace ClothingWebApp.Data
{
    /// <summary>
    /// Main database context for the application
    /// Defines database tables and their relationships
    /// </summary>
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
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Cart> Carts { get; set; }

        /// <summary>
        /// Configures database schema and relationships
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Categories to use explicit IDs (not auto-generated)
            modelBuilder.Entity<Category>()
                .Property(c => c.CategoryId)
                .ValueGeneratedNever();

            // Configure Products to use explicit IDs (not auto-generated)
            modelBuilder.Entity<Product>()
                .Property(p => p.ProductId)
                .ValueGeneratedNever();

            // Configure relationship: Products belong to Categories
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId);

            // Configure relationship: Orders belong to Customers
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Customer)
                .WithMany()
                .HasForeignKey(o => o.CustomerId);

            // Configure OrderId as auto-generated
            modelBuilder.Entity<Order>()
                .Property(o => o.OrderId)
                .ValueGeneratedOnAdd();

            // Configure relationship: Payments belong to Orders
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Order)
                .WithMany()
                .HasForeignKey(p => p.OrderId);

            // Configure relationship: Carts belong to Customers
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
                
            // Ignore CartItems navigation property (handled through serialization)
            modelBuilder.Entity<Cart>()
                .Ignore(c => c.CartItems);

            // Fix decimal precision for Price in Products
            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasColumnType("decimal(18,2)");

            // Fix decimal precision for TotalAmount in Orders
            modelBuilder.Entity<Order>()
                .Property(o => o.TotalAmount)
                .HasColumnType("decimal(18,2)");

            // Configure CustomerId as auto-generated
            modelBuilder.Entity<Customer>()
                .Property(c => c.CustomerId)
                .ValueGeneratedOnAdd();
        }
    }
}