using ClothingWebApp.Data;
using ClothingWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClothingWebApp.Controllers
{
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CategoryController> _logger;

        public CategoryController(ApplicationDbContext context, ILogger<CategoryController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Category
        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories.ToListAsync();
            _logger.LogInformation($"Found {categories.Count} categories");
            
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

        // GET: Category/Details/5
        public async Task<IActionResult> Details(int id)
        {
            _logger.LogInformation($"Looking for category with ID: {id}");
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                _logger.LogWarning($"Category with ID {id} not found");
                return NotFound();
            }

            _logger.LogInformation($"Found category: {category.Name}");
            
            // Check total product count
            var totalProductCount = await _context.Products.CountAsync();
            _logger.LogInformation($"Total products in database: {totalProductCount}");
            
            var products = await _context.Products
                .Where(p => p.CategoryId == id)
                .ToListAsync();

            _logger.LogInformation($"Found {products.Count} products in category {category.Name}");
            
            if (products.Count == 0)
            {
                _logger.LogWarning($"No products found for category {category.Name} (ID: {id})");
                
                // Log some sample products for diagnostics
                var sampleProducts = await _context.Products.Take(5).ToListAsync();
                foreach (var prod in sampleProducts)
                {
                    _logger.LogInformation($"Sample product: ID={prod.ProductId}, Name={prod.Name}, CategoryID={prod.CategoryId}");
                }
            }
            
            ViewBag.Products = products;
            return View(category);
        }

        // Add these methods to your CategoryController
public async Task<IActionResult> Bags()
{
    return await GetCategoryByName("BAGS");
}

public async Task<IActionResult> Blazers()
{
    return await GetCategoryByName("BLAZERS");
}

public async Task<IActionResult> Dresses()
{
    return await GetCategoryByName("DRESSES/JUMPSUITS");
}

public async Task<IActionResult> Jackets()
{
    return await GetCategoryByName("JACKETS");
}

public async Task<IActionResult> Shirts()
{
    return await GetCategoryByName("SHIRTS");
}

public async Task<IActionResult> Shoes()
{
    return await GetCategoryByName("SHOES");
}

public async Task<IActionResult> Sweaters()
{
    return await GetCategoryByName("SWEATERS");
}

public async Task<IActionResult> Skirts()
{
    return await GetCategoryByName("SKIRTS");
}

public async Task<IActionResult> Tops()
{
    return await GetCategoryByName("T-SHIRT/TOPS");
}

// Helper method to get category by name
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
        
        // Diagnostic endpoint to check database status
        public async Task<IActionResult> CheckDatabase()
        {
            var result = new Dictionary<string, object>();
            
            // Check categories
            var categories = await _context.Categories.ToListAsync();
            result.Add("CategoryCount", categories.Count);
            result.Add("Categories", categories.Select(c => new { c.CategoryId, c.Name }).ToList());
            
            // Check products
            var productCount = await _context.Products.CountAsync();
            result.Add("ProductCount", productCount);
            
            if (productCount > 0)
            {
                var products = await _context.Products.Take(10).ToListAsync();
                result.Add("SampleProducts", products.Select(p => new { 
                    p.ProductId, p.Name, p.CategoryId, p.Price, p.Color 
                }).ToList());
                
                // Check products per category
                var productsByCategory = new Dictionary<string, int>();
                foreach (var cat in categories)
                {
                    var count = await _context.Products.CountAsync(p => p.CategoryId == cat.CategoryId);
                    productsByCategory.Add(cat.Name, count);
                }
                result.Add("ProductsByCategory", productsByCategory);
            }
            
            return Json(result);
        }
    }
}