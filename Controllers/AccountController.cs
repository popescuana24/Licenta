using ClothingWebApp.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ClothingWebApp.Controllers
{
    public class AccountController : Controller
    {
        // Temporary in-memory collection for customers
        private static List<Customer> _customers = new List<Customer>
        {
            new Customer
            {
                CustomerId = 1,
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com",
                Address = "123 Main St",
                Password = "password123"
            }
        };
        private static int _nextCustomerId = 2;

        // GET: Account/Login
        public IActionResult Login(string returnUrl = "")
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, string returnUrl = "")
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("", "Email and password are required.");
                return View();
            }

            var customer = _customers.FirstOrDefault(c => 
                c.Email.Equals(email, System.StringComparison.OrdinalIgnoreCase) && 
                c.Password == password);

            if (customer == null)
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View();
            }

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
        public IActionResult Register(Customer customer)
        {
            if (ModelState.IsValid)
            {
                if (_customers.Any(c => c.Email.Equals(customer.Email, System.StringComparison.OrdinalIgnoreCase)))
                {
                    ModelState.AddModelError("Email", "Email is already registered.");
                    return View(customer);
                }

                customer.CustomerId = _nextCustomerId++;
                _customers.Add(customer);

                TempData["SuccessMessage"] = "Registration successful! Please log in.";
                return RedirectToAction(nameof(Login));
            }
            return View(customer);
        }

        // POST: Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        // GET: Account/Profile
        public IActionResult Profile()
        {
            int userId = GetCurrentUserId();
            if (userId == 0)
            {
                return RedirectToAction(nameof(Login));
            }

            var customer = _customers.FirstOrDefault(c => c.CustomerId == userId);
            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }
        
        // GET: Account/EditProfile
        public IActionResult EditProfile()
        {
            int userId = GetCurrentUserId();
            if (userId == 0)
            {
                return RedirectToAction(nameof(Login));
            }

            var customer = _customers.FirstOrDefault(c => c.CustomerId == userId);
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
            int userId = GetCurrentUserId();
            if (userId == 0 || userId != customer.CustomerId)
            {
                return RedirectToAction(nameof(Login));
            }

            if (ModelState.IsValid)
            {
                var existingCustomer = _customers.FirstOrDefault(c => c.CustomerId == userId);
                if (existingCustomer != null)
                {
                    // Update the customer information
                    existingCustomer.FirstName = customer.FirstName;
                    existingCustomer.LastName = customer.LastName;
                    existingCustomer.Address = customer.Address;
                    
                    // Update authentication cookie to reflect name changes
                    await UpdateAuthenticationCookie(existingCustomer);
                    
                    TempData["SuccessMessage"] = "Your profile has been updated successfully.";
                    return RedirectToAction(nameof(Profile));
                }
            }

            return View(customer);
        }
        
        // GET: Account/ChangePassword
        public IActionResult ChangePassword()
        {
            int userId = GetCurrentUserId();
            if (userId == 0)
            {
                return RedirectToAction(nameof(Login));
            }

            return View();
        }
        
        // POST: Account/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangePassword(string currentPassword, string newPassword, string confirmNewPassword)
        {
            int userId = GetCurrentUserId();
            if (userId == 0)
            {
                return RedirectToAction(nameof(Login));
            }

            var customer = _customers.FirstOrDefault(c => c.CustomerId == userId);
            if (customer == null)
            {
                return NotFound();
            }

            if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmNewPassword))
            {
                ModelState.AddModelError("", "All fields are required.");
                return View();
            }

            if (customer.Password != currentPassword)
            {
                ModelState.AddModelError("", "Current password is incorrect.");
                return View();
            }

            if (newPassword != confirmNewPassword)
            {
                ModelState.AddModelError("", "New password and confirmation do not match.");
                return View();
            }

            // Update the password
            customer.Password = newPassword;
            
            TempData["SuccessMessage"] = "Your password has been changed successfully.";
            return RedirectToAction(nameof(Profile));
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "CustomerId");
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            return 0;
        }
        
        private async Task UpdateAuthenticationCookie(Customer customer)
        {
            // Sign out the current user
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            
            // Create updated claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, $"{customer.FirstName} {customer.LastName}"),
                new Claim(ClaimTypes.Email, customer.Email),
                new Claim(ClaimTypes.NameIdentifier, customer.CustomerId.ToString()),
                new Claim("CustomerId", customer.CustomerId.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            
            // Sign in with updated claims
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                new AuthenticationProperties { IsPersistent = true });
        }
    }
}