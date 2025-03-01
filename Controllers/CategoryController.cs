using ClothingWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;

namespace ClothingWebApp.Controllers
{
    public class CategoryController : Controller
    {
        private static List<Category> _categories = new List<Category>
        {
            new Category { CategoryId = 1, Name = "Bags", Description = "Stylish bags for all occasions", Products = new List<Product>() },
            new Category { CategoryId = 2, Name = "Blazers", Description = "Elegant blazers for professional look", Products = new List<Product>() },
            new Category { CategoryId = 3, Name = "Dresses/Jumpsuits", Description = "Beautiful dresses and jumpsuits", Products = new List<Product>() },
            new Category { CategoryId = 4, Name = "Jackets", Description = "Trendy jackets for all seasons", Products = new List<Product>() },
            new Category { CategoryId = 5, Name = "Jeans", Description = "Comfortable and stylish jeans", Products = new List<Product>() }
        };
        
        private static int _nextCategoryId = 6;
        
        // CUSTOMER-FACING ACTIONS
        
        // GET: Category
        public IActionResult Index()
        {
            return View(_categories);
        }
        
        // GET: Category/Bags
        public IActionResult Bags()
        {
            var category = _categories.FirstOrDefault(c => c.Name == "Bags");
            return View("CategoryPage", category);
        }
        
        // GET: Category/Blazers
        public IActionResult Blazers()
        {
            var category = _categories.FirstOrDefault(c => c.Name == "Blazers");
            return View("CategoryPage", category);
        }
        
        // GET: Category/Dresses
        public IActionResult Dresses()
        {
            var category = _categories.FirstOrDefault(c => c.Name == "Dresses/Jumpsuits");
            return View("CategoryPage", category);
        }
        
        // GET: Category/Jackets
        public IActionResult Jackets()
        {
            var category = _categories.FirstOrDefault(c => c.Name == "Jackets");
            return View("CategoryPage", category);
        }
        
        // GET: Category/Jeans
        public IActionResult Jeans()
        {
            var category = _categories.FirstOrDefault(c => c.Name == "Jeans");
            return View("CategoryPage", category);
        }
        
        // ADMIN/BACKEND CRUD OPERATIONS
        
        // GET: Category/Admin
        public IActionResult Admin()
        {
            return View(_categories);
        }
        
        // GET: Category/Details/5
        public IActionResult Details(int id)
        {
            var category = _categories.FirstOrDefault(c => c.CategoryId == id);
            if (category == null)
            {
                return NotFound();
            }
            
            return View(category);
        }
        
        // GET: Category/Create
        public IActionResult Create()
        {
            return View();
        }
        
        // POST: Category/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Category category)
        {
            if (ModelState.IsValid)
            {
                category.CategoryId = _nextCategoryId++;
                category.Products = new List<Product>();
                _categories.Add(category);
                return RedirectToAction(nameof(Admin));
            }
            return View(category);
        }
        
        // GET: Category/Edit/5
        public IActionResult Edit(int id)
        {
            var category = _categories.FirstOrDefault(c => c.CategoryId == id);
            if (category == null)
            {
                return NotFound();
            }
            
            return View(category);
        }
        
        // POST: Category/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Category category)
        {
            if (id != category.CategoryId)
            {
                return NotFound();
            }
            
            if (ModelState.IsValid)
            {
                var existingCategory = _categories.FirstOrDefault(c => c.CategoryId == id);
                if (existingCategory != null)
                {
                    existingCategory.Name = category.Name;
                    existingCategory.Description = category.Description;
                    // Keep the existing Products reference
                }
                
                return RedirectToAction(nameof(Admin));
            }
            return View(category);
        }
        
        // GET: Category/Delete/5
        public IActionResult Delete(int id)
        {
            var category = _categories.FirstOrDefault(c => c.CategoryId == id);
            if (category == null)
            {
                return NotFound();
            }
            
            return View(category);
        }
        
        // POST: Category/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var category = _categories.FirstOrDefault(c => c.CategoryId == id);
            if (category != null)
            {
                _categories.Remove(category);
            }
            
            return RedirectToAction(nameof(Admin));
        }
    }
}