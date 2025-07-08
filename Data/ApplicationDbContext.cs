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

        // Database tables
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; } 
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

         
            modelBuilder.Entity<Cart>()
                .Property(c => c.CartId)
                .ValueGeneratedOnAdd();

            // CartItemsJson column to store serialized cart items
            modelBuilder.Entity<Cart>()
                .Property<string>("CartItemsJson")
                .HasColumnType("nvarchar(max)"); // This column will store the JSON representation of cart items

            
            modelBuilder.Entity<Cart>()
                .Ignore(c => c.CartItems); // Ignore the CartItems property in the database mapping

            //  SA AM DOUA DECIMALE
            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasColumnType("decimal(18,2)");

            
            modelBuilder.Entity<Order>()
                .Property(o => o.TotalAmount)
                .HasColumnType("decimal(18,2)");


            modelBuilder.Entity<Customer>()
                .Property(c => c.CustomerId)
                .ValueGeneratedOnAdd();
                
            
            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Order) // relationship with the Order entity
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade); // When an order is deleted, its items are also deleted

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Product) // relationship with the Product entity
                .WithMany()
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Restrict); //


            modelBuilder.Entity<OrderItem>()
                .Property(oi => oi.UnitPrice) // unit price of the product at the time of order
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<OrderItem>()
                .Property(oi => oi.TotalPrice) // total price of the order item
                .HasColumnType("decimal(18,2)");
        }
    }
}
