using System.ComponentModel.DataAnnotations.Schema;

namespace ClothingWebApp.Models
{
    /// <summary>
    /// Represents a customer's shopping cart in the database
    /// </summary>
    public class Cart
    {
        /// <summary>
        /// Primary key for the cart
        /// </summary>
        public int CartId { get; set; }
        
        /// <summary>
        /// Foreign key to the Customer table
        /// </summary>
        public int CustomerId { get; set; }
        
        /// <summary>
        /// JSON string representation of cart items to avoid complex many-to-many relationship
        /// </summary>
        public string CartItemsJson { get; set; } = string.Empty;
        
        /// <summary>
        /// Navigation property to the associated Customer
        /// </summary>
        public Customer? Customer { get; set; }
        
        /// <summary>
        /// Deserialized collection of cart items, not stored in the database
        /// </summary>
        [NotMapped]
        public List<CartProduct> CartItems { get; set; } = new List<CartProduct>();
    }
}