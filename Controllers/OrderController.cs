using ClothingWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ClothingWebApp.Controllers
{
    public class OrderController : Controller
    {
        private static List<Order> _orders = new List<Order>();
        private static int _nextOrderId = 1;
        
        // GET: Order
        public IActionResult Index()
        {
            var orders = _orders.Select(o => {
                if (o.Customer == null)
                {
                    o.Customer = GetDefaultCustomer(o.CustomerId);
                }
                return o;
            }).ToList();
            
            return View(orders);
        }
        
        // GET: Order/Details/5
        public IActionResult Details(int id)
        {
            var order = _orders.FirstOrDefault(o => o.OrderId == id);
            if (order == null)
            {
                return NotFound();
            }
            
            if (order.Customer == null)
            {
                order.Customer = GetDefaultCustomer(order.CustomerId);
            }
            
            return View(order);
        }
        
        // GET: Order/Create
        public IActionResult Create()
        {
            ViewData["CustomerId"] = new SelectList(GetCustomers(), "CustomerId", "FullName");
            return View();
        }
        
        // POST: Order/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Order order)
        {
            if (ModelState.IsValid)
            {
                order.OrderId = _nextOrderId++;
                order.OrderDate = DateTime.Now;
                
                // Ensure Customer property is set
                var customer = GetCustomers().FirstOrDefault(c => c.CustomerId == order.CustomerId);
                order.Customer = customer ?? GetDefaultCustomer(order.CustomerId);
                
                _orders.Add(order);
                return RedirectToAction(nameof(Index));
            }
            
            ViewData["CustomerId"] = new SelectList(GetCustomers(), "CustomerId", "FullName");
            return View(order);
        }
        
        // GET: Order/Edit/5
        public IActionResult Edit(int id)
        {
            var order = _orders.FirstOrDefault(o => o.OrderId == id);
            if (order == null)
            {
                return NotFound();
            }
            
            ViewData["CustomerId"] = new SelectList(GetCustomers(), "CustomerId", "FullName", order.CustomerId);
            return View(order);
        }
        
        // POST: Order/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Order order)
        {
            if (id != order.OrderId)
            {
                return NotFound();
            }
            
            if (ModelState.IsValid)
            {
                var existingOrder = _orders.FirstOrDefault(o => o.OrderId == id);
                if (existingOrder != null)
                {
                    existingOrder.CustomerId = order.CustomerId;
                    existingOrder.OrderDate = order.OrderDate;
                    existingOrder.TotalAmount = order.TotalAmount;
                    
                    // Update Customer property
                    var customer = GetCustomers().FirstOrDefault(c => c.CustomerId == order.CustomerId);
                    existingOrder.Customer = customer ?? GetDefaultCustomer(order.CustomerId);
                }
                
                return RedirectToAction(nameof(Index));
            }
            
            ViewData["CustomerId"] = new SelectList(GetCustomers(), "CustomerId", "FullName", order.CustomerId);
            return View(order);
        }
        
        // GET: Order/Delete/5
        public IActionResult Delete(int id)
        {
            var order = _orders.FirstOrDefault(o => o.OrderId == id);
            if (order == null)
            {
                return NotFound();
            }
            
            if (order.Customer == null)
            {
                order.Customer = GetDefaultCustomer(order.CustomerId);
            }
            
            return View(order);
        }
        
        // POST: Order/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var order = _orders.FirstOrDefault(o => o.OrderId == id);
            if (order != null)
            {
                _orders.Remove(order);
            }
            
            return RedirectToAction(nameof(Index));
        }
        
        // Helper methods
        private List<Customer> GetCustomers()
        {
            return new List<Customer>
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
        }
        
        private Customer GetDefaultCustomer(int customerId)
        {
            return new Customer
            {
                CustomerId = customerId,
                FirstName = "Unknown",
                LastName = "Customer",
                Email = "unknown@example.com",
                Address = "Unknown Address",
                Password = "unknown"
            };
        }
    }
}