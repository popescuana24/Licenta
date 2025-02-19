namespace ClothingWebApp.Models;
public class Payment
{
    public required int PaymentId { get; set; } 
    public required int OrderId { get; set; }  
    public required Order Order { get; set; }  
    public required string PaymentMethod { get; set; }  
    public required bool IsPaid { get; set; } = false;  
}