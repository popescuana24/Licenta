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
            var products = await _context.Products
                .Include(p => p.Category)
                .ToListAsync();
                
            return View(products);
        }
        
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.ProductId == id);
                
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
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            
            ViewData["CategoryId"] = new SelectList(await _context.Categories.ToListAsync(), "CategoryId", "Name", product.CategoryId);
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
                    await _context.SaveChangesAsync();
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
            
            ViewData["CategoryId"] = new SelectList(await _context.Categories.ToListAsync(), "CategoryId", "Name", product.CategoryId);
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
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
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
            return _context.Products.Any(e => e.ProductId == id);
        }
    }
}