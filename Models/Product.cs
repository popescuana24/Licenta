namespace ClothingWebApp.Models
{
    public class Product  
    {
        public required int ProductId { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public required decimal Price { get; set; }
        public required string Color { get; set; }
        public required string ImageUrl { get; set; }
        public string Size { get; set; } = string.Empty; // Default empty string
        public required int CategoryId { get; set; }
        public Category Category { get; set; } = null!; // Use null! to indicate it will be populated by EF Core
    }
}