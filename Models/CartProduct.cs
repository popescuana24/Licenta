// Models/CartProduct.cs
using System.Text.Json.Serialization;

namespace ClothingWebApp.Models
{
    // This is NOT a database entity, just a helper class
    public class CartProduct
    {
        public int ProductId { get; set; }
        
        [JsonIgnore] 
        public Product? Product { get; set; }
        
        public string Size { get; set; } = string.Empty;
        
        public int Quantity { get; set; } = 1;
    }
}