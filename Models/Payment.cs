using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothingWebApp.Models
{
    /// <summary>
    /// Represents a payment for an order in the system
    /// </summary>
    public class Payment
    {
        /// <summary>
        /// Primary key for the payment
        /// </summary>
        public int PaymentId { get; set; }
        
        /// <summary>
        /// Foreign key to the Order table
        /// </summary>
        public int OrderId { get; set; }
        
        /// <summary>
        /// Navigation property to the associated order
        /// </summary>
        public Order? Order { get; set; }
        
        /// <summary>
        /// Payment method used (e.g., Credit Card, PayPal, etc.)
        /// </summary>
        public string PaymentMethod { get; set; } = string.Empty;
        
        /// <summary>
        /// Flag indicating whether the payment has been processed successfully
        /// </summary>
        public bool IsPaid { get; set; } = false;
    }
}