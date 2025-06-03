using ClothingWebApp.Data;
using ClothingWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ClothingWebApp.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = User.FindFirst("CustomerId");
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int id))
                {
                    return id;
                }
            }
            
            throw new InvalidOperationException("User is not authenticated");
        }

        private async Task<Cart> GetOrCreateUserCart()
        {
            int userId = GetCurrentUserId();
            
            var cart = await _context.Carts
                .FirstOrDefaultAsync(c => c.CustomerId == userId);

            if (cart == null)
            {
                cart = new Cart
                {
                    CustomerId = userId,
                    CartItemsJson = "[]"
                };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            // Deserialize cart items from JSON
            try
            {
                cart.CartItems = JsonSerializer.Deserialize<List<CartProduct>>(cart.CartItemsJson) 
                    ?? new List<CartProduct>();
            }
            catch
            {
                cart.CartItems = new List<CartProduct>();
            }

            return cart;
        }

        private async Task SaveCartToDatabase(Cart cart)
        {
            cart.CartItemsJson = JsonSerializer.Serialize(cart.CartItems);
            _context.Carts.Update(cart);
            await _context.SaveChangesAsync();

            // Update cart count cookie
            int totalItems = cart.CartItems.Sum(item => item.Quantity);
            Response.Cookies.Append("CartCount", totalItems.ToString());
        }

        private async Task LoadProductDetails(Cart cart)
        {
            foreach (var item in cart.CartItems.ToList())
            {
                var product = await _context.Products
                    .Include(p => p.Category)
                    .FirstOrDefaultAsync(p => p.ProductId == item.ProductId);
                
                if (product != null)
                {
                    item.Product = product;
                }
                else
                {
                    // Remove items for products that no longer exist
                    cart.CartItems.Remove(item);
                }
            }
        }

        // ================ MAIN ACTIONS ================

        public async Task<IActionResult> Index()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                TempData["InfoMessage"] = "Please log in to view your cart.";
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var cart = await GetOrCreateUserCart();
                await LoadProductDetails(cart);
                await SaveCartToDatabase(cart);
                
                return View(cart);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error loading cart: " + ex.Message;
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, string selectedSize = "", string returnToProduct = "false")
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                TempData["InfoMessage"] = "Please log in to add items to cart.";
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var product = await _context.Products
                    .Include(p => p.Category)
                    .FirstOrDefaultAsync(p => p.ProductId == productId);
                    
                if (product == null)
                {
                    TempData["ErrorMessage"] = "Product not found";
                    return RedirectToAction("Index", "Home");
                }
                
                // Size validation
                if (RequiresSize(product) && string.IsNullOrEmpty(selectedSize))
                {
                    TempData["ErrorMessage"] = "Please select a size";
                    return RedirectToAction("Details", "Product", new { id = productId });
                }

                // Default size for bags
                if (string.IsNullOrEmpty(selectedSize) && IsBag(product))
                {
                    selectedSize = "One Size";
                }

                var cart = await GetOrCreateUserCart();
                
                // Check if item already exists
                var existingItem = cart.CartItems.FirstOrDefault(item => 
                    item.ProductId == productId && item.Size == selectedSize);
                
                if (existingItem != null)
                {
                    existingItem.Quantity++;
                }
                else
                {
                    cart.CartItems.Add(new CartProduct
                    {
                        ProductId = productId,
                        Size = selectedSize,
                        Quantity = 1
                    });
                }
                
                await SaveCartToDatabase(cart);
                TempData["SuccessMessage"] = "Added to cart!";
                
                return returnToProduct.ToLower() == "true" 
                    ? RedirectToAction("Details", "Product", new { id = productId })
                    : RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error adding to cart: " + ex.Message;
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int productId, int quantity)
        {
            if (User.Identity?.IsAuthenticated != true)
                return RedirectToAction("Login", "Account");

            try
            {
                var cart = await GetOrCreateUserCart();
                var item = cart.CartItems.FirstOrDefault(item => item.ProductId == productId);
                
                if (item != null)
                {
                    if (quantity <= 0)
                    {
                        cart.CartItems.Remove(item);
                        TempData["SuccessMessage"] = "Item removed from cart";
                    }
                    else
                    {
                        item.Quantity = quantity;
                        TempData["SuccessMessage"] = "Quantity updated";
                    }
                    
                    await SaveCartToDatabase(cart);
                }
                
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error updating quantity: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        public async Task<IActionResult> RemoveFromCart(int productId)
        {
            if (User.Identity?.IsAuthenticated != true)
                return RedirectToAction("Login", "Account");

            try
            {
                var cart = await GetOrCreateUserCart();
                var itemToRemove = cart.CartItems.FirstOrDefault(item => item.ProductId == productId);
                
                if (itemToRemove != null)
                {
                    cart.CartItems.Remove(itemToRemove);
                    await SaveCartToDatabase(cart);
                    TempData["SuccessMessage"] = "Item removed from cart";
                }
                
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error removing item: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> Checkout()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                TempData["InfoMessage"] = "Please log in to checkout.";
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var cart = await GetOrCreateUserCart();
                
                if (!cart.CartItems.Any())
                {
                    TempData["ErrorMessage"] = "Your cart is empty.";
                    return RedirectToAction(nameof(Index));
                }
                
                await LoadProductDetails(cart);
                
               
                var customer = await _context.Customers.FindAsync(GetCurrentUserId());
                cart.Customer = customer;
                
                return View(cart);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error processing checkout: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        public async Task<IActionResult> PlaceOrder(string paymentMethod)
        {
            if (User.Identity?.IsAuthenticated != true)
                return RedirectToAction("Login", "Account");

            try
            {
                int userId = GetCurrentUserId();
                var cart = await GetOrCreateUserCart();
                
                if (!cart.CartItems.Any())
                {
                    TempData["ErrorMessage"] = "Your cart is empty.";
                    return RedirectToAction(nameof(Index));
                }
                
                await LoadProductDetails(cart);
                
                if (!cart.CartItems.Any())
                {
                    TempData["ErrorMessage"] = "Cart items are no longer available.";
                    return RedirectToAction(nameof(Index));
                }
                
                decimal totalAmount = cart.CartItems
                    .Where(i => i.Product != null)
                    .Sum(i => i.Product!.Price * i.Quantity);
                
                using var transaction = await _context.Database.BeginTransactionAsync();
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
                    
                    // Create order items
                    foreach (var cartItem in cart.CartItems.Where(c => c.Product != null))
                    {
                        var orderItem = new OrderItem
                        {
                            OrderId = order.OrderId,
                            ProductId = cartItem.ProductId,
                            Quantity = cartItem.Quantity,
                            Size = cartItem.Size,
                            UnitPrice = cartItem.Product!.Price,
                            TotalPrice = cartItem.Product.Price * cartItem.Quantity,
                            ProductName = cartItem.Product.Name
                        };
                        
                        _context.OrderItems.Add(orderItem);
                    }
                    
                    // Create payment
                    var payment = new Payment
                    {
                        OrderId = order.OrderId,
                        PaymentMethod = paymentMethod,
                        IsPaid = paymentMethod == "Credit Card"
                    };
                    
                    _context.Payments.Add(payment);
                    await _context.SaveChangesAsync();
                    
                    // Delete cart
                    _context.Carts.Remove(cart);
                    await _context.SaveChangesAsync();
                    
                    // Clear cart cookie
                    Response.Cookies.Append("CartCount", "0");
                    
                    await transaction.CommitAsync();
                    
                    TempData["SuccessMessage"] = "Order placed successfully!";
                    return RedirectToAction("OrderConfirmation", new { orderId = order.OrderId });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw new Exception($"Order processing error: {ex.Message}", ex);
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error placing order: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> OrderConfirmation(int orderId)
        {
            if (User.Identity?.IsAuthenticated != true)
                return RedirectToAction("Login", "Account");

            try
            {
                var order = await _context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
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
                TempData["ErrorMessage"] = "Error loading order confirmation: " + ex.Message;
                return RedirectToAction("Index", "Home");
            }
        }

      
        
        private bool RequiresSize(Product product)
        {
            return !IsBag(product);
        }

        private bool IsBag(Product product)
        {
            return product.Category?.Name?.ToUpper().Contains("BAG") == true;
        }
    }
}