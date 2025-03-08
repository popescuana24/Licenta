using ClothingWebApp.Data;
using ClothingWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace ClothingWebApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        
        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }
        
        public async Task<IActionResult> Index()
{
    var featuredProducts = await _context.Products
        .Include(p => p.Category)
        .OrderBy(p => p.ProductId)  // Add an ordering
        .Take(6)
        .ToListAsync();
                
    return View(featuredProducts);
}
        
        public IActionResult Privacy()
        {
            return View();
        }
        
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        // Add to HomeController
public async Task<IActionResult> CheckDatabase()
{
    var data = new Dictionary<string, object>();
    
    // Check categories
    var categories = await _context.Categories.ToListAsync();
    data["Categories"] = categories.Select(c => new { c.CategoryId, c.Name }).ToList();
    data["CategoryCount"] = categories.Count;
    
    // Check all products
    var products = await _context.Products.Take(50).ToListAsync();
    data["Products"] = products.Select(p => new { 
        p.ProductId, 
        p.Name, 
        p.CategoryId, 
        p.Price,
        p.Color
    }).ToList();
    data["ProductCount"] = await _context.Products.CountAsync();
    
    // Check products per category
    var productsByCategory = new Dictionary<string, int>();
    foreach (var category in categories)
    {
        var count = await _context.Products.CountAsync(p => p.CategoryId == category.CategoryId);
        productsByCategory[category.Name] = count;
    }
    data["ProductsByCategory"] = productsByCategory;
    
    return Json(data);
}
    }
}