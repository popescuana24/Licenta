using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothingWebApp.Models
{
    
    /// Represents individual items within an order
    /// This creates the proper many-to-many relationship between Orders and Products
    public class OrderItem
    {
    
        /// Primary key for the order item
        public int OrderItemId { get; set; }
        
        /// Foreign key to the Order table
        public int OrderId { get; set; }
        
        /// Foreign key to the Product table
        public int ProductId { get; set; }
        
        /// Quantity of this product in the order
        public int Quantity { get; set; }
        
        /// Size selected for this product
        public string Size { get; set; } = string.Empty;

        /// Price per unit at the time of order (CRITICAL for price history)

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        /// Total price for this line item (Quantity * UnitPrice)
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }
        
        /// Product name at time of order (in case product name changes)
        public string ProductName { get; set; } = string.Empty;
        
        /// Navigation property to the associated order
        public Order? Order { get; set; }
        
        /// Navigation property to the associated product
        public Product? Product { get; set; }
    }
}