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

        //GET: Cart
        //This method retrieves the current customer's cart and displays it
        public async Task<IActionResult> Index()
        {
            //calls the  method GetCurrentCustomer()
            //asynchronous database call so we can await it
            var customer = await GetCurrentCustomer();
            if (customer == null)
            {
                TempData["Message"] = "Please log in to view your cart";
                //redirects the user to the Login action of the Account controller if they are not logged in
                return RedirectToAction("Login", "Account");
            }
            //calls the method GetOrCreateCart() to get the user's cart or create a new one if it doesn't exist
            //asynchronous database call so we can await it
            var cart = await GetOrCreateCart();
            //calls the method LoadProductDetails(cart) to load product details for each item in the cart
            await LoadProductDetails(cart);
            //saves the cart to the database
            await SaveCart(cart);

            return View(cart);
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, string selectedSize = "")
        {
            //retrieves the currently logged-in customer asynchronously
            var customer = await GetCurrentCustomer();
            if (customer == null)
            {
                //if the customer is not logged in, receive a message and redirect to the login page
                TempData["Message"] = "Please log in to add items to cart";
                return RedirectToAction("Login", "Account");
            }
            // Fetch the product details from the database
            //using Include to load the related Category entity din care apartine
            var product = await _context.Products
                .Include(p => p.Category)
                //safely return the product or null if not found
                .FirstOrDefaultAsync(p => p.ProductId == productId);

            if (product == null)
            {
                TempData["Message"] = "Product not found";
                return RedirectToAction("Index", "Home");
            }

            //checks if the selectedSize parameter is empty or null
            if (string.IsNullOrEmpty(selectedSize))
            {
                //calls a helper method IsBag(product) to check if the product is a bag
                if (IsBag(product))
                {
                    selectedSize = "One Size";
                }
                else
                {
                    TempData["Message"] = "Please select a size";
                    //redirects the user back to the product details page so they can select a size
                    return RedirectToAction("Details", "Product", new { id = productId });
                }
            }
            //retrieves or creates the user's cart
            var cart = await GetOrCreateCart();

            // Check if item already exists, FirstOrDefault returns the first matching item or null if not found(este in memory)
            var existingItem = cart.CartItems.FirstOrDefault(item =>
                item.ProductId == productId && item.Size == selectedSize);

            if (existingItem != null)
            {
                existingItem.Quantity++;
            }
            else
            {
                //creates a new CartProduct object 
                //and adds it to the cart's CartItems collection
                cart.CartItems.Add(new CartProduct
                {
                    ProductId = productId,
                    Size = selectedSize,
                    Quantity = 1
                });
            }

            await SaveCart(cart);
            TempData["Message"] = "Added to cart!";

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int productId, int quantity)
        {
            var customer = await GetCurrentCustomer();
            if (customer == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var cart = await GetOrCreateCart();
            //searches the cart for the item with the specified productId
            var item = cart.CartItems.FirstOrDefault(item => item.ProductId == productId);

            if (item != null)
            {
                if (quantity <= 0)
                {
                    //Removes the item from the cart
                    cart.CartItems.Remove(item);
                    TempData["Message"] = "Item removed from cart";
                }
                else
                {
                    //pdates the item's quantity to the new value
                    item.Quantity = quantity;
                    TempData["Message"] = "Quantity updated";
                }

                await SaveCart(cart);
            }

            return RedirectToAction("Index");
        }

        //method to remove an item from the cart
        [HttpPost]
        public async Task<IActionResult> RemoveFromCart(int productId)
        {
            var customer = await GetCurrentCustomer();
            if (customer == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var cart = await GetOrCreateCart();
            //Searches the cart's items for the one with the matching productId
            var item = cart.CartItems.FirstOrDefault(item => item.ProductId == productId); //returns the first matching item or null if not found

            if (item != null)
            {
                //removes it from the cart and save the changes
                cart.CartItems.Remove(item);
                await SaveCart(cart); // Save the updated cart to the database
                TempData["Message"] = "Item removed from cart";
            }

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Checkout()
        {
            var customer = await GetCurrentCustomer();
            if (customer == null)
            {
                TempData["Message"] = "Please log in to checkout";
                return RedirectToAction("Login", "Account");
            }

            var cart = await GetOrCreateCart();

            //checks if the cart has no items
            if (!cart.CartItems.Any())
            {
                TempData["Message"] = "Your cart is empty";
                return RedirectToAction("Index");
            }
            //loads product details for each item in the cart
            await LoadProductDetails(cart);
            //associates the cart with the current customer
            cart.Customer = customer;

            return View(cart);
        }

        [HttpPost]
        //takes paymentMethod as a parameter
        public async Task<IActionResult> PlaceOrder(string paymentMethod)
        {
            var customer = await GetCurrentCustomer();
            if (customer == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var cart = await GetOrCreateCart();
            //checks if the cart has no items
            //if it does, it redirects the user to the Index action with a message
            if (!cart.CartItems.Any())
            {
                TempData["Message"] = "Your cart is empty";
                return RedirectToAction("Index");
            }
            //loads product details for each item in the cart
            await LoadProductDetails(cart);

            // checks if the cart has no items after loading product details
            if (!cart.CartItems.Any())
            {
                TempData["Message"] = "Cart items are no longer available";
                return RedirectToAction("Index");
            }
            // Calculate total amount for the order
            decimal totalAmount = cart.CartItems
                .Where(i => i.Product != null)
                .Sum(i => i.Product!.Price * i.Quantity);

            // group of database operations happen together 
            //start a database tto be sure that all steps (order, order items, payment, cart removal) happened
            using var transaction = await _context.Database.BeginTransactionAsync();

            //creates a new Order object with customer, date, and total amount
            var order = new Order
            {
                CustomerId = customer.CustomerId,
                OrderDate = DateTime.Now,
                TotalAmount = totalAmount
            };

            //adds the order to the database and saves it
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            //Creates an OrderItem for each cart item
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

            //removes the user's cart from the database afterthe order is complete
            _context.Carts.Remove(cart);
            await _context.SaveChangesAsync();

            //updates the browser cookie that tracks the cart item count to 0
            Response.Cookies.Append("CartCount", "0");

            //commits all database changes
            //ensures order, order items, payment, and cart removal happen
            await transaction.CommitAsync();

            TempData["Message"] = "Order placed successfully!";
            return RedirectToAction("OrderConfirmation", new { orderId = order.OrderId });
        }

        public async Task<IActionResult> OrderConfirmation(int orderId)
        {
            var customer = await GetCurrentCustomer();
            if (customer == null)
            {
                return RedirectToAction("Login", "Account");
            }
            //retrieves the order details from the database using the provided orderId
            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                //returns the first matching order or null if not found
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                TempData["Message"] = "Order not found";
                return RedirectToAction("Index", "Home");
            }

            //retrieves the payment record linked to the order using the orderId
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.OrderId == orderId);

            //save order and payment details in ViewBag for use in the view
            //temporary storage container
            ViewBag.Order = order;
            ViewBag.Payment = payment;

            return View();
        }

        //metode ajutatoare pentru restul metodelor
        //if it does not find a customer, it returns null and we use ?
        private async Task<Customer?> GetCurrentCustomer()
        {
            if (User.Identity?.IsAuthenticated != true)
                return null;
            //finds the "CustomerId" claim in the current user's claims
            var userIdClaim = User.FindFirst("CustomerId");
            //checks if the "CustomerId" claim exists and if its value can be parsed as an integer
            //if parsing succeeds, the parsed integer customer ID is stored in userId
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return null;

            return await _context.Customers.FindAsync(userId);
        }

        //
        private async Task<Cart> GetOrCreateCart()
        {
            //Checks if the customer is null
            var customer = await GetCurrentCustomer();
            if (customer == null)
                throw new InvalidOperationException("User not authenticated");

            //query the database context _context to find a cart where the CustomerId matches the current user's ID
            var cart = await _context.Carts
                .FirstOrDefaultAsync(c => c.CustomerId == customer.CustomerId);

            if (cart == null)
            {
                //if no cart was found (cart == null) this block creates a new Cart object
                cart = new Cart
                {
                    //we set the customerId to the current user's ID
                    CustomerId = customer.CustomerId,
                    //initializes CartItemsJson to "[]"
                    CartItemsJson = "[]"
                };
                //adds the new cart to the database context
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            // Load cart items from JSON
            try
            {
                //deserialize the JSON string stored in CartItemsJson into a list of CartProduct objects
                cart.CartItems = JsonSerializer.Deserialize<List<CartProduct>>(cart.CartItemsJson)
                //if deserialization fails or results in null, it assigns an empty list 
                    ?? new List<CartProduct>();
            }
            catch
            {
                cart.CartItems = new List<CartProduct>();
            }

            return cart;
        }

        private async Task SaveCart(Cart cart)
        {
            //onvert the cart's list of items into a JSON string and store it in CartItemsJson
            cart.CartItemsJson = JsonSerializer.Serialize(cart.CartItems);
            //updates the cart in the database
            _context.Carts.Update(cart);
            //saves the changes to the database asynchronously
            await _context.SaveChangesAsync();

            //Calculate the total quantity of all items in the cart
            int totalItems = cart.CartItems.Sum(item => item.Quantity);

            // Update the "CartCount" cookie with the total number of items as a string
            Response.Cookies.Append("CartCount", totalItems.ToString());
        }

        private async Task LoadProductDetails(Cart cart)
        {
            //iterate over a copy of the cart items (ToList creates a copy) creeaza o lista 
            foreach (var item in cart.CartItems.ToList())
            {
                //query the database for the product including its category based on the ProductId
                var product = await _context.Products
                    .Include(p => p.Category)
                    .FirstOrDefaultAsync(p => p.ProductId == item.ProductId);

                if (product != null)
                {
                    item.Product = product;
                }
                else
                {
                    cart.CartItems.Remove(item);
                }
            }
        }

        private bool IsBag(Product product)
        {
            //check if the product's category name contains the word "BAG" (case insensitive)
            return product.Category?.Name?.ToUpper().Contains("BAG") == true;
        }
    }
}