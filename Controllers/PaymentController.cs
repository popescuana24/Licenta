using ClothingWebApp.Data;
using ClothingWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClothingWebApp.Controllers
{
    /// <summary>
    /// Handles payment management (admin functionality)
    /// </summary>
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        
        public PaymentController(ApplicationDbContext context)
        {
            _context = context;
        }
        
        /// <summary>
        /// Shows all payments (admin view)
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var payments = await _context.Payments
                .Include(p => p.Order)
                .ThenInclude(o => o!.Customer)  // Correct placement of ! before the dot
                .ToListAsync();
                
            return View(payments);
        }
        
        /// <summary>
        /// Shows details for a specific payment
        /// </summary>
        public async Task<IActionResult> Details(int id)
        {
            var payment = await _context.Payments
                .Include(p => p.Order)
                .ThenInclude(o => o!.Customer)  // Correct placement of ! before the dot
                .FirstOrDefaultAsync(p => p.PaymentId == id);
                
            if (payment == null)
            {
                return NotFound();
            }
            
            return View(payment);
        }
    }
}