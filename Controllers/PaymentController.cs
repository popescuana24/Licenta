using ClothingWebApp.Data;
using ClothingWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClothingWebApp.Controllers
{
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        
        public PaymentController(ApplicationDbContext context)
        {
            _context = context;
        }
        //all payments
        public async Task<IActionResult> Index()
        {
            var payments = await _context.Payments
                .Include(p => p.Order)
                .ThenInclude(o => o!.Customer)  
                .ToListAsync();
                
            return View(payments);
        }
        
        // Shows details for a specific payment
        public async Task<IActionResult> Details(int id)
        {
            var payment = await _context.Payments
                .Include(p => p.Order)
                .ThenInclude(o => o!.Customer)  
                .FirstOrDefaultAsync(p => p.PaymentId == id);
                
            if (payment == null)
            {
                return NotFound();
            }
            
            return View(payment);
        }
    }
}