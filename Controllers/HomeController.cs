using ClothingWebApp.Data;
using ClothingWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace ClothingWebApp.Controllers
{
    /// <summary>
    /// Handles main page and site navigation
    /// </summary>
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        
        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
{
    var categories = await _context.Categories.ToListAsync();
    ViewBag.Categories = categories;
    
    return View();
}
        
        /// <summary>
        /// Shows the homepage with featured products
        /// </summary>
        public async Task<IActionResult> Index2()
        {
            // Get 6 featured products for the homepage
            var featuredProducts = await _context.Products
                .Include(p => p.Category)
                .OrderBy(p => p.ProductId)
                .Take(6)
                .ToListAsync();
                
            return View(featuredProducts);
        }
        
        /// <summary>
        /// Shows the privacy policy page
        /// </summary>
        public IActionResult Privacy()
        {
            return View();
        }
        
        /// <summary>
        /// Handles errors and returns the error view
        /// </summary>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}