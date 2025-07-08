using ClothingWebApp.Data;
using ClothingWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ClothingWebApp.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        
        public ProductController(ApplicationDbContext context)
        {
            _context = context;
        }
        
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            //asynchronously loads all products from the database, including their related categories
            //asynchronously executes the query and returns a list of products
            var products = await _context.Products
                .Include(p => p.Category)
                .ToListAsync(); //product list
                
            return View(products);
        }
        
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.ProductId == id); // Gets the first matching product or null
                
            if (product == null)
            {
                return NotFound();
            }
            
            // Ensure the product has a valid image URL
            if (string.IsNullOrEmpty(product.ImageUrl))
            {
                product.ImageUrl = "/images/products/no-image.jpg";
            }
            
            return View(product);
        }
        
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products.FindAsync(id); // Finds the product by ID
            if (product == null)
            {
                return NotFound();
            }
            
            ViewData["CategoryId"] = new SelectList(await _context.Categories.ToListAsync(), "CategoryId", "Name", product.CategoryId); // Populates the dropdown for categories
            return View(product);
        }
        
        [HttpPost]
        public async Task<IActionResult> Edit(int id, Product product)
        {
            if (id != product.ProductId)
            {
                return NotFound();
            }
            
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(product);
                    await _context.SaveChangesAsync(); // Saves changes to the database
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.ProductId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            
            ViewData["CategoryId"] = new SelectList(await _context.Categories.ToListAsync(), "CategoryId", "Name", product.CategoryId);// Re-populate the category dropdown in case of validation errors
            return View(product);
        }
        
        //delete method
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            
            try
            {
                _context.Products.Remove(product); // Removes the product from the context
                await _context.SaveChangesAsync(); // Deletes the product from the database
                TempData["SuccessMessage"] = "Product deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error deleting product: " + ex.Message;
            }
            
            return RedirectToAction(nameof(Index));
        }
        
        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.ProductId == id); // Checks if a product with the given ID exists
        }
    }
}
