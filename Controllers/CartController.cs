using ClothingWebApp.Data;
using ClothingWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClothingWebApp.Controllers 
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Cart
        public async Task<IActionResult> Index()
        {
            int userId = GetCurrentUserId();
            
            var cart = await _context.Carts
                .Include(c => c.Products)
                .Include(c => c.Customer)
                .FirstOrDefaultAsync(c => c.CustomerId == userId);
                
            if (cart == null)
            {
                // Create a new cart for this user
                var customer = await GetOrCreateCustomerAsync(userId);
                if (customer != null)
                {
                    cart = new Cart
                    {
                        CartId = customer.CustomerId,
                        CustomerId = customer.CustomerId,
                        Customer = customer,
                        Products = new List<Product>()
                    };
                    
                    _context.Carts.Add(cart);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    // Handle case where customer creation failed
                    return RedirectToAction("Login", "Account");
                }
            }
            
            return View(cart);
        }

        // POST: Cart/AddToCart
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId)
        {
            int userId = GetCurrentUserId();
            
            // Get the cart for this user
            var cart = await _context.Carts
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.CustomerId == userId);
                
            if (cart == null)
            {
                // Create a new cart
                var customer = await GetOrCreateCustomerAsync(userId);
                if (customer != null)
                {
                    cart = new Cart
                    {
                        CartId = customer.CustomerId,
                        CustomerId = customer.CustomerId,
                        Customer = customer,
                        Products = new List<Product>()
                    };
                    
                    _context.Carts.Add(cart);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    // Handle case where customer creation failed
                    return RedirectToAction("Login", "Account");
                }
            }
            
            // Get the product
            var product = await _context.Products.FindAsync(productId);
            if (product != null)
            {
                cart.Products.Add(product);
                await _context.SaveChangesAsync();
            }
            
            return RedirectToAction(nameof(Index));
        }

        // POST: Cart/RemoveFromCart
        [HttpPost]
        public async Task<IActionResult> RemoveFromCart(int productId)
        {
            int userId = GetCurrentUserId();
            
            var cart = await _context.Carts
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.CustomerId == userId);
                
            if (cart != null)
            {
                var product = cart.Products.FirstOrDefault(p => p.ProductId == productId);
                if (product != null)
                {
                    cart.Products.Remove(product);
                    await _context.SaveChangesAsync();
                }
            }
            
            return RedirectToAction(nameof(Index));
        }
        
        // GET: Cart/Checkout
        public async Task<IActionResult> Checkout()
        {
            int userId = GetCurrentUserId();
            
            var cart = await _context.Carts
                .Include(c => c.Products)
                .Include(c => c.Customer)
                .FirstOrDefaultAsync(c => c.CustomerId == userId);
                
            if (cart != null && cart.Products.Any())
            {
                return View(cart);
            }
            
            return RedirectToAction(nameof(Index));
        }
        
        // POST: Cart/PlaceOrder
        [HttpPost]
        public async Task<IActionResult> PlaceOrder()
        {
            int userId = GetCurrentUserId();
            
            var cart = await _context.Carts
                .Include(c => c.Products)
                .Include(c => c.Customer)
                .FirstOrDefaultAsync(c => c.CustomerId == userId);
                
            if (cart != null && cart.Products.Any())
            {
                // Calculate total amount
                decimal total = cart.Products.Sum(p => p.Price);
                
                // Get the next available OrderId
                int nextOrderId = 1;
                if (await _context.Orders.AnyAsync())
                {
                    nextOrderId = await _context.Orders.MaxAsync(o => o.OrderId) + 1;
                }
                
                // Create an order with the OrderId set explicitly
                var order = new Order
                {
                    OrderId = nextOrderId,
                    CustomerId = userId,
                    Customer = cart.Customer,
                    OrderDate = System.DateTime.Now,
                    TotalAmount = total
                };
                
                _context.Orders.Add(order);
                
                // Clear the cart
                cart.Products.Clear();
                
                await _context.SaveChangesAsync();
                
                return RedirectToAction("OrderConfirmation", new { orderId = order.OrderId });
            }
            
            return RedirectToAction(nameof(Index));
        }
        
        // GET: Cart/OrderConfirmation
        public IActionResult OrderConfirmation(int orderId)
        {
            ViewBag.OrderId = orderId;
            return View();
        }
        
        // Helper methods
        private int GetCurrentUserId()
        {
            // If user is authenticated, get the user ID from claims
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var userIdClaim = User.FindFirst("CustomerId");
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int id))
                {
                    return id;
                }
            }
            
            // For guest users or if authentication fails, use a default ID
            return 999; // Guest user ID
        }
        
        // Helper method to get or create a customer
        private async Task<Customer?> GetOrCreateCustomerAsync(int userId)
        {
            var customer = await _context.Customers.FindAsync(userId);
            
            if (customer != null)
            {
                return customer;
            }
            
            if (userId == 999) // Guest user
            {
                try
                {
                    // Create a new customer with auto-generated ID
                    customer = new Customer
                    {
                        // Don't set CustomerId - let the database generate it
                        FirstName = "Guest",
                        LastName = "User",
                        Email = $"guest{DateTime.Now.Ticks}@example.com", // Make it unique
                        Address = "Guest Address",
                        Password = "guest" + Guid.NewGuid().ToString().Substring(0, 8) // Random password
                    };
                    
                    _context.Customers.Add(customer);
                    await _context.SaveChangesAsync();
                    
                    // Get the customer with the generated ID
                    return customer;
                }
                catch (Exception ex)
                {
                    // Log the error
                    Console.WriteLine($"Error creating guest customer: {ex.Message}");
                    return null;
                }
            }
            
            return null;
        }
    }
}