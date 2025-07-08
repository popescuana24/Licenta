using System.ComponentModel.DataAnnotations;

namespace ClothingWebApp.Models
{
    public class Category
    {
        // Primary key for the category
        public required int CategoryId { get; set; }
        // Name of the category
        public required string Name { get; set; }
        //Description of the category
        public required string Description { get; set; }
        
        // Collection of products belonging to this category
        
        public required ICollection<Product> Products { get; set; }
    }
}
