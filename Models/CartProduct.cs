using System.Text.Json.Serialization;

namespace ClothingWebApp.Models
{
    
    public class CartProduct
    {
        // ID of the product in the cart
        public int ProductId { get; set; }
        
        [JsonIgnore]
        public Product? Product { get; set; }
     
        public string Size { get; set; } = string.Empty;
        
        // Quantity of the product in the cart
        public int Quantity { get; set; } = 1;
    }
}