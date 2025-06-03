using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothingWebApp.Models
{
    public class Product
    {
        //Primary key for the product
        public required int ProductId { get; set; }
        
        //Name of the product
        public required string Name { get; set; }
        
        //Detailed description of the product
        public required string Description { get; set; }
        
        //Price of the product
        public required decimal Price { get; set; }
        
        //Color of the product
        public required string Color { get; set; }
        //PATH to the product image
        public required string ImageUrl { get; set; }
        //Size of the product
        public string Size { get; set; } = string.Empty;
        //Foreign key to the Category table
        public required int CategoryId { get; set; }
        public Category Category { get; set; } = null!;
    }
}