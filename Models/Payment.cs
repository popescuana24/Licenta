using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothingWebApp.Models
{    public class Payment
    {
        //Primary key for the payment
        public int PaymentId { get; set; }
        //Foreign key to the Order table
        public int OrderId { get; set; }
       
        public Order? Order { get; set; }
        //Payment method 
        public string PaymentMethod { get; set; } = string.Empty;

        public bool IsPaid { get; set; } = false;
    }
}