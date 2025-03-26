using System.Text.Json.Serialization;

namespace ClothingWebApp.Models
{
    /// <summary>
    /// Helper class representing a product in a cart with size and quantity
    /// This is NOT a database entity, just a helper class
    /// </summary>
    public class CartProduct
    {
        /// <summary>
        /// ID of the product in the cart
        /// </summary>
        public int ProductId { get; set; }
        
        /// <summary>
        /// Navigation property to the associated Product
        /// Excluded from JSON serialization to avoid circular references
        /// </summary>
        [JsonIgnore]
        public Product? Product { get; set; }
        
        /// <summary>
        /// Selected size for the product
        /// </summary>
        public string Size { get; set; } = string.Empty;
        
        /// <summary>
        /// Quantity of the product in the cart
        /// </summary>
        public int Quantity { get; set; } = 1;
    }
}