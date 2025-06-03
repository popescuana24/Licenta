using ClothingWebApp.Data;
using ClothingWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace ClothingWebApp.Controllers
{
    public class CustomerController : Controller
    {
        private readonly ApplicationDbContext _context;
        
        public CustomerController(ApplicationDbContext context)
        {
            _context = context;
        }
        
        // GET all customers i have(if i have an admin pannel but i dont have)
        public async Task<IActionResult> Index()
        {
            var customers = await _context.Customers.ToListAsync();
            return View(customers);
        }
        
    }
}