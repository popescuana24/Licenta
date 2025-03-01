using ClothingWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Linq;

namespace ClothingWebApp.Controllers
{
    public class PaymentController : Controller
    {
        private static List<Payment> _payments = new List<Payment>();
        private static int _nextPaymentId = 1;
        
        // GET: Payment
        public IActionResult Index()
        {
            var payments = _payments.ToList();
            foreach (var payment in payments)
            {
                if (payment.Order == null)
                {
                    payment.Order = GetDefaultOrder(payment.OrderId);
                }
            }
            
            return View(payments);
        }
        
        // GET: Payment/Details/5
        public IActionResult Details(int id)
        {
            var payment = _payments.FirstOrDefault(p => p.PaymentId == id);
            if (payment == null)
            {
                return NotFound();
            }
            
            if (payment.Order == null)
            {
                payment.Order = GetDefaultOrder(payment.OrderId);
            }
            
            return View(payment);
        }
        
        // GET: Payment/Create
        public IActionResult Create()
        {
            ViewData["OrderId"] = new SelectList(GetOrders(), "OrderId", "OrderId");
            return View();
        }
        
        // POST: Payment/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Payment payment)
        {
            if (ModelState.IsValid)
            {
                payment.PaymentId = _nextPaymentId++;
                
                // Ensure Order property is set
                var order = GetOrders().FirstOrDefault(o => o.OrderId == payment.OrderId);
                payment.Order = order ?? GetDefaultOrder(payment.OrderId);
                
                _payments.Add(payment);
                return RedirectToAction(nameof(Index));
            }
            
            ViewData["OrderId"] = new SelectList(GetOrders(), "OrderId", "OrderId");
            return View(payment);
        }
        
        // GET: Payment/Edit/5
        public IActionResult Edit(int id)
        {
            var payment = _payments.FirstOrDefault(p => p.PaymentId == id);
            if (payment == null)
            {
                return NotFound();
            }
            
            ViewData["OrderId"] = new SelectList(GetOrders(), "OrderId", "OrderId", payment.OrderId);
            return View(payment);
        }
        
        // POST: Payment/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Payment payment)
        {
            if (id != payment.PaymentId)
            {
                return NotFound();
            }
            
            if (ModelState.IsValid)
            {
                var existingPayment = _payments.FirstOrDefault(p => p.PaymentId == id);
                if (existingPayment != null)
                {
                    existingPayment.OrderId = payment.OrderId;
                    existingPayment.PaymentMethod = payment.PaymentMethod;
                    existingPayment.IsPaid = payment.IsPaid;
                    
                    // Update Order property
                    var order = GetOrders().FirstOrDefault(o => o.OrderId == payment.OrderId);
                    existingPayment.Order = order ?? GetDefaultOrder(payment.OrderId);
                }
                
                return RedirectToAction(nameof(Index));
            }
            
            ViewData["OrderId"] = new SelectList(GetOrders(), "OrderId", "OrderId", payment.OrderId);
            return View(payment);
        }
        
        // GET: Payment/Delete/5
        public IActionResult Delete(int id)
        {
            var payment = _payments.FirstOrDefault(p => p.PaymentId == id);
            if (payment == null)
            {
                return NotFound();
            }
            
            if (payment.Order == null)
            {
                payment.Order = GetDefaultOrder(payment.OrderId);
            }
            
            return View(payment);
        }
        
        // POST: Payment/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var payment = _payments.FirstOrDefault(p => p.PaymentId == id);
            if (payment != null)
            {
                _payments.Remove(payment);
            }
            
            return RedirectToAction(nameof(Index));
        }
        
        // Helper methods
        private List<Order> GetOrders()
        {
            // Create a default customer for these orders
            var customer = new Customer
            {
                CustomerId = 1,
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com",
                Address = "123 Main St",
                Password = "password123"
            };
            
            return new List<Order>
            {
                new Order
                {
                    OrderId = 1,
                    CustomerId = 1,
                    Customer = customer,
                    OrderDate = System.DateTime.Now,
                    TotalAmount = 99.99m
                }
            };
        }
        
        private Order GetDefaultOrder(int orderId)
        {
            // Create a default customer for this order
            var customer = new Customer
            {
                CustomerId = 1,
                FirstName = "Unknown",
                LastName = "Customer",
                Email = "unknown@example.com",
                Address = "Unknown",
                Password = "unknown"
            };
            
            return new Order
            {
                OrderId = orderId,
                CustomerId = 1,
                Customer = customer,
                OrderDate = System.DateTime.Now,
                TotalAmount = 0
            };
        }
    }
}