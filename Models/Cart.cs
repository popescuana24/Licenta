using System.ComponentModel.DataAnnotations.Schema;

namespace ClothingWebApp.Models
{
    
    public class Cart
    {
        
        // this is the primary key
        public int CartId { get; set; }
        
        // Foreign key to the Customer table
        public int CustomerId { get; set; }
        
       
        /// JSON string of CART ITEMS
        public string CartItemsJson { get; set; } = string.Empty;
        
      
        //Navigation property to the associated Customer
        public Customer? Customer { get; set; }
        
      
        /// Deserialized colle ction of cart items, not stored in the database
        [NotMapped]
        public List<CartProduct> CartItems { get; set; } = new List<CartProduct>();
    }
}