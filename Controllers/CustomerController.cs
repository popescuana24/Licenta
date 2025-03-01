using ClothingWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;

namespace ClothingWebApp.Controllers
{
    public class CustomerController : Controller
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
        
        // GET: Customer
        public IActionResult Index()
        {
            return View(_customers);
        }
        
        // GET: Customer/Details/5
        public IActionResult Details(int id)
        {
            var customer = _customers.FirstOrDefault(c => c.CustomerId == id);
            if (customer == null)
            {
                return NotFound();
            }
            
            return View(customer);
        }
        
        // GET: Customer/Create
        public IActionResult Create()
        {
            return View();
        }
        
        // POST: Customer/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Customer customer)
        {
            if (ModelState.IsValid)
            {
                customer.CustomerId = _nextCustomerId++;
                _customers.Add(customer);
                return RedirectToAction(nameof(Index));
            }
            return View(customer);
        }
        
        // GET: Customer/Edit/5
        public IActionResult Edit(int id)
        {
            var customer = _customers.FirstOrDefault(c => c.CustomerId == id);
            if (customer == null)
            {
                return NotFound();
            }
            
            return View(customer);
        }
        
        // POST: Customer/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Customer customer)
        {
            if (id != customer.CustomerId)
            {
                return NotFound();
            }
            
            if (ModelState.IsValid)
            {
                var existingCustomer = _customers.FirstOrDefault(c => c.CustomerId == id);
                if (existingCustomer != null)
                {
                    existingCustomer.FirstName = customer.FirstName;
                    existingCustomer.LastName = customer.LastName;
                    existingCustomer.Email = customer.Email;
                    existingCustomer.Address = customer.Address;
                    existingCustomer.Password = customer.Password;
                }
                
                return RedirectToAction(nameof(Index));
            }
            return View(customer);
        }
        
        // GET: Customer/Delete/5
        public IActionResult Delete(int id)
        {
            var customer = _customers.FirstOrDefault(c => c.CustomerId == id);
            if (customer == null)
            {
                return NotFound();
            }
            
            return View(customer);
        }
        
        // POST: Customer/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var customer = _customers.FirstOrDefault(c => c.CustomerId == id);
            if (customer != null)
            {
                _customers.Remove(customer);
            }
            
            return RedirectToAction(nameof(Index));
        }
    }
}