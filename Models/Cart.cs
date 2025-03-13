namespace ClothingWebApp.Models
{
    public class Cart
    {
        public int CartId { get; set; }  // Primary key
        public int CustomerId { get; set; }  // Foreign key
        
        // Make this nullable to resolve the non-nullable reference warning
        public Customer? Customer { get; set; }
        
        // Initialize the list to avoid null reference exceptions
        public List<CartProduct> CartItems { get; set; } = new List<CartProduct>();
    }

    // This is just a helper class, not a database entity
    public class CartProduct
    {
        public int ProductId { get; set; }
        
        // Make Product nullable
        public Product? Product { get; set; }
        
        // Initialize with empty string to solve non-null requirement
        public string Size { get; set; } = string.Empty;
        
        public int Quantity { get; set; } = 1;
    }
}