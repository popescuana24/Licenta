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
            var orders = await _context.Orders
                .Include(o => o.Customer)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
                
            return View(orders);
        }
        
        //Shows details for a specific order
        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.OrderId == id);
                
            if (order == null)
            {
                return NotFound();
            }
            
            // Get payment info for this order
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.OrderId == id);
                
            ViewBag.Payment = payment;
            // Return view with order details
            return View(order);
        }
        
}
}