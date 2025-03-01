using ClothingWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;

namespace ClothingWebApp.Controllers 
{
    public class CartController : Controller
    {
        private static Dictionary<int, Cart> _carts = new Dictionary<int, Cart>();
        private static List<Product> _products = new List<Product>();

        // GET: Cart
        public IActionResult Index()
        {
            int userId = GetCurrentUserId();
            
            if (!_carts.ContainsKey(userId))
            {
                _carts[userId] = new Cart
                {
                    CartId = userId,
                    CustomerId = userId,
                    Customer = GetCustomer(userId),
                    Products = new List<Product>()
                };
            }
            
            return View(_carts[userId]);
        }

        // POST: Cart/AddToCart
        [HttpPost]
        public IActionResult AddToCart(int productId)
        {
            int userId = GetCurrentUserId();
            
            
            if (!_carts.ContainsKey(userId))
            {
                _carts[userId] = new Cart
                {
                    CartId = userId,
                    CustomerId = userId,
                    Customer = GetCustomer(userId),
                    Products = new List<Product>()
                };
            }
            
            var product = _products.FirstOrDefault(p => p.ProductId == productId);
            if (product != null)
            {
                _carts[userId].Products.Add(product);
            }
            
            return RedirectToAction(nameof(Index));
        }

        // POST: Cart/RemoveFromCart
        [HttpPost]
        public IActionResult RemoveFromCart(int productId)
        {
            int userId = GetCurrentUserId();
            
            if (_carts.ContainsKey(userId))
            {
                var cart = _carts[userId];
                var productToRemove = cart.Products.FirstOrDefault(p => p.ProductId == productId);
                if (productToRemove != null)
                {
                    cart.Products.Remove(productToRemove);
                }
            }
            
            return RedirectToAction(nameof(Index));
        }
        
        // GET: Cart/Checkout
        public IActionResult Checkout()
        {
            int userId = GetCurrentUserId();
            
            if (_carts.ContainsKey(userId) && _carts[userId].Products.Any())
            {
                return View(_carts[userId]);
            }
            
            return RedirectToAction(nameof(Index));
        }
        
        // POST: Cart/PlaceOrder
        [HttpPost]
        public IActionResult PlaceOrder()
        {
            int userId = GetCurrentUserId();
            
            if (_carts.ContainsKey(userId) && _carts[userId].Products.Any())
            {
                // Calculate total amount
                decimal total = _carts[userId].Products.Sum(p => p.Price);
                
                // Create customer for the order
                var customer = GetCustomer(userId);
                
                // Create an order
                var order = new Order
                {
                    OrderId = 1,
                    CustomerId = userId,
                    Customer = customer,
                    OrderDate = System.DateTime.Now,
                    TotalAmount = total
                };
                
                // Clear the cart
                _carts[userId].Products.Clear();
                
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
            // In a real app, this would come from authentication
            return 1;
        }
        
        private Customer GetCustomer(int userId)
        {
            // In a real app, this would fetch the customer from a database
            return new Customer
            {
                CustomerId = userId,
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com",
                Address = "123 Main St",
                Password = "password123"
            };
        }
    }
}