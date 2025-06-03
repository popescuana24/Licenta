using ClothingWebApp.Data;
using ClothingWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClothingWebApp.Controllers
{
    
     public class CategoryController : Controller
{
     private readonly ApplicationDbContext _context;
    
    public CategoryController(ApplicationDbContext context)
    {
        _context = context;
    }
    
    // GET requests to /Category/Details/
    public async Task<IActionResult> Details(int id)
       {
           // Creates an efficient query that retrieves both the category and its products in one database operation
           var categoryWithProducts = await _context.Categories
               .Where(c => c.CategoryId == id)  // Filters categories to find the one with matching ID
               .Select(c => new               // Projects results into an anonymous type
               {
                   Category = c,              // Includes the full category object
                   Products = _context.Products.Where(p => p.CategoryId == c.CategoryId).ToList()  // Gets all products in this category
               })
               .FirstOrDefaultAsync();        // Executes query and returns first match or null if none found
           
           // If no category was found with the specified ID, return a 404 Not Found response
           if (categoryWithProducts == null)
           {
               return NotFound();
           }
           
           // Puts the products list into ViewBag for access in the view
           ViewBag.Products = categoryWithProducts.Products;
           // Returns the Details view with the category as the model
           return View(categoryWithProducts.Category);
       }
    
    public async Task<IActionResult> ByName(string name)
       {
           // If no name was provided or it's empty, redirect to the category index page
           if (string.IsNullOrEmpty(name))
           {
               return RedirectToAction(nameof(Index));
           }
           
           // Search for a category with a matching name (case-insensitive)
           var category = await _context.Categories
               .FirstOrDefaultAsync(c => c.Name.ToUpper() == name.ToUpper());
           
           // If no matching category was found, return a 404 Not Found response
           if (category == null)
           {
               return NotFound();
           }
           
           return RedirectToAction(nameof(Details), new { id = category.CategoryId });
       }
}
}