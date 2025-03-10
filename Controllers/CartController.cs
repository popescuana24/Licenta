using ClothingWebApp.Data;
using ClothingWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
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
            try
            {
                // Clear any size-related error messages when viewing the cart
                if (TempData.ContainsKey("ErrorMessage") && 
                    TempData["ErrorMessage"].ToString().Contains("size"))
                {
                    TempData.Remove("ErrorMessage");
                }
                
                int userId = GetCurrentUserId();
                
                // Try to get the cart with products
                var cart = await _context.Carts
                    .Include(c => c.Products)
                        .ThenInclude(p => p.Category)
                    .Include(c => c.Customer)
                    .FirstOrDefaultAsync(c => c.CustomerId == userId);
                    
                if (cart == null)
                {
                    // Create a new cart
                    var customer = await GetOrCreateCustomerAsync(userId);
                    if (customer != null)
                    {
                        cart = new Cart
                        {
                            CustomerId = customer.CustomerId,
                            Customer = customer,
                            Products = new List<Product>()
                        };
                        
                        _context.Carts.Add(cart);
                        await _context.SaveChangesAsync();
                    }
                }
                
                // Update cart count cookie
                if (cart != null)
                {
                    int cartCount = cart.Products.Count;
                    Response.Cookies.Append("CartCount", cartCount.ToString());
                }
                
                return View(cart);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error retrieving cart: " + ex.Message;
                return RedirectToAction("Index", "Home");
            }
        }

        // POST: Cart/AddToCart
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, string selectedSize = "")
        {
            try
            {
                // Clear any previous messages
                TempData.Remove("ErrorMessage");
                TempData.Remove("SuccessMessage");
                
                int userId = GetCurrentUserId();
                
                // Get the product
                var product = await _context.Products
                    .Include(p => p.Category)
                    .FirstOrDefaultAsync(p => p.ProductId == productId);
                    
                if (product == null)
                {
                    TempData["ErrorMessage"] = "Product not found";
                    return RedirectToAction("Index", "Home");
                }
                
                // For products that require sizes (non-bag items), ensure size is selected
                if (product.Category != null && 
                    !product.Category.Name.ToUpper().Contains("BAG") && 
                    string.IsNullOrEmpty(selectedSize))
                {
                    TempData["ErrorMessage"] = "Please select a size for this product";
                    return RedirectToAction("Details", "Product", new { id = productId });
                }
                
                // If size is provided, update the product's size
                if (!string.IsNullOrEmpty(selectedSize))
                {
                    product.Size = selectedSize;
                }
                else if (product.Category != null && product.Category.Name.ToUpper().Contains("BAG"))
                {
                    // For bags, set default size
                    product.Size = "One Size";
                }
                
                // Get or create the cart
                var cart = await _context.Carts
                    .Include(c => c.Products)
                    .FirstOrDefaultAsync(c => c.CustomerId == userId);
                    
                if (cart == null)
                {
                    // Create a new cart
                    var customer = await GetOrCreateCustomerAsync(userId);
                    if (customer == null)
                    {
                        TempData["ErrorMessage"] = "Error creating user account";
                        return RedirectToAction("Login", "Account");
                    }
                    
                    cart = new Cart
                    {
                        CustomerId = customer.CustomerId,
                        Customer = customer,
                        Products = new List<Product>()
                    };
                    
                    _context.Carts.Add(cart);
                    await _context.SaveChangesAsync();
                }
                
                // Add the product to the cart
                cart.Products.Add(product);
                await _context.SaveChangesAsync();
                
                // Update cart count in cookie
                int cartCount = cart.Products.Count;
                Response.Cookies.Append("CartCount", cartCount.ToString());
                
                TempData["SuccessMessage"] = "Product added to cart!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error adding to cart: " + ex.Message;
                return RedirectToAction("Index", "Home");
            }
        }

        // POST: Cart/UpdateSize
        [HttpPost]
        public async Task<IActionResult> UpdateSize(int productId, string newSize)
        {
            try
            {
                // Clear previous messages
                TempData.Remove("ErrorMessage");
                TempData.Remove("SuccessMessage");
                
                int userId = GetCurrentUserId();
                
                var cart = await _context.Carts
                    .Include(c => c.Products)
                    .FirstOrDefaultAsync(c => c.CustomerId == userId);
                    
                if (cart != null)
                {
                    var product = cart.Products.FirstOrDefault(p => p.ProductId == productId);
                    if (product != null)
                    {
                        // Update the size
                        product.Size = newSize;
                        await _context.SaveChangesAsync();
                        TempData["SuccessMessage"] = "Size updated successfully!";
                    }
                }
                
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error updating size: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Cart/RemoveFromCart
        [HttpPost]
        public async Task<IActionResult> RemoveFromCart(int productId)
        {
            try
            {
                // Clear previous messages
                TempData.Remove("ErrorMessage");
                TempData.Remove("SuccessMessage");
                
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
                        
                        // Update cart count cookie
                        int cartCount = cart.Products.Count;
                        Response.Cookies.Append("CartCount", cartCount.ToString());
                        
                        TempData["SuccessMessage"] = "Product removed from cart";
                    }
                }
                
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error removing product: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }
        
        // GET: Cart/Checkout
        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            // Clear previous messages
            TempData.Remove("ErrorMessage");
            TempData.Remove("SuccessMessage");
            
            int userId = GetCurrentUserId();
            
            // If user is not logged in, redirect to login
            if (!User.Identity.IsAuthenticated)
            {
                TempData["InfoMessage"] = "Please log in or register to complete your purchase.";
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Checkout", "Cart") });
            }
            
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
            // Clear previous messages
            TempData.Remove("ErrorMessage");
            TempData.Remove("SuccessMessage");
            
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
                    OrderDate = DateTime.Now,
                    TotalAmount = total
                };
                
                _context.Orders.Add(order);
                
                // Clear the cart
                cart.Products.Clear();
                
                await _context.SaveChangesAsync();
                
                // Clear cart count cookie
                Response.Cookies.Append("CartCount", "0");
                
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