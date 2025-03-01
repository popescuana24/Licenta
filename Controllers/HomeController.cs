using ClothingWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ClothingWebApp.Controllers
{
    public class HomeController : Controller
    {
        // Temporary product data
        private static List<Product> _products = new List<Product>();
        
        public IActionResult Index()
        {
            
            var featuredProducts = _products.Take(6).ToList();
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
    }
    
    public class ErrorViewModel
{
    public string? RequestId { get; set; } // Make nullable
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
}