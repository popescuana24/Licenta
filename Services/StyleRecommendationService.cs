using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using ClothingWebApp.Data;
using ClothingWebApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ClothingWebApp.Services
{
    public class StyleRecommendationService
    {
        private readonly ApplicationDbContext _context;
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _apiUrl = "https://api.openai.com/v1/chat/completions";
        
        //THE LOGIC FOR THE CATEGORIES I HAVE
        private readonly Dictionary<string, List<string>> CategoryCompatibility = new Dictionary<string, List<string>>
        {
            ["BAGS"] = new List<string> { "BLAZERS", "DRESSES/JUMPSUITS", "JACKETS", "SHIRTS", "SHOES", "SWEATERS", "SKIRTS", "T-SHIRTS/TOPS" },
            ["BLAZERS"] = new List<string> { "BAGS", "DRESSES/JUMPSUITS", "SHIRTS", "SHOES", "SWEATERS", "SKIRTS", "T-SHIRTS/TOPS" },
            ["DRESSES/JUMPSUITS"] = new List<string> { "BAGS", "JACKETS", "SHOES", "BLAZERS" },
            ["JACKETS"] = new List<string> { "BAGS", "DRESSES/JUMPSUITS", "SHIRTS", "SHOES", "SWEATERS", "SKIRTS", "T-SHIRTS/TOPS" },
            ["SHIRTS"] = new List<string> { "BAGS", "BLAZERS", "JACKETS", "SHOES", "SKIRTS" },
            ["SHOES"] = new List<string> { "BLAZERS", "DRESSES/JUMPSUITS", "JACKETS", "SHIRTS", "BAGS", "SWEATERS", "SKIRTS", "T-SHIRTS/TOPS" },
            ["SWEATERS"] = new List<string> { "BAGS", "BLAZERS", "JACKETS", "SHOES", "SKIRTS" },
            ["SKIRTS"] = new List<string> { "BAGS", "BLAZERS", "JACKETS", "SHIRTS", "SHOES", "SWEATERS", "T-SHIRTS/TOPS" },
            ["T-SHIRTS/TOPS"] = new List<string> { "BAGS", "BLAZERS", "JACKETS", "SHOES", "SKIRTS" }
        };

        public StyleRecommendationService(ApplicationDbContext context, string apiKey)
        {
            _context = context;
            _httpClient = new HttpClient();
            _apiKey = apiKey ?? throw new ArgumentNullException("API key is required");
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        }

      
        
        // METHOD 1: Get matching products for "Show Matching Items" button
        public async Task<StyleRecommendationResponse> GetMatchingProducts(int productId, string? categoryFilter = null)
        {
            var selectedProduct = await GetProductById(productId);
            var selectedCategory = selectedProduct.Category?.Name?.ToUpper() ?? "";
            var selectedColor = selectedProduct.Color?.ToUpper() ?? "";
            
            // Get categories that work with this product
            var compatibleCategories = GetCompatibleCategories(selectedCategory);
            
            // Filter to specific category if requested
            if (!string.IsNullOrEmpty(categoryFilter))
            {
                compatibleCategories = FilterToRequestedCategory(compatibleCategories, categoryFilter);
                if (!compatibleCategories.Any())
                {
                    return new StyleRecommendationResponse
                    {
                        Message = $"Sorry, {categoryFilter} don't typically pair well with {selectedProduct.Category?.Name ?? "this item"}.",
                        RecommendedProducts = new List<Product>()
                    };
                }
            }
            
            // Get AI color suggestions
            var aiColors = await GetAIColorSuggestions(selectedColor);
            
            // Find matching products in database
            var matchingProducts = await FindMatchingProductsInDatabase(selectedColor, aiColors, compatibleCategories, productId);
            
            // Randomize and limit results
            var finalProducts = RandomizeAndLimit(matchingProducts, 12);
            
            var message = string.IsNullOrEmpty(categoryFilter)
                ? $"Here are items that would go perfectly with your {selectedColor} {selectedProduct.Name}:"
                : $"Here are some {categoryFilter} that would match your {selectedColor} {selectedProduct.Name}:";
            
            return new StyleRecommendationResponse
            {
                Message = message,
                RecommendedProducts = finalProducts
            };
        }
        
        // METHOD 2: Get fashion advice for "Fashion Tips" button 
        public async Task<StyleRecommendationResponse> GetFashionAdvice(int productId, string? question = null)
        {
            var selectedProduct = await GetProductById(productId);
            var color = selectedProduct.Color ?? "";
            var category = selectedProduct.Category?.Name ?? "";
            var productName = selectedProduct.Name ?? "";
            
            // Create AI prompt for fashion advice
            var fashionAdvice = await GetAIFashionAdvice(color, category, productName, question);
            
            // Return ONLY advice text, NO products
            return new StyleRecommendationResponse
            {
                Message = fashionAdvice,
                RecommendedProducts = new List<Product>() // Empty - no products for fashion tips
            };
        }

        // METHOD 3: Process chat messages (for conversation, greetings, category requests)
        public async Task<StyleRecommendationResponse> ProcessChatMessage(int productId, string userMessage)
        {
            var selectedProduct = await GetProductById(productId);
            var lowerMessage = userMessage.ToLower().Trim();
            
            // Handle greetings
            if (IsGreeting(lowerMessage))
            {
                return new StyleRecommendationResponse
                {
                    Message = $"Hello! I'm your style assistant. I can help you find items that go with your {selectedProduct.Color} {selectedProduct.Name} or give you fashion advice. What would you like to know?",
                    RecommendedProducts = new List<Product>()
                };
            }
            
            // Handle thank you
            if (IsThankYou(lowerMessage))
            {
                return new StyleRecommendationResponse
                {
                    Message = "You're welcome! I'm happy to help with your style questions",
                    RecommendedProducts = new List<Product>()
                };
            }
            
            // Handle goodbye
            if (IsGoodbye(lowerMessage))
            {
                return new StyleRecommendationResponse
                {
                    Message = "Goodbye! Feel free to come back anytime for more styling advice",
                    RecommendedProducts = new List<Product>()
                };
            }
            
            // Handle specific category requests (e.g., "other bags", "show me shoes")
            var requestedCategory = ExtractCategoryRequest(lowerMessage);
            if (!string.IsNullOrEmpty(requestedCategory))
            {
                return await GetMatchingProducts(productId, requestedCategory);
            }
            
            // Handle general styling questions
            var fashionAdvice = await GetAIFashionAdvice(selectedProduct.Color, selectedProduct.Category?.Name ?? string.Empty, selectedProduct.Name, userMessage);
            
            return new StyleRecommendationResponse
            {
                Message = fashionAdvice,
                RecommendedProducts = new List<Product>()
            };
        }


        
        private async Task<Product> GetProductById(int id)
        {
            return await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.ProductId == id) 
                ?? throw new Exception("Product not found");
        }
        
        private List<string> GetCompatibleCategories(string selectedCategory)
        {
            if (CategoryCompatibility.ContainsKey(selectedCategory))
            {
                return CategoryCompatibility[selectedCategory];
            }
            
            // Fallback: all categories except the selected one
            return CategoryCompatibility.Values
                .SelectMany(x => x)
                .Where(c => c != selectedCategory)
                .Distinct()
                .ToList();
        }
        
        private List<string> FilterToRequestedCategory(List<string> compatibleCategories, string requestedCategory)
        {
            var categoryUpper = requestedCategory.ToUpper();
            return compatibleCategories
                .Where(c => c.Contains(categoryUpper) || categoryUpper.Contains(c))
                .ToList();
        }
        
        private async Task<List<string>> GetAIColorSuggestions(string selectedColor)
        {
            var prompt = $@"
You are a color theory expert. List 6-8 colors that coordinate beautifully with {selectedColor} in fashion.
Use standard color names like: BLACK, WHITE, GRAY, NAVY, BLUE, RED, GREEN, BROWN, BEIGE, CREAM, GOLD, SILVER, etc.
Respond with ONLY a comma-separated list of colors.
Example: BLACK, WHITE, NAVY, BEIGE, CREAM
";

            try
            {
                var aiResponse = await CallOpenAI(prompt, maxTokens: 60, temperature: 0.3);
                
                return aiResponse
                    .Split(',')
                    .Select(c => c.Trim().ToUpper())
                    .Where(c => !string.IsNullOrEmpty(c) && c.Length > 1)
                    .ToList();
            }
            catch
            {
                // Fallback to basic neutrals if AI fails
                return new List<string> { "BLACK", "WHITE", "GRAY", "NAVY", "BEIGE" };
            }
        }
        
        private async Task<string> GetAIFashionAdvice(string color, string category, string productName, string? question = null)
        {
            var userQuestion = string.IsNullOrEmpty(question) ? "Give me complete fashion styling advice for this item" : question;
            
            var prompt = $@"
You are a professional fashion stylist . Give detailed fashion advice for a {color} {productName} from the {category} category.

User question: {userQuestion}

Provide comprehensive styling advice including:

JEWELRY & ACCESSORIES:
- What metal jewelry works best (gold/silver/rose gold) and why
- Specific jewelry pieces to consider (earrings, necklaces, bracelets)
- Bag and accessory recommendations

OCCASIONS & STYLING:
- Perfect occasions to wear this item (work, casual, evening, formal)
- How to style it for different settings
- Seasonal considerations

COLOR COORDINATION:
- What colors complement {color} beautifully
- Color combinations to avoid
- How to create different moods with color

STYLING TECHNIQUES:
- Layering tips and techniques
- How to dress it up or down
- Proportion and silhouette advice

Keep your response detailed but conversational, around 50-100 words. Be specific and actionable.
";

            try
            {
                return await CallOpenAI(prompt, maxTokens: 300, temperature: 0.7);
            }
            catch
            {
                return $@"Here's styling advice for your {color} {productName}:

JEWELRY: {color} pairs beautifully with {(color.ToUpper().Contains("WARM") || color.ToUpper().Contains("RED") || color.ToUpper().Contains("YELLOW") ? "gold" : "silver")} jewelry. Consider delicate pieces for daytime or statement jewelry for evening.

OCCASIONS: Perfect for both professional settings and casual outings. Dress it up with heels and structured accessories, or keep it casual with sneakers and relaxed pieces.

COLORS: Neutral colors like black, white, and gray always work well. Navy and beige are also excellent choices for a sophisticated look.

STYLING: Layer with complementary pieces and choose accessories that match the occasion you're dressing for.";
            }
        }
        
        // Helper methods for chat processing
        private bool IsGreeting(string message)
        {
            return message == "hello" || message == "hi" || message == "hey" || message.StartsWith("hello") || message.StartsWith("hi ");
        }
        
        private bool IsThankYou(string message)
        {
            return message.Contains("thank") || message.Contains("thanks");
        }
        
        private bool IsGoodbye(string message)
        {
            return message == "bye" || message == "goodbye" || message == "see you" || message.Contains("talk later");
        }
        
        private string ExtractCategoryRequest(string message)
        {
            var categoryKeywords = new Dictionary<string, string>
            {
                ["other bags"] = "bags",
                ["more bags"] = "bags", 
                ["different bags"] = "bags",
                ["other shoes"] = "shoes",
                ["more shoes"] = "shoes",
                ["different shoes"] = "shoes",
                ["other blazers"] = "blazers",
                ["more blazers"] = "blazers",
                ["other jackets"] = "jackets",
                ["more jackets"] = "jackets",
                ["other dresses"] = "dresses/jumpsuits",
                ["more dresses"] = "dresses/jumpsuits",
                ["other skirts"] = "skirts",
                ["more skirts"] = "skirts",
                ["other shirts"] = "shirts",
                ["more shirts"] = "shirts",
                ["other sweaters"] = "sweaters",
                ["more sweaters"] = "sweaters",
                ["show me bags"] = "bags",
                ["show me shoes"] = "shoes",
                ["show me blazers"] = "blazers",
                ["show me jackets"] = "jackets",
                ["show me dresses"] = "dresses/jumpsuits",
                ["show me skirts"] = "skirts",
                ["show me shirts"] = "shirts",
                ["show me sweaters"] = "sweaters"
            };
            
            foreach (var keyword in categoryKeywords)
            {
                if (message.Contains(keyword.Key))
                {
                    return keyword.Value;
                }
            }
            
            return "";
        }
        
        private async Task<List<Product>> FindMatchingProductsInDatabase(string selectedColor, List<string> aiColors, List<string> compatibleCategories, int excludeProductId)
        {
            // Combine selected color with AI-suggested colors
            var allColors = new List<string> { selectedColor };
            allColors.AddRange(aiColors);
            allColors = allColors.Distinct().ToList();
            
            return await _context.Products
                .Include(p => p.Category)
                .Where(p => p.ProductId != excludeProductId && 
                           p.Category != null && 
                           compatibleCategories.Contains(p.Category.Name.ToUpper()) &&
                           allColors.Contains(p.Color.ToUpper()))
                .ToListAsync();
        }
        
        private List<Product> RandomizeAndLimit(List<Product> products, int limit)
        {
            var random = new Random();
            return products
                .OrderBy(x => random.Next())
                .Take(limit)
                .ToList();
        }
        
        private async Task<string> CallOpenAI(string prompt, int maxTokens = 150, double temperature = 0.7)
        {
            var requestData = new
            {
                model = "gpt-3.5-turbo",
                messages = new[] { new { role = "user", content = prompt } },
                max_tokens = maxTokens,
                temperature = temperature
            };

            var content = new StringContent(
                JsonSerializer.Serialize(requestData),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(_apiUrl, content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var responseObject = JsonSerializer.Deserialize<OpenAIResponse>(responseContent);
                return responseObject?.choices?[0]?.message?.content?.Trim() ?? "I couldn't provide advice right now.";
            }

            throw new Exception($"OpenAI API error: {response.StatusCode}");
        }
        
        // METHOD 3: Test API connection (for Program.cs)
        public async Task<bool> TestApiConnection()
        {
            try
            {
                var testResponse = await CallOpenAI("Hello, this is a test.", maxTokens: 5, temperature: 0.5);
                return !string.IsNullOrEmpty(testResponse);
            }
            catch
            {
                return false;
            }
        }
    }
    
    // ================ RESPONSE CLASSES ================
    
    public class StyleRecommendationResponse
    {
        public string Message { get; set; } = string.Empty;
        public List<Product> RecommendedProducts { get; set; } = new List<Product>();
    }
    
    // OpenAI response classes
    public class OpenAIResponse
    {
        public Choice[]? choices { get; set; }
    }
    
    public class Choice
    {
        public Message? message { get; set; }
    }
    
    public class Message
    {
        public string? content { get; set; }
    }
}