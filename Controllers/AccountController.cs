using ClothingWebApp.Data;
using ClothingWebApp.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ClothingWebApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        
        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }
        
        // GET: Account/Login
        public IActionResult Login(string returnUrl = "")
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }
        
        // POST: Account/Login
        // In AccountController.cs, update the Login POST method
// POST: Account/Login
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Login(string email, string password, string returnUrl = "")
{
    if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
    {
        ModelState.AddModelError("", "Email and password are required.");
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }
    
    var customer = await _context.Customers
        .FirstOrDefaultAsync(c => c.Email.Equals(email) && c.Password == password);
        
    if (customer == null)
    {
        ModelState.AddModelError("", "Invalid email or password.");
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }
    
    await LoginUser(customer);
    
    // Restore cart from database
    var cart = await _context.Carts.FirstOrDefaultAsync(c => c.CustomerId == customer.CustomerId);
    if (cart != null)
    {
        // Cart exists in database, update cart count cookie
        // In a full implementation, you would load the cart items here too
        var cartCount = 0; // Calculate actual cart item count here
        Response.Cookies.Append("CartCount", cartCount.ToString());
    }
    
    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
    {
        return Redirect(returnUrl);
    }
    
    return RedirectToAction("Index", "Home");
}


        // GET: Account/Register
        public IActionResult Register()
        {
            return View();
        }
        
        // POST: Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(Customer customer)
        {
            if (ModelState.IsValid)
            {
                // Check if email already exists
                var existingCustomer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.Email.Equals(customer.Email));
                    
                if (existingCustomer != null)
                {
                    ModelState.AddModelError("", "Email already registered.");
                    return View(customer);
                }
                
                // Add customer to database - don't set CustomerId
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();
                
                // Automatically log in the new user
                await LoginUser(customer);
                
                TempData["SuccessMessage"] = "Registration successful!";
                return RedirectToAction("Index", "Home");
            }
            
            return View(customer);
        }
        
        // GET: Account/Logout
        // In AccountController.cs
// GET: Account/Logout
public async Task<IActionResult> Logout()
{
    // Don't clear the cart from cookies when logging out
    // Just clear it from the session so it will be restored from cookie on next login
    HttpContext.Session.Remove("ShoppingCart");
    
    // Update cart count cookie for the UI
    Response.Cookies.Append("CartCount", "0");
    
    // Sign out
    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return RedirectToAction("Index", "Home");
}
        
        // GET: Account/Profile
        public async Task<IActionResult> Profile()
        {
            int customerId = GetCurrentUserId();
            
            if (customerId == 0)
            {
                return RedirectToAction("Login");
            }
            
            var customer = await _context.Customers.FindAsync(customerId);
            if (customer == null)
            {
                return NotFound();
            }
            
            return View(customer);
        }
        
        // GET: Account/EditProfile
        public async Task<IActionResult> EditProfile()
        {
            int customerId = GetCurrentUserId();
            
            if (customerId == 0)
            {
                return RedirectToAction("Login");
            }
            
            var customer = await _context.Customers.FindAsync(customerId);
            if (customer == null)
            {
                return NotFound();
            }
            
            return View(customer);
        }
        
        // POST: Account/EditProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(Customer customer)
        {
            if (ModelState.IsValid)
            {
                var existingCustomer = await _context.Customers.FindAsync(customer.CustomerId);
                if (existingCustomer == null)
                {
                    return NotFound();
                }
                
                existingCustomer.FirstName = customer.FirstName;
                existingCustomer.LastName = customer.LastName;
                existingCustomer.Address = customer.Address;
                
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Profile updated successfully.";
                return RedirectToAction(nameof(Profile));
            }
            
            return View(customer);
        }
        
        // GET: Account/ChangePassword
        public IActionResult ChangePassword()
        {
            if (GetCurrentUserId() == 0)
            {
                return RedirectToAction("Login");
            }
            
            return View();
        }
        
        // POST: Account/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmNewPassword)
        {
            int customerId = GetCurrentUserId();
            
            if (customerId == 0)
            {
                return RedirectToAction("Login");
            }
            
            if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmNewPassword))
            {
                ModelState.AddModelError("", "All fields are required.");
                return View();
            }
            
            if (newPassword != confirmNewPassword)
            {
                ModelState.AddModelError("", "New password and confirmation do not match.");
                return View();
            }
            
            var customer = await _context.Customers.FindAsync(customerId);
            if (customer == null)
            {
                return NotFound();
            }
            
            if (customer.Password != currentPassword)
            {
                ModelState.AddModelError("", "Current password is incorrect.");
                return View();
            }
            
            customer.Password = newPassword;
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Password changed successfully.";
            return RedirectToAction(nameof(Profile));
        }

        // Helper method to login a user
        private async Task LoginUser(Customer customer)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, $"{customer.FirstName} {customer.LastName}"),
                new Claim(ClaimTypes.Email, customer.Email),
                new Claim(ClaimTypes.NameIdentifier, customer.CustomerId.ToString()),
                new Claim("CustomerId", customer.CustomerId.ToString())
            };
            
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                new AuthenticationProperties { IsPersistent = true });
        }
        
        // Helper method to get current user ID
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("CustomerId");
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            
            return 0;
        }

        /// GET: Account/OrderHistory
// In AccountController.cs
public async Task<IActionResult> OrderHistory()
{
    if (!User.Identity.IsAuthenticated)
    {
        return RedirectToAction("Login");
    }
    
    try
    {
        int userId = GetCurrentUserId();
        
        var orders = await _context.Orders
            .Include(o => o.Customer)
            .Where(o => o.CustomerId == userId)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
        
        return View(orders);
    }
    catch (Exception ex)
    {
        TempData["ErrorMessage"] = "Error retrieving order history: " + ex.Message;
        return RedirectToAction("Profile");
    }
}
// GET: Account/OrderDetails
public async Task<IActionResult> OrderDetails(int id)
{
    if (!User.Identity.IsAuthenticated)
    {
        return RedirectToAction("Login");
    }
    
    try
    {
        int userId = GetCurrentUserId();
        
        var order = await _context.Orders
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.OrderId == id && o.CustomerId == userId);
            
        if (order == null)
        {
            TempData["ErrorMessage"] = "Order not found.";
            return RedirectToAction("OrderHistory");
        }
        
        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.OrderId == id);
            
        ViewBag.Payment = payment;
        
        return View(order);
    }
    catch (Exception ex)
    {
        TempData["ErrorMessage"] = "Error retrieving order details: " + ex.Message;
        return RedirectToAction("OrderHistory");
    }
}
    }}