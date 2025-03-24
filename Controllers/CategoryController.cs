using ClothingWebApp.Data;
using ClothingWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClothingWebApp.Controllers
{
    /// <summary>
    /// Handles product categories display and browsing
    /// </summary>
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CategoryController> _logger;

        public CategoryController(ApplicationDbContext context, ILogger<CategoryController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Shows a list of all product categories
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories.ToListAsync();
            
            // Get product counts for each category
            var categoriesWithCounts = new List<dynamic>();
            foreach (var category in categories)
            {
                var count = await _context.Products.CountAsync(p => p.CategoryId == category.CategoryId);
                categoriesWithCounts.Add(new { Category = category, ProductCount = count });
            }
            
            ViewBag.CategoriesWithCounts = categoriesWithCounts;
            return View(categories);
        }

        /// <summary>
        /// Shows details of a specific category and its products
        /// </summary>
        public async Task<IActionResult> Details(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            
            var products = await _context.Products
                .Where(p => p.CategoryId == id)
                .ToListAsync();
            
            ViewBag.Products = products;
            return View(category);
        }

        // Category-specific convenience methods
        public async Task<IActionResult> Bags() => await GetCategoryByName("BAGS");
        public async Task<IActionResult> Blazers() => await GetCategoryByName("BLAZERS");
        public async Task<IActionResult> Dresses() => await GetCategoryByName("DRESSES/JUMPSUITS");
        public async Task<IActionResult> Jackets() => await GetCategoryByName("JACKETS");
        public async Task<IActionResult> Shirts() => await GetCategoryByName("SHIRTS");
        public async Task<IActionResult> Shoes() => await GetCategoryByName("SHOES");
        public async Task<IActionResult> Sweaters() => await GetCategoryByName("SWEATERS");
        public async Task<IActionResult> Skirts() => await GetCategoryByName("SKIRTS");
        public async Task<IActionResult> Tops() => await GetCategoryByName("T-SHIRT/TOPS");

        /// <summary>
        /// Helper method to get category by name
        /// </summary>
        private async Task<IActionResult> GetCategoryByName(string categoryName)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Name == categoryName);
                
            if (category == null)
            {
                return NotFound();
            }
            
            var products = await _context.Products
                .Where(p => p.CategoryId == category.CategoryId)
                .ToListAsync();
                
            ViewBag.Products = products;
            return View("Details", category);
        }
    }
}