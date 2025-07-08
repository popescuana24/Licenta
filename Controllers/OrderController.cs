using ClothingWebApp.Data;
using ClothingWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClothingWebApp.Controllers
{
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
    
        
        public OrderController(ApplicationDbContext context)
        {
            _context = context;
        }
        
        //shows all orders
        public async Task<IActionResult> Index()
        {
            //Queries the Orders table, includes related Customer data, orders by OrderDate descending
            var orders = await _context.Orders
                .Include(o => o.Customer)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync(); //execut. query and returns a list asynchronously
                
            return View(orders);
        }
        
        //Shows details for a specific order
        public async Task<IActionResult> Details(int id)
        {
            //searches for an order by its ID, also includes related Customer information
            var order = await _context.Orders
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.OrderId == id); // Gets the first matching order or null
                
            if (order == null)
            {
                return NotFound();  // If no order is found, return a 404 Not Found response
            }
            
            // Get payment info for this order
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.OrderId == id); // Searches Payment table by OrderId
                
            ViewBag.Payment = payment; // Stores payment info in ViewBag for use in the view
            // Return view with order details
            return View(order);
        }
        
}
}
