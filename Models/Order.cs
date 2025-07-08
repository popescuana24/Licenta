using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothingWebApp.Models
{
    public class Order
    {
        //Primary key for the order
        public int OrderId { get; set; }
        
        //Foreign key to the Customer table
        public int CustomerId { get; set; }
        
        //Navigation property to the customer who placed the order
        public Customer? Customer { get; set; }
        
        //Date and time when the order was placed
        public DateTime OrderDate { get; set; }
        
        //Total amount of the order
        public decimal TotalAmount { get; set; }
        
        // this is a list with all order history with products, quantities, and prices
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
