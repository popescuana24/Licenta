using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothingWebApp.Models
{
    /// <summary>
    /// Represents a customer order in the system
    /// </summary>
    public class Order
    {
        /// <summary>
        /// Primary key for the order
        /// </summary>
        public int OrderId { get; set; }
        
        /// <summary>
        /// Foreign key to the Customer table
        /// </summary>
        public int CustomerId { get; set; }
        
        /// <summary>
        /// Navigation property to the customer who placed the order
        /// </summary>
        public Customer? Customer { get; set; }
        
        /// <summary>
        /// Date and time when the order was placed
        /// </summary>
        public DateTime OrderDate { get; set; }
        
        /// <summary>
        /// Total monetary amount of the order
        /// </summary>
        public decimal TotalAmount { get; set; }
    }
}