using ClothingWebApp.Data;
using ClothingWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClothingWebApp.Controllers
{
    /// <summary>
    /// Handles order management (admin functionality)
    /// </summary>
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        
        public OrderController(ApplicationDbContext context)
        {
            _context = context;
        }
        
        /// <summary>
        /// Shows all orders (admin view)
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var orders = await _context.Orders
                .Include(o => o.Customer)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
                
            return View(orders);
        }
        
        /// <summary>
        /// Shows details for a specific order
        /// </summary>
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
            
            return View(order);
        }
        
        /// <summary>
        /// Shows user's order history
        /// </summary>
        public async Task<IActionResult> History()
        {
            // Redirect to Account controller's OrderHistory
            return RedirectToAction("OrderHistory", "Account");
        }
    }
}