namespace ClothingWebApp.Models;

public class Order
{
    public int OrderId { get; set; } // Remove 'required' to allow auto-generation
    public int CustomerId { get; set; } 
    public Customer? Customer { get; set; } // Make nullable to prevent issues
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
}