using ClothingWebApp.Data;
using ClothingWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ClothingWebApp.Controllers 
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const string CartSessionKey = "ShoppingCart";

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Helper method to get current user ID
        private int GetCurrentUserId()
        {
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

        // Session-based method to get cart items while database migration is pending
        // Helper method to get cart items
private async Task<List<CartProduct>> GetCartItemsAsync()
{
    // Try to get from session first
    string sessionJson = HttpContext.Session.GetString(CartSessionKey);
    var cartItems = new List<CartProduct>();
    
    if (!string.IsNullOrEmpty(sessionJson))
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                ReferenceHandler = ReferenceHandler.IgnoreCycles
            };
            
            cartItems = JsonSerializer.Deserialize<List<CartProduct>>(sessionJson, options) ?? new List<CartProduct>();
            return cartItems;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deserializing session cart: {ex.Message}");
        }
    }
    
    // If session failed or is empty, try to get from database
    if (User.Identity.IsAuthenticated)
    {
        try
        {
            int userId = GetCurrentUserId();
            
            var cart = await _context.Carts.FirstOrDefaultAsync(c => c.CustomerId == userId);
            if (cart != null && !string.IsNullOrEmpty(cart.CartItemsJson))
            {
                try
                {
                    var options = new JsonSerializerOptions
                    {
                        ReferenceHandler = ReferenceHandler.IgnoreCycles
                    };
                    
                    cartItems = JsonSerializer.Deserialize<List<CartProduct>>(cart.CartItemsJson, options) ?? new List<CartProduct>();
                    
                    // Update session for future use
                    HttpContext.Session.SetString(CartSessionKey, cart.CartItemsJson);
                    
                    return cartItems;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deserializing DB cart: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting cart from DB: {ex.Message}");
        }
    }
    
    return cartItems;
}

private async Task SaveCartItemsAsync(List<CartProduct> cartItems)
{
    try
    {
        // Create a clean copy without circular references
        var simplifiedItems = cartItems.Select(item => new CartProduct
        {
            ProductId = item.ProductId,
            Size = item.Size ?? string.Empty,
            Quantity = item.Quantity
            // Don't include Product to avoid circular references
        }).ToList();
        
        var options = new JsonSerializerOptions
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles
        };
        
        string json = JsonSerializer.Serialize(simplifiedItems, options);
        
        // Save to session
        HttpContext.Session.SetString(CartSessionKey, json);
        
        // Save to database if user is authenticated
        if (User.Identity.IsAuthenticated)
        {
            try
            {
                int userId = GetCurrentUserId();
                
                // Check if the cart exists in the database
                var cart = await _context.Carts.FirstOrDefaultAsync(c => c.CustomerId == userId);
                
                if (cart == null)
                {
                    // Create a new cart
                    cart = new Cart
                    {
                        CustomerId = userId,
                        CartItemsJson = json
                    };
                    
                    // This is where the error might happen - add detailed logging
                    Console.WriteLine($"Creating new cart for user {userId}");
                    _context.Carts.Add(cart);
                }
                else
                {
                    // Update existing cart
                    Console.WriteLine($"Updating existing cart for user {userId}");
                    cart.CartItemsJson = json;
                    _context.Carts.Update(cart);
                }
                
                // Save changes
                var saveResult = await _context.SaveChangesAsync();
                Console.WriteLine($"SaveChanges result: {saveResult} entities affected");
                
                // Verify the cart was saved
                var verifyCart = await _context.Carts.FirstOrDefaultAsync(c => c.CustomerId == userId);
                if (verifyCart != null)
                {
                    Console.WriteLine($"Cart verification: Found cart with JSON length: {verifyCart.CartItemsJson?.Length ?? 0}");
                }
                else
                {
                    Console.WriteLine("Cart verification failed: Cart not found after save");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database error in SaveCartItemsAsync: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error saving cart: {ex.Message}");
    }
}
        // Method to clear cart
        private async Task ClearCartAsync()
        {
            try
            {
                // Clear from session
                HttpContext.Session.Remove(CartSessionKey);
                
                // If authenticated, also clear from database
                if (User.Identity.IsAuthenticated)
                {
                    try 
                    {
                        int userId = GetCurrentUserId();
                        
                        var cart = await _context.Carts.FirstOrDefaultAsync(c => c.CustomerId == userId);
                        if (cart != null)
                        {
                            _context.Carts.Remove(cart);
                            await _context.SaveChangesAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log but don't fail if the database operation fails
                        Console.WriteLine($"Error removing cart from database: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing cart: {ex.Message}");
            }
        }

       public async Task<IActionResult> Index()
{
    if (!User.Identity.IsAuthenticated)
    {
        TempData["InfoMessage"] = "Please log in or register to view your shopping cart.";
        return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Index", "Cart") });
    }

    try
    {
        int userId = GetCurrentUserId();
        
        // First try to get from session
        string sessionJson = HttpContext.Session.GetString(CartSessionKey);
        List<CartProduct> cartItems = new List<CartProduct>();
        
        if (!string.IsNullOrEmpty(sessionJson))
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    ReferenceHandler = ReferenceHandler.IgnoreCycles
                };
                
                cartItems = JsonSerializer.Deserialize<List<CartProduct>>(sessionJson, options) ?? new List<CartProduct>();
                Console.WriteLine($"Found {cartItems.Count} items in session cart");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deserializing session cart: {ex.Message}");
            }
        }
        
        // If we have no items in session, try database as fallback
        if (!cartItems.Any())
        {
            try 
            {
                var cart = await _context.Carts.AsNoTracking().FirstOrDefaultAsync(c => c.CustomerId == userId);
                if (cart != null && !string.IsNullOrEmpty(cart.CartItemsJson))
                {
                    var options = new JsonSerializerOptions
                    {
                        ReferenceHandler = ReferenceHandler.IgnoreCycles
                    };
                    
                    cartItems = JsonSerializer.Deserialize<List<CartProduct>>(cart.CartItemsJson, options) ?? new List<CartProduct>();
                    Console.WriteLine($"Found {cartItems.Count} items in database cart");
                    
                    // Update session
                    HttpContext.Session.SetString(CartSessionKey, cart.CartItemsJson);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting cart from database: {ex.Message}");
            }
        }
        
        // Load product details for each cart item
        for (int i = 0; i < cartItems.Count; i++)
        {
            var item = cartItems[i];
            try 
            {
                var product = await _context.Products
                    .AsNoTracking()
                    .Include(p => p.Category)
                    .FirstOrDefaultAsync(p => p.ProductId == item.ProductId);
                
                if (product != null)
                {
                    cartItems[i].Product = product;
                    Console.WriteLine($"Loaded product {product.Name} for cart item");
                }
                else
                {
                    Console.WriteLine($"Product with ID {item.ProductId} not found");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading product {item.ProductId}: {ex.Message}");
            }
        }
        
        // Remove items with missing products
        cartItems.RemoveAll(item => item.Product == null);
        
        // Update cart count cookie
        int cartCount = cartItems.Sum(item => item.Quantity);
        Response.Cookies.Append("CartCount", cartCount.ToString());
        
        // Create a cart object for the view
        var cartModel = new Cart
        {
            CustomerId = userId,
            CartItems = cartItems
        };
        
        Console.WriteLine($"Returning cart with {cartItems.Count} items to view");
        return View(cartModel);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error in Cart/Index: {ex.Message}");
        Console.WriteLine(ex.StackTrace);
        TempData["ErrorMessage"] = "Error retrieving cart: " + ex.Message;
        return RedirectToAction("Index", "Home");
    }
}
        // POST: Cart/AddToCart
       [HttpPost]
public async Task<IActionResult> AddToCart(int productId, string selectedSize = "", string returnToProduct = "false")
{
    if (!User.Identity.IsAuthenticated)
    {
        TempData["InfoMessage"] = "Please log in or register to add items to your shopping cart.";
        return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Details", "Product", new { id = productId }) });
    }

    try
    {
        TempData.Remove("ErrorMessage");
        TempData.Remove("SuccessMessage");
        
        // Get the product
        var product = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.ProductId == productId);
            
        if (product == null)
        {
            TempData["ErrorMessage"] = "Product not found";
            return RedirectToAction("Index", "Home");
        }
        
        // For products that require sizes, ensure size is selected
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
        var cartItems = await GetCartItemsAsync();
        
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
        
        // Save updated cart to both session and database
        await SaveCartItemsAsync(cartItems);
        
        // Update cart count cookie
        int cartCount = cartItems.Sum(item => item.Quantity);
        Response.Cookies.Append("CartCount", cartCount.ToString());
        
        TempData["SuccessMessage"] = "Product added to cart!";
        
        // This is the important part - respect the returnToProduct parameter
        if (returnToProduct.ToLower() == "true")
        {
            return RedirectToAction("Details", "Product", new { id = productId });
        }
        else
        {
            // Go directly to cart page
            return RedirectToAction(nameof(Index));
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error in AddToCart: {ex.Message}");
        Console.WriteLine(ex.StackTrace);
        TempData["ErrorMessage"] = "Error adding to cart: " + ex.Message;
        return RedirectToAction("Index", "Home");
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
                
                var cartItems = await GetCartItemsAsync();
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

        // POST: Cart/UpdateSize
[HttpPost]
public async Task<IActionResult> UpdateSize(int productId, string currentSize, string newSize)
{
    if (!User.Identity.IsAuthenticated)
    {
        return RedirectToAction("Login", "Account");
    }

    try
    {
        // Get cart items
        var cartItems = await GetCartItemsAsync();
        
        // Find the item by product ID and size
        var item = cartItems.FirstOrDefault(i => i.ProductId == productId && i.Size == currentSize);
        
        if (item != null)
        {
            // Update the size
            item.Size = newSize;
            
            // Save the updated cart
            await SaveCartItemsAsync(cartItems);
            
            TempData["SuccessMessage"] = "Size updated successfully!";
        }
        else
        {
            TempData["ErrorMessage"] = "Product not found in cart.";
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
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                TempData.Remove("ErrorMessage");
                TempData.Remove("SuccessMessage");
                
                var cartItems = await GetCartItemsAsync();
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
        var cartItems = await GetCartItemsAsync();
        
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
                // Create order
                var order = new Order
                {
                    CustomerId = userId,
                    OrderDate = DateTime.Now,
                    TotalAmount = totalAmount
                };
                
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();
                
                // Create payment
                var payment = new Payment
                {
                    OrderId = order.OrderId,
                    PaymentMethod = paymentMethod,
                    IsPaid = paymentMethod == "Credit Card"
                };
                
                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();
                
                // Clear cart from session
                await ClearCartAsync();
                Response.Cookies.Append("CartCount", "0");
                
                await transaction.CommitAsync();
                
                return RedirectToAction("OrderConfirmation", new { orderId = order.OrderId });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"Transaction error in PlaceOrder: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error in PlaceOrder: {ex.Message}");
        Console.WriteLine(ex.StackTrace);
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

// GET: Cart/Checkout
public async Task<IActionResult> Checkout()
{
    if (!User.Identity.IsAuthenticated)
    {
        TempData["InfoMessage"] = "Please log in or register to complete your purchase.";
        return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Checkout", "Cart") });
    }

    try
    {
        int userId = GetCurrentUserId();
        
        // Get cart items from session or database
        var cartItems = await GetCartItemsAsync();
        
        // Check if cart is empty
        if (!cartItems.Any())
        {
            TempData["ErrorMessage"] = "Your cart is empty. Please add products before checkout.";
            return RedirectToAction(nameof(Index));
        }
        
        // Load product details for each cart item
        foreach (var item in cartItems.ToList())
        {
            try
            {
                var product = await _context.Products
                    .AsNoTracking()
                    .Include(p => p.Category)
                    .FirstOrDefaultAsync(p => p.ProductId == item.ProductId);
                    
                if (product != null)
                {
                    item.Product = product;
                }
                else
                {
                    // Remove items with missing products
                    cartItems.Remove(item);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading product {item.ProductId}: {ex.Message}");
            }
        }
        
        // If all products were removed (because they don't exist anymore)
        if (!cartItems.Any())
        {
            TempData["ErrorMessage"] = "All products in your cart are no longer available.";
            return RedirectToAction(nameof(Index));
        }
        
        // Get customer details
        var customer = await _context.Customers.FindAsync(userId);
        
        // Create a cart model for the view
        var cartModel = new Cart
        {
            CustomerId = userId,
            Customer = customer,
            CartItems = cartItems
        };
        
        return View(cartModel);
    }
    catch (Exception ex)
    {
        TempData["ErrorMessage"] = "Error processing checkout: " + ex.Message;
        return RedirectToAction(nameof(Index));
    }
}
        public IActionResult DebugCart()
{
    if (!User.Identity.IsAuthenticated)
    {
        return Content("Not logged in");
    }
    
    try
    {
        int userId = GetCurrentUserId();
        var result = new System.Text.StringBuilder();
        result.AppendLine($"User ID: {userId}");
        
        // Check session
        string sessionJson = HttpContext.Session.GetString(CartSessionKey);
        result.AppendLine($"Session has cart data: {!string.IsNullOrEmpty(sessionJson)}");
        
        if (!string.IsNullOrEmpty(sessionJson))
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    ReferenceHandler = ReferenceHandler.IgnoreCycles
                };
                
                var sessionItems = JsonSerializer.Deserialize<List<CartProduct>>(sessionJson, options);
                result.AppendLine($"Session cart items: {sessionItems?.Count ?? 0}");
                
                if (sessionItems != null && sessionItems.Any())
                {
                    foreach (var item in sessionItems)
                    {
                        result.AppendLine($"- Product ID: {item.ProductId}, Size: {item.Size}, Quantity: {item.Quantity}");
                    }
                }
            }
            catch (Exception ex)
            {
                result.AppendLine($"Error deserializing session cart: {ex.Message}");
            }
        }
        
        // Check database
        try
        {
            var cart = _context.Carts.FirstOrDefault(c => c.CustomerId == userId);
            result.AppendLine($"Database has cart: {cart != null}");
            
            if (cart != null)
            {
                result.AppendLine($"Database cart JSON: {!string.IsNullOrEmpty(cart.CartItemsJson)}");
                
                if (!string.IsNullOrEmpty(cart.CartItemsJson))
                {
                    try
                    {
                        var options = new JsonSerializerOptions
                        {
                            ReferenceHandler = ReferenceHandler.IgnoreCycles
                        };
                        
                        var dbItems = JsonSerializer.Deserialize<List<CartProduct>>(cart.CartItemsJson, options);
                        result.AppendLine($"Database cart items: {dbItems?.Count ?? 0}");
                        
                        if (dbItems != null && dbItems.Any())
                        {
                            foreach (var item in dbItems)
                            {
                                result.AppendLine($"- Product ID: {item.ProductId}, Size: {item.Size}, Quantity: {item.Quantity}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        result.AppendLine($"Error deserializing database cart: {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            result.AppendLine($"Error accessing database cart: {ex.Message}");
        }
        
        return Content(result.ToString(), "text/plain");
    }
    catch (Exception ex)
    {
        return Content($"Error: {ex.Message}");
    }
}
public IActionResult ViewCart()
{
    var result = new System.Text.StringBuilder();
    result.AppendLine("<h3>Cart Debug Info</h3>");
    
    try
    {
        // Check session
        string sessionJson = HttpContext.Session.GetString(CartSessionKey);
        if (string.IsNullOrEmpty(sessionJson))
        {
            result.AppendLine("<p>No cart in session</p>");
        }
        else
        {
            result.AppendLine("<p>Cart found in session</p>");
            
            try
            {
                var options = new JsonSerializerOptions
                {
                    ReferenceHandler = ReferenceHandler.IgnoreCycles
                };
                
                var cartItems = JsonSerializer.Deserialize<List<CartProduct>>(sessionJson, options);
                if (cartItems == null || !cartItems.Any())
                {
                    result.AppendLine("<p>Cart is empty or could not be deserialized</p>");
                }
                else
                {
                    result.AppendLine($"<p>Found {cartItems.Count} items in cart:</p>");
                    result.AppendLine("<ul>");
                    
                    foreach (var item in cartItems)
                    {
                        result.AppendLine($"<li>Product ID: {item.ProductId}, Size: {item.Size}, Quantity: {item.Quantity}</li>");
                    }
                    
                    result.AppendLine("</ul>");
                }
            }
            catch (Exception ex)
            {
                result.AppendLine($"<p>Error deserializing cart: {ex.Message}</p>");
            }
        }
        
        return Content(result.ToString(), "text/html");
    }
    catch (Exception ex)
    {
        return Content($"<p>Error: {ex.Message}</p>", "text/html");
    }
}
    }
}