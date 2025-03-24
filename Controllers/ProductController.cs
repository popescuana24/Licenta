using ClothingWebApp.Data;
using ClothingWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ClothingWebApp.Controllers
{
    /// <summary>
    /// Handles product display and management
    /// </summary>
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        
        public ProductController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Shows all products (admin view)
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .ToListAsync();
            
            return View(products);
        }

        /// <summary>
        /// Shows details for a specific product
        /// </summary>
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

        /// <summary>
        /// Shows form to create a new product (admin functionality)
        /// </summary>
        public async Task<IActionResult> Create()
        {
            ViewData["CategoryId"] = new SelectList(await _context.Categories.ToListAsync(), "CategoryId", "Name");
            return View();
        }

        /// <summary>
        /// Handles creation of a new product
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product)
        {
            if (ModelState.IsValid)
            {
                _context.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            
            ViewData["CategoryId"] = new SelectList(await _context.Categories.ToListAsync(), "CategoryId", "Name", product.CategoryId);
            return View(product);
        }

        /// <summary>
        /// Shows form to edit an existing product (admin functionality)
        /// </summary>
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

        /// <summary>
        /// Handles editing of an existing product
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
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

        /// <summary>
        /// Shows confirmation page for deleting a product (admin functionality)
        /// </summary>
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.ProductId == id);
                
            if (product == null)
            {
                return NotFound();
            }
            
            return View(product);
        }

        /// <summary>
        /// Handles confirmation of product deletion
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        
        /// <summary>
        /// Helper method to check if a product exists by ID
        /// </summary>
        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.ProductId == id);
        }
    }
}