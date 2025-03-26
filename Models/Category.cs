using System.ComponentModel.DataAnnotations;

namespace ClothingWebApp.Models
{
    /// <summary>
    /// Represents a product category in the system
    /// </summary>
    public class Category
    {
        /// <summary>
        /// Primary key for the category
        /// </summary>
        public required int CategoryId { get; set; }
        
        /// <summary>
        /// Name of the category
        /// </summary>
        public required string Name { get; set; }
        
        /// <summary>
        /// Description of the category
        /// </summary>
        public required string Description { get; set; }
        
        /// <summary>
        /// Collection of products belonging to this category
        /// Used for navigation in entity framework
        /// </summary>
        public required ICollection<Product> Products { get; set; }
    }
}