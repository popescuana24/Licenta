namespace ClothingWebApp.Models;
public class Payment
{
    public int PaymentId { get; set; } 
    public int OrderId { get; set; }  
    public Order? Order { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;  
    public bool IsPaid { get; set; } = false;  
}