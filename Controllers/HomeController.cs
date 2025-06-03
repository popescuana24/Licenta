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
        var categories = await _context.Categories.ToListAsync();
        ViewBag.Categories = categories;
    
        return View();
        }
       
        public IActionResult Privacy()
        {
            return View();
        }

       public IActionResult Contact()
      { 
          return View();
      }
        
        /// Handles errors and returns the error view
        
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}