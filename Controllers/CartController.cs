using ClothingWebApp.Data;
using ClothingWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClothingWebApp.Controllers
{
    /// <summary>
    /// Handles shopping cart functionality
    /// </summary>
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const string CartSessionKey = "ShoppingCart";

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Helper method to get current user ID
        /// </summary>
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

        /// <summary>
        /// Gets cart items from session or database
        /// </summary>
        private async Task<List<CartProduct>> GetCartItemsAsync()
        {
            // Try to get from session first
            
            string sessionJson = HttpContext.Session.GetString(CartSessionKey) ?? string.Empty;
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
            if (User.Identity != null && User.Identity.IsAuthenticated)
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

        /// <summary>
        /// Saves cart items to session and database
        /// </summary>
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
                }).ToList();
                
                var options = new JsonSerializerOptions
                {
                    ReferenceHandler = ReferenceHandler.IgnoreCycles
                };
                
                string json = JsonSerializer.Serialize(simplifiedItems, options);
                
                // Save to session
                HttpContext.Session.SetString(CartSessionKey, json);
                
                // Save to database if user is authenticated
                if (User.Identity != null && User.Identity.IsAuthenticated)
                {
                    try
                    {
                        int userId = GetCurrentUserId();
                        
                        // Get or create cart
                        var cart = await _context.Carts.FirstOrDefaultAsync(c => c.CustomerId == userId);
                        if (cart == null)
                        {
                            cart = new Cart
                            {
                                CustomerId = userId,
                                CartItemsJson = json
                            };
                            _context.Carts.Add(cart);
                        }
                        else
                        {
                            cart.CartItemsJson = json;
                            _context.Update(cart);
                        }
                        
                        await _context.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error saving cart to database: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving cart: {ex.Message}");
            }
        }

        /// <summary>
        /// Clears the cart from session and database
        /// </summary>
        private async Task ClearCartAsync()
        {
            try
            {
                // Clear from session
                HttpContext.Session.Remove(CartSessionKey);

                // If authenticated, also clear from database
                if (User.Identity != null && User.Identity.IsAuthenticated)
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
                        Console.WriteLine($"Error removing cart from database: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing cart: {ex.Message}");
            }
        }

        /// <summary>
        /// Shows the shopping cart
        /// </summary>
        public async Task<IActionResult> Index()
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                TempData["InfoMessage"] = "Please log in or register to view your shopping cart.";
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Index", "Cart") });
            }

            try
            {
                int userId = GetCurrentUserId();
                
                // Get cart items
                var cartItems = await GetCartItemsAsync();
                
                // Load product details for each cart item
                foreach (var item in cartItems.ToList())
                {
                    var product = await _context.Products
                        .AsNoTracking()
                        .Include(p => p.Category)
                        .FirstOrDefaultAsync(p => p.ProductId == item.ProductId);
                    
                    if (product == null)
                    {
                        cartItems.Remove(item);
                    }
                    else
                    {
                        item.Product = product;
                    }
                }
                
                // Update cart count cookie
                int cartCount = cartItems.Sum(item => item.Quantity);
                Response.Cookies.Append("CartCount", cartCount.ToString());
                
                // Create cart model for view
                var cartModel = new Cart
                {
                    CustomerId = userId,
                    CartItems = cartItems
                };
                
                return View(cartModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error retrieving cart: " + ex.Message;
                return RedirectToAction("Index", "Home");
            }
        }

        /// <summary>
        /// Adds a product to the cart
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, string selectedSize = "", string returnToProduct = "false")
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
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
                
                if (returnToProduct.ToLower() == "true")
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

        /// <summary>
        /// Updates quantity of a cart item
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int productId, int quantity)
        {
            if (User?.Identity == null || !User.Identity.IsAuthenticated)
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

        /// <summary>
        /// Updates size of a cart item
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UpdateSize(int productId, string currentSize, string newSize)
        {
            if (User?.Identity == null || !User.Identity.IsAuthenticated)
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

        /// <summary>
        /// Removes a product from the cart
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> RemoveFromCart(int productId)
        {
            if (User?.Identity == null || !User.Identity.IsAuthenticated)
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

        /// <summary>
        /// Shows the checkout page
        /// </summary>
        public async Task<IActionResult> Checkout()
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                TempData["InfoMessage"] = "Please log in or register to complete your purchase.";
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Checkout", "Cart") });
            }

            try
            {
                int userId = GetCurrentUserId();
                
                // Get cart items
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
                        cartItems.Remove(item);
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

        /// <summary>
        /// Places an order
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> PlaceOrder(string paymentMethod, string cardNumber = "", string cardName = "", string expiryDate = "", string cvv = "")
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
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
                decimal totalAmount = validCartItems
                    .Where(item => item.Product != null)
                    .Sum(item => (item.Product?.Price ?? 0) * item.Quantity);
                
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
                        throw new Exception($"Database error: {ex.Message}", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error placing order: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Shows order confirmation after successful order placement
        /// </summary>
        public async Task<IActionResult> OrderConfirmation(int orderId)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
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
    }
}