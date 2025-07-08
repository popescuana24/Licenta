using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothingWebApp.Models
{
    public class Customer
    {
        // Primary key for the customer
        public int CustomerId { get; set; }
        // Customer's first name
        public required string FirstName { get; set; }
        //Customer's last name
        public required string LastName { get; set; }
        //Customer's email address
        public required string Email { get; set; }
        //Customer's shipping address
        public required string Address { get; set; }
        
        // ustomer's hashed password
        public required string Password { get; set; }
        //property that returns the full name of the customer
        public string FullName => $"{FirstName} {LastName}"; //$ is used for string 
    }
}
