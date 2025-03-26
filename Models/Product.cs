using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothingWebApp.Models
{
    /// <summary>
    /// Represents a product in the clothing web store
    /// </summary>
    public class Product
    {
        /// <summary>
        /// Primary key for the product
        /// </summary>
        public required int ProductId { get; set; }
        
        /// <summary>
        /// Name of the product
        /// </summary>
        public required string Name { get; set; }
        
        /// <summary>
        /// Detailed description of the product
        /// </summary>
        public required string Description { get; set; }
        
        /// <summary>
        /// Price of the product
        /// </summary>
        public required decimal Price { get; set; }
        
        /// <summary>
        /// Color of the product
        /// </summary>
        public required string Color { get; set; }
        
        /// <summary>
        /// URL to the product image
        /// </summary>
        public required string ImageUrl { get; set; }
        
        /// <summary>
        /// Size of the product, defaults to empty string
        /// </summary>
        public string Size { get; set; } = string.Empty;
        
        /// <summary>
        /// Foreign key to the Category table
        /// </summary>
        public required int CategoryId { get; set; }
        
        /// <summary>
        /// Navigation property to the product's category
        /// null! indicates it will be populated by EF Core during query execution
        /// </summary>
        public Category Category { get; set; } = null!;
    }
}