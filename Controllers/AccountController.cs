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
        // in returnUrl i store the page where to redirect after log in in Viewbag 
        public IActionResult Login(string returnUrl = "")
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }
        
        //POST method 
        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        { 
       // check if the email and password are not empty
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
       {
          ModelState.AddModelError("", "Email and password are required");
          return View();
       }
        
          var customer = await _context.Customers
         
        .FirstOrDefaultAsync(c => c.Email.Equals(email));
        
        if (customer == null)
       {
          ModelState.AddModelError("", "Invalid email or password");
          return View();
       }
    
       // here we verify if the password matches the hashed password stored in the database
       bool passwordVerified = BCrypt.Net.BCrypt.Verify(password, customer.Password);
    
        if (!passwordVerified){
          ModelState.AddModelError("", "Invalid email or password");
          return View();
    }
    
      // Log in the user- we call the method to create cookie and sign in user
       await LoginUser(customer);
    
      
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
           
            if (!ModelState.IsValid)
         {
             return View(customer);
        }

      
            //if it exists, add an error to the model state and return the view with the
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
           TempData["SuccessMessage"] = "Welcome! Your account has been created!!";
    
            
          return RedirectToAction("Index", "Home");
    }
   
       //LOGOUT
        public async Task<IActionResult> Logout()
        {
            //When the user logs out, their cart count cookie is reset to 0
            Response.Cookies.Append("CartCount", "0");
            // Sign out the user by clearing the authentication cookie
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
        
        // Shows the user profile
        public async Task<IActionResult> Profile()
        {
            int customerId = GetCurrentUserId();
            if (customerId == 0)
            {
                return RedirectToAction("Login");
            }
            // fetches the record by primary key, else returns null
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
            // Fetches the customer record by primary key
            // If the customer is found, it returns the customer object to the view
            var customer = await _context.Customers.FindAsync(customerId);
            if (customer == null)
            {
                // If the customer is not found, return a 404 Not Found response
                return NotFound();
            }
            
            return View(customer);
        }

        //POST method for profile update
        [HttpPost]
        //we take the customer object as a parameter
        public async Task<IActionResult> EditProfile(Customer customer)
        {
            //check if the model is valid means all required fields are filled correctly
            if (ModelState.IsValid)
            {
                // loads the existing customer record from the database using the provided CustomerId from the form
                var existingCustomer = await _context.Customers.FindAsync(customer.CustomerId);
                if (existingCustomer == null)
                {
                    return NotFound();
                }
                existingCustomer.FirstName = customer.FirstName;
                existingCustomer.LastName = customer.LastName;
                existingCustomer.Address = customer.Address;
                //saves the changes to the database
                await _context.SaveChangesAsync();

                //TempData["SuccessMessage"] = "Profile updated successfully";
                return RedirectToAction("Profile");
            }
            // If the model state is not valid, return the view with the current customer data
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
        // Check if the current password, new password, and confirm new password fields are not empty
         if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmNewPassword))
            {
                ModelState.AddModelError("", "All fields are required!!");
                //to return the view with the error message
                return View();
            }
    
        if (newPassword != confirmNewPassword)
       {
          ModelState.AddModelError("", "New password and confirmation do not match");
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
            // pt parolele vechi
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

            //TempData["SuccessMessage"] = "Password changed successfully.";
            return RedirectToAction("Profile");
        }

        public async Task<IActionResult> OrderHistory()
        {
            int customerId = GetCurrentUserId();
            if (customerId == 0)
            {
                return RedirectToAction("Login");
            }
    
            
            var orders = await _context.Orders
            .Where(o => o.CustomerId == customerId)
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .Include(o => o.Customer)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
            //to the orders history page
            return View(orders);
        }

        //methods that help to keep the details of a customer
        //Customer object is passed as a parameter
        private async Task LoginUser(Customer customer)
        {
            
            //these claims are used to create an authentication cookie that keeps the user logged in
            var claims = new List<Claim>
            {
                //$ concatenates the first and last name of the customer
                new Claim(ClaimTypes.Name, $"{customer.FirstName} {customer.LastName}"),
                new Claim(ClaimTypes.Email, customer.Email),
                //name identifier-used to uniquely identify a user
                new Claim(ClaimTypes.NameIdentifier, customer.CustomerId.ToString()),
                //stores the customer ID as a claim
                new Claim("CustomerId", customer.CustomerId.ToString())
            };
            //groups these claims together and specifies the authentication scheme — here i use the cookie authentication scheme
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            //HttpContext.SignInAsync creates an authentication cookie containing the user's claims
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                // the cookie persists even after closing the browser
                new AuthenticationProperties { IsPersistent = true });
        }
        
        // Helper method to get current user ID
        private int GetCurrentUserId()
        {
            // extracts the user ID from the claims of the currently authenticated user
            // If the claim exists and can be parsed as an integer, it returns the user ID
            var userIdClaim = User.FindFirst("CustomerId");
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            
            return 0;
        }
         
        
        
        
    }
}
