namespace ClothingWebApp.Models;

public class Category
{
    public required int CategoryId { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required ICollection<Product> Products { get; set; }
}
