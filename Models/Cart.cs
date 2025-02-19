namespace ClothingWebApp.Models;

public class Cart 
{
    public required int CartId { get; set; }  // Primary key
    public required int CustomerId { get; set; }  // Foreign key
    public required Customer Customer { get; set; }
    
    public required ICollection<Product> Products { get; set; }

}