// Update your Cart.cs model
namespace ClothingWebApp.Models
{
    public class Cart 
    {
        public int CartId { get; set; }  // Primary key
        public required int CustomerId { get; set; }  // Foreign key
        public required Customer Customer { get; set; }
        
        public List<Product> Products { get; set; } = new List<Product>();
    }
}