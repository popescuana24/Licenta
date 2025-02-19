namespace  ClothingWebApp.Models;

public class Order
{
     public required int OrderId { get; set; } 
    public required int CustomerId { get; set; } 
    public required Customer Customer { get; set; }
    public required DateTime OrderDate { get; set; }
    public required decimal TotalAmount { get; set; } 
    
   
}

