using ClothingWebApp.Data;
using ClothingWebApp.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using BCrypt.Net;

namespace ClothingWebApp.Controllers
{
    
   
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        
        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }
        
        // GET method for the login page
        // Stores the returnUrl (where to redirect after login) in ViewBag
        public IActionResult Login(string returnUrl = "")
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }
        
        //POST method for login form submission
         [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
      { 
       // Validate credentials
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
       {
          ModelState.AddModelError("", "Email and password are required.");
          return View();
       }
    
          var customer = await _context.Customers
        .FirstOrDefaultAsync(c => c.Email.Equals(email));
        
        if (customer == null)
       {
          ModelState.AddModelError("", "Invalid email or password.");
          return View();
       }
    
       // Verify password
       bool passwordVerified = BCrypt.Net.BCrypt.Verify(password, customer.Password);
    
        if (!passwordVerified){
          ModelState.AddModelError("", "Invalid email or password.");
          return View();
    }
    
      // Log in the user
       await LoginUser(customer);
    
      // redirect to home page
       return RedirectToAction("Index", "Home");
   }
        
        

        //GET method for the registration page
        public IActionResult Register()
        {
            return View();
        }
        
        //POST method for registration form submission
        [HttpPost]
        public async Task<IActionResult> Register(Customer customer){
            // Quick validation
            if (!ModelState.IsValid)
         {
             return View(customer);
        }

            // Check for duplicate email
            if (await _context.Customers.AnyAsync(c => c.Email == customer.Email))
       {
            ModelState.AddModelError("Email", "This email is already registered");
            return View(customer);
       }
    
            // Hash password and save
            customer.Password = BCrypt.Net.BCrypt.HashPassword(customer.Password);
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
    
           // Log in and redirect
           await LoginUser(customer);
           TempData["SuccessMessage"] = "Welcome! Your account has been created.";
    
          return RedirectToAction("Index", "Home");
    }
   
       //LOGOUT
        public async Task<IActionResult> Logout()
        {
            
           
           Response.Cookies.Append("CartCount", "0");
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
        
        /// Shows the user profile
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
        
        //GET method for the edit profile page
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
     
        //POST method for profile update
        [HttpPost]
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
        //GET method for password change page
        public IActionResult ChangePassword()
        {
            if (GetCurrentUserId() == 0)
            {
                return RedirectToAction("Login");
            }
            return View();
        }

        //POST method for password change
       [HttpPost]
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
       //Retrieves the customer from database
        var customer = await _context.Customers.FindAsync(customerId);
        if (customer == null)
       {
          return NotFound();
       }
    
       bool passwordVerified = false;
    
       // Check if it's a BCrypt hash
       if (customer.Password.StartsWith("$2a$") || customer.Password.StartsWith("$2b$") || customer.Password.StartsWith("$2y$"))
         {
            // Verify with BCrypt
             passwordVerified = BCrypt.Net.BCrypt.Verify(currentPassword, customer.Password);
         }
       else
        {
            // For old customers, verify the old way
            passwordVerified = (customer.Password == currentPassword);
        }
    
         if (!passwordVerified)
        {
             ModelState.AddModelError("", "Current password is incorrect.");
             return View();
        }
    
        // Hash the new password before saving
        customer.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _context.SaveChangesAsync();
    
        TempData["SuccessMessage"] = "Password changed successfully.";
          return RedirectToAction(nameof(Profile));
        }
    
    //methods that help to keep the details of a customer 
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
         
        public async Task<IActionResult> OrderHistory()
    {
        int customerId = GetCurrentUserId();
        if (customerId == 0)
        {
            return RedirectToAction("Login");
        }
    
    // Get all orders for this customer with related data
         var orders = await _context.Orders
            .Where(o => o.CustomerId == customerId)
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .Include(o => o.Customer)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    
            return View(orders);
    }
        
        
    }
}