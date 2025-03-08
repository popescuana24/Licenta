namespace ClothingWebApp.Models;

public class Order
{
    public int OrderId { get; set; } // Changed from 'required' to allow setting
    public required int CustomerId { get; set; } 
    public required Customer Customer { get; set; }
    public required DateTime OrderDate { get; set; }
    public required decimal TotalAmount { get; set; }
}