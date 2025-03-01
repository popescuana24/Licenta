using ClothingWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Linq;

namespace ClothingWebApp.Controllers
{
    public class ProductController : Controller
    {
        private static List<Product> _products = new List<Product>();
        private static int _nextProductId = 1;
        
        // GET: Product
        public IActionResult Index()
        {
            var products = _products.Select(p => {
                if (p.Category == null)
                {
                    p.Category = GetDefaultCategory(p.CategoryId);
                }
                return p;
            }).ToList();
            
            return View(products);
        }
        
        // GET: Product/Details/5
        public IActionResult Details(int id)
        {
            var product = _products.FirstOrDefault(p => p.ProductId == id);
            if (product == null)
            {
                return NotFound();
            }
            
            if (product.Category == null)
            {
                product.Category = GetDefaultCategory(product.CategoryId);
            }
            
            return View(product);
        }
        
        // GET: Product/Create
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(GetCategories(), "CategoryId", "Name");
            return View();
        }
        
        // POST: Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Product product)
        {
            if (ModelState.IsValid)
            {
                product.ProductId = _nextProductId++;
                
                // Set the Category property
                var category = GetCategories().FirstOrDefault(c => c.CategoryId == product.CategoryId);
                product.Category = category ?? GetDefaultCategory(product.CategoryId);
                
                _products.Add(product);
                return RedirectToAction(nameof(Index));
            }
            
            ViewData["CategoryId"] = new SelectList(GetCategories(), "CategoryId", "Name", product.CategoryId);
            return View(product);
        }
        
        // GET: Product/Edit/5
        public IActionResult Edit(int id)
        {
            var product = _products.FirstOrDefault(p => p.ProductId == id);
            if (product == null)
            {
                return NotFound();
            }
            
            ViewData["CategoryId"] = new SelectList(GetCategories(), "CategoryId", "Name", product.CategoryId);
            return View(product);
        }
        
        // POST: Product/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Product product)
        {
            if (id != product.ProductId)
            {
                return NotFound();
            }
            
            if (ModelState.IsValid)
            {
                var existingProduct = _products.FirstOrDefault(p => p.ProductId == id);
                if (existingProduct != null)
                {
                    existingProduct.Name = product.Name;
                    existingProduct.Description = product.Description;
                     existingProduct.Price = product.Price;
                    existingProduct.ImageUrl = product.ImageUrl;
                    existingProduct.Color = product.Color;
                    existingProduct.CategoryId = product.CategoryId;
                    
                    // Update Category property
                    var category = GetCategories().FirstOrDefault(c => c.CategoryId == product.CategoryId);
                    existingProduct.Category = category ?? GetDefaultCategory(product.CategoryId);
                }
                
                return RedirectToAction(nameof(Index));
            }
            
            ViewData["CategoryId"] = new SelectList(GetCategories(), "CategoryId", "Name", product.CategoryId);
            return View(product);
        }
        
        // GET: Product/Delete/5
        public IActionResult Delete(int id)
        {
            var product = _products.FirstOrDefault(p => p.ProductId == id);
            if (product == null)
            {
                return NotFound();
            }
            
            if (product.Category == null)
            {
                product.Category = GetDefaultCategory(product.CategoryId);
            }
            
            return View(product);
        }
        
        // POST: Product/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var product = _products.FirstOrDefault(p => p.ProductId == id);
            if (product != null)
            {
                _products.Remove(product);
            }
            
            return RedirectToAction(nameof(Index));
        }
        
        // Helper methods
        private List<Category> GetCategories()
        {
            return new List<Category>
            {
                new Category { CategoryId = 1, Name = "Men's Clothing", Description = "Clothing for men", Products = new List<Product>() },
                new Category { CategoryId = 2, Name = "Women's Clothing", Description = "Clothing for women", Products = new List<Product>() },
                new Category { CategoryId = 3, Name = "Accessories", Description = "Fashion accessories", Products = new List<Product>() }
            };
        }
        
        private Category GetDefaultCategory(int categoryId)
        {
            return new Category
            {
                CategoryId = categoryId,
                Name = "Unknown Category",
                Description = "Category not found",
                Products = new List<Product>()
            };
        }
    }
}