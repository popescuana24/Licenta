using System.ComponentModel.DataAnnotations.Schema;

namespace ClothingWebApp.Models
{
    public class Cart
    {
        public int CartId { get; set; }
        public int CustomerId { get; set; }
        public string CartItemsJson { get; set; } = string.Empty;
        
        public Customer? Customer { get; set; }
        
        [NotMapped]
        public List<CartProduct> CartItems { get; set; } = new List<CartProduct>();
    }
}