using ClothingWebApp.Data;
using ClothingWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ClothingWebApp.Controllers 
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const string CartSessionKey = "ShoppingCart";
        private const string CartCookiePrefix = "UserCart_";

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Cart
        public async Task<IActionResult> Index()
        {
            // Redirect to login if not authenticated
            if (!User.Identity.IsAuthenticated)
            {
                TempData["InfoMessage"] = "Please log in or register to view your shopping cart.";
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Index", "Cart") });
            }

            try
            {
                int userId = GetCurrentUserId();
                
                // Get cart items from session or cookie
                var cartItems = GetCartItemsAsync().Result;
                
                // Create a safe copy to iterate
                var itemsToRemove = new List<CartProduct>();
                
                // Load product details for each cart item
                foreach (var item in cartItems)
                {
                    item.Product = await _context.Products
                        .AsNoTracking() // Don't track the entity to avoid circular references
                        .Include(p => p.Category)
                        .FirstOrDefaultAsync(p => p.ProductId == item.ProductId);
                        
                    // Handle case where product might not exist anymore
                    if (item.Product == null)
                    {
                        // Mark for removal
                        itemsToRemove.Add(item);
                    }
                }
                
                // Remove any items with missing products
                foreach (var item in itemsToRemove)
                {
                    cartItems.Remove(item);
                }
                
                // Save updated cart (in case items were removed)
                await SaveCartItemsAsync(cartItems);
                
                // Create a cart object
                var cart = new Cart
                {
                    CustomerId = userId,
                    CartItems = cartItems
                };
                
                // Update cart count cookie
                int cartCount = cartItems.Sum(item => item.Quantity);
                Response.Cookies.Append("CartCount", cartCount.ToString());
                
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
        public async Task<IActionResult> AddToCart(int productId, string selectedSize = "", string returnToProduct = "false")
        {
            // Redirect to login if not authenticated
            if (!User.Identity.IsAuthenticated)
            {
                TempData["InfoMessage"] = "Please log in or register to add items to your shopping cart.";
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Details", "Product", new { id = productId }) });
            }

            try
            {
                // Clear any previous messages
                TempData.Remove("ErrorMessage");
                TempData.Remove("SuccessMessage");
                
                // Get the product
                var product = await _context.Products
                    .AsNoTracking() // Don't track the entity
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
                
                // Set size for bag products
                if (string.IsNullOrEmpty(selectedSize) && product.Category != null && 
                    product.Category.Name.ToUpper().Contains("BAG"))
                {
                    selectedSize = "One Size";
                }

                // Get cart items
                var cartItems = GetCartItemsAsync().Result;
                
                // Check if this product+size combination already exists in the cart
                var existingItem = cartItems.FirstOrDefault(
                    item => item.ProductId == productId && item.Size == selectedSize);
                
                if (existingItem != null)
                {
                    // Increment quantity
                    existingItem.Quantity++;
                }
                else
                {
                    // Add new item
                    cartItems.Add(new CartProduct
                    {
                        ProductId = productId,
                        Size = selectedSize,
                        Quantity = 1
                    });
                }
                
                // Save updated cart
                await SaveCartItemsAsync(cartItems);
                
                // Update cart count cookie
                int cartCount = cartItems.Sum(item => item.Quantity);
                Response.Cookies.Append("CartCount", cartCount.ToString());
                
                TempData["SuccessMessage"] = "Product added to cart!";
                
                // Return to the product page or go to cart based on parameter
                if (returnToProduct == "true")
                {
                    return RedirectToAction("Details", "Product", new { id = productId });
                }
                else
                {
                    return RedirectToAction(nameof(Index));
                }
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
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                // Clear previous messages
                TempData.Remove("ErrorMessage");
                TempData.Remove("SuccessMessage");
                
                var cartItems = GetCartItemsAsync().Result;
                var item = cartItems.FirstOrDefault(item => item.ProductId == productId);
                
                if (item != null)
                {
                    item.Size = newSize;
                    await SaveCartItemsAsync(cartItems);
                    TempData["SuccessMessage"] = "Size updated successfully!";
                }
                
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error updating size: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Cart/UpdateQuantity
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int productId, int quantity)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                if (quantity <= 0)
                {
                    return await RemoveFromCart(productId);
                }
                
                var cartItems = GetCartItemsAsync().Result;
                var item = cartItems.FirstOrDefault(item => item.ProductId == productId);
                
                if (item != null)
                {
                    item.Quantity = quantity;
                    await SaveCartItemsAsync(cartItems);
                    
                    // Update cart count cookie
                    int cartCount = cartItems.Sum(item => item.Quantity);
                    Response.Cookies.Append("CartCount", cartCount.ToString());
                    
                    TempData["SuccessMessage"] = "Quantity updated!";
                }
                
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error updating quantity: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Cart/RemoveFromCart
        [HttpPost]
        public async Task<IActionResult> RemoveFromCart(int productId)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                // Clear previous messages
                TempData.Remove("ErrorMessage");
                TempData.Remove("SuccessMessage");
                
                var cartItems = GetCartItemsAsync().Result;
                var itemToRemove = cartItems.FirstOrDefault(item => item.ProductId == productId);
                
                if (itemToRemove != null)
                {
                    cartItems.Remove(itemToRemove);
                    await SaveCartItemsAsync(cartItems);
                    
                    // Update cart count cookie
                    int cartCount = cartItems.Sum(item => item.Quantity);
                    Response.Cookies.Append("CartCount", cartCount.ToString());
                    
                    TempData["SuccessMessage"] = "Product removed from cart";
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
            // Redirect to login if not authenticated
            if (!User.Identity.IsAuthenticated)
            {
                TempData["InfoMessage"] = "Please log in or register to complete your purchase.";
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Checkout", "Cart") });
            }
            
            try
            {
                var cartItems = GetCartItemsAsync().Result;
                
                if (!cartItems.Any())
                {
                    TempData["ErrorMessage"] = "Your cart is empty. Please add some products before checkout.";
                    return RedirectToAction(nameof(Index));
                }
                
                // Create a safe copy to iterate
                var itemsToRemove = new List<CartProduct>();
                
                // Load product details for each cart item
                foreach (var item in cartItems)
                {
                    item.Product = await _context.Products
                        .AsNoTracking()
                        .Include(p => p.Category)
                        .FirstOrDefaultAsync(p => p.ProductId == item.ProductId);
                        
                    if (item.Product == null)
                    {
                        // Mark for removal
                        itemsToRemove.Add(item);
                    }
                }
                
                // Remove any items with missing products
                foreach (var item in itemsToRemove)
                {
                    cartItems.Remove(item);
                }
                
                if (!cartItems.Any())
                {
                    TempData["ErrorMessage"] = "All products in your cart are no longer available.";
                    return RedirectToAction(nameof(Index));
                }
                
                int userId = GetCurrentUserId();
                var customer = await _context.Customers.FindAsync(userId);
                
                // Create a cart object
                var cart = new Cart
                {
                    CustomerId = userId,
                    Customer = customer,
                    CartItems = cartItems
                };
                
                // Save updated cart in case items were removed
                await SaveCartItemsAsync(cartItems);
                
                return View(cart);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error processing checkout: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }
        
      [HttpPost]
public async Task<IActionResult> PlaceOrder(string paymentMethod, string cardNumber = "", string cardName = "", string expiryDate = "", string cvv = "")
{
    if (!User.Identity.IsAuthenticated)
    {
        return RedirectToAction("Login", "Account");
    }

    try
    {
        int userId = GetCurrentUserId();
        var cartItems = GetCartItemsAsync().Result;
        
        if (!cartItems.Any())
        {
            TempData["ErrorMessage"] = "Your cart is empty. Please add some products before placing an order.";
            return RedirectToAction(nameof(Index));
        }
        
        // Load products
        var validCartItems = new List<CartProduct>();
        foreach (var item in cartItems)
        {
            var product = await _context.Products.FindAsync(item.ProductId);
            if (product != null)
            {
                item.Product = product;
                validCartItems.Add(item);
            }
        }
        
        if (!validCartItems.Any())
        {
            TempData["ErrorMessage"] = "All products in your cart are no longer available.";
            return RedirectToAction(nameof(Index));
        }
        
        // Get customer
        var customer = await _context.Customers.FindAsync(userId);
        
        if (customer == null)
        {
            TempData["ErrorMessage"] = "Customer account not found.";
            return RedirectToAction("Login", "Account");
        }
        
        // Calculate total amount
        decimal totalAmount = validCartItems.Sum(item => item.Product.Price * item.Quantity);
        
        // Begin transaction
        using (var transaction = await _context.Database.BeginTransactionAsync())
        {
            try
            {
                // Create order - IMPORTANT: Do NOT set OrderId explicitly
                var order = new Order
                {
                    CustomerId = userId,
                    Customer = customer,
                    OrderDate = DateTime.Now,
                    TotalAmount = totalAmount
                };
                
                _context.Orders.Add(order);
                await _context.SaveChangesAsync(); // Save to get the auto-generated OrderId
                
                // Create payment (don't set PaymentId)
                var payment = new Payment
                {
                    OrderId = order.OrderId,
                    PaymentMethod = paymentMethod,
                    IsPaid = paymentMethod == "Credit Card" // Only mark as paid for credit card payments
                };
                
                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();
                
                // Commit transaction
                await transaction.CommitAsync();
                
                // Clear cart
                await ClearCartAsync();
                
                // Clear cart count cookie
                Response.Cookies.Append("CartCount", "0");
                
                return RedirectToAction("OrderConfirmation", new { orderId = order.OrderId });
            }
            catch (Exception ex)
            {
                // Rollback transaction on error
                await transaction.RollbackAsync();
                throw new Exception($"Database error: {ex.Message}");
            }
        }
    }
    catch (Exception ex)
    {
        TempData["ErrorMessage"] = "Error placing order: " + ex.Message;
        return RedirectToAction(nameof(Index));
    }
}
        
        // GET: Cart/OrderConfirmation
        public async Task<IActionResult> OrderConfirmation(int orderId)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var order = await _context.Orders
                    .Include(o => o.Customer)
                    .FirstOrDefaultAsync(o => o.OrderId == orderId);
                    
                if (order == null)
                {
                    TempData["ErrorMessage"] = "Order not found.";
                    return RedirectToAction("Index", "Home");
                }
                
                var payment = await _context.Payments.FirstOrDefaultAsync(p => p.OrderId == orderId);
                
                ViewBag.Order = order;
                ViewBag.Payment = payment;
                
                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error retrieving order confirmation: " + ex.Message;
                return RedirectToAction("Index", "Home");
            }
        }
        
        // Enhanced helper methods for cart management - fixed to handle circular references
        private Task<List<CartProduct>> GetCartItemsAsync()
        {
            // Try to get from session first
            string json = HttpContext.Session.GetString(CartSessionKey);
            
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var options = new JsonSerializerOptions
                    {
                        ReferenceHandler = ReferenceHandler.IgnoreCycles
                    };
                    
                    var result = JsonSerializer.Deserialize<List<CartProduct>>(json, options);
                    if (result != null)
                    {
                        return Task.FromResult(result);
                    }
                }
                catch (Exception ex)
                {
                    // Log the error but continue - session data might be corrupted
                    Console.WriteLine($"Error deserializing session cart: {ex.Message}");
                }
            }
            
            // If not in session but user is authenticated, try to get from cookie
            if (User.Identity.IsAuthenticated)
            {
                try
                {
                    int userId = GetCurrentUserId();
                    string cookieKey = $"{CartCookiePrefix}{userId}";
                    string cookieValue = Request.Cookies[cookieKey];
                    
                    if (!string.IsNullOrEmpty(cookieValue))
                    {
                        var options = new JsonSerializerOptions
                        {
                            ReferenceHandler = ReferenceHandler.IgnoreCycles
                        };
                        
                        var cartItems = JsonSerializer.Deserialize<List<CartProduct>>(cookieValue, options);
                        if (cartItems != null)
                        {
                            // Store in session for faster access
                            HttpContext.Session.SetString(CartSessionKey, cookieValue);
                            
                            return Task.FromResult(cartItems);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log the error but continue - cookie data might be corrupted
                    Console.WriteLine($"Error deserializing cookie cart: {ex.Message}");
                }
            }
            
            // Return empty cart if nothing found
            return Task.FromResult(new List<CartProduct>());
        }
        
        private Task SaveCartItemsAsync(List<CartProduct> cartItems)
{
    try
    {
        // Create a clean copy without circular references
        var simplifiedItems = cartItems.Select(item => new CartProduct
        {
            ProductId = item.ProductId,
            Size = item.Size ?? string.Empty,
            Quantity = item.Quantity,
            // Include all required properties for Product
            Product = item.Product != null ? new Product
            {
                ProductId = item.Product.ProductId,
                Name = item.Product.Name ?? string.Empty,
                Description = item.Product.Description ?? string.Empty,
                Price = item.Product.Price,
                Color = item.Product.Color ?? string.Empty,
                ImageUrl = item.Product.ImageUrl ?? string.Empty,
                Size = item.Product.Size ?? string.Empty,
                CategoryId = item.Product.CategoryId
                // Don't include Category to avoid circular references
            } : null
        }).ToList();
        
        var options = new JsonSerializerOptions
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles
        };
        
        string json = JsonSerializer.Serialize(simplifiedItems, options);
        
        // Save to session
        HttpContext.Session.SetString(CartSessionKey, json);
        
        // If authenticated, also save to cookie for persistence between sessions
        if (User.Identity.IsAuthenticated)
        {
            int userId = GetCurrentUserId();
            string cookieKey = $"{CartCookiePrefix}{userId}";
            
            var cookieOptions = new CookieOptions
            {
                Expires = DateTime.Now.AddDays(30),
                HttpOnly = true,
                IsEssential = true
            };
            
            Response.Cookies.Append(cookieKey, json, cookieOptions);
        }
    }
    catch (Exception ex)
    {
        // Log the error but don't throw - this is optional functionality
        Console.WriteLine($"Error saving cart: {ex.Message}");
    }
    
    return Task.CompletedTask;
}
        private Task ClearCartAsync()
        {
            try
            {
                // Clear from session
                HttpContext.Session.Remove(CartSessionKey);
                
                // If authenticated, also clear from cookie
                if (User.Identity.IsAuthenticated)
                {
                    int userId = GetCurrentUserId();
                    string cookieKey = $"{CartCookiePrefix}{userId}";
                    
                    Response.Cookies.Delete(cookieKey);
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't throw - this is optional functionality
                Console.WriteLine($"Error clearing cart: {ex.Message}");
            }
            
            return Task.CompletedTask;
        }
        
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
            
            throw new InvalidOperationException("User is not authenticated or user ID not available");
        }
    }
}