using System.Text.Json.Serialization;

namespace ClothingWebApp.Models
{
    
    public class CartProduct
    {
        // ID of the product in the cart
        public int ProductId { get; set; }


        [JsonIgnore]
        //json ignore to avoid circular reference issues
        public Product? Product { get; set; } // Navigationto the associated Product
     
        public string Size { get; set; } = string.Empty;
        
        // Quantity of the product in the cart
        public int Quantity { get; set; } = 1;
    }
}
