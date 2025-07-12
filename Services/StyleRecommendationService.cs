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

        private readonly List<string> _availableCategories = new List<string>
        {
            "BAGS", "BLAZERS", "DRESSES/JUMPSUITS", "JACKETS", "SHIRTS", "SHOES", "SWEATERS", "SKIRTS", "T-SHIRTS/TOPS"
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

            // Get categories that work with this product using AI
            var compatibleCategories = await GetAICompatibleCategories(selectedCategory, categoryFilter);

            // Check if no compatible categories found for the specific filter
            if (!string.IsNullOrEmpty(categoryFilter) && !compatibleCategories.Any())
            {
                return new StyleRecommendationResponse
                {
                    Message = $"Sorry, {categoryFilter} don't typically pair well with {selectedProduct.Category?.Name ?? "this item"}.", 
                    RecommendedProducts = new List<Product>()
                };
            }

            // Get AI color suggestions
            var aiColors = await GetAIColorSuggestions(selectedColor);

            // Find matching products in database
            var matchingProducts = await FindMatchingProductsInDatabase(selectedColor, aiColors, compatibleCategories, productId);

            // Randomize and limit results
            var finalProducts = RandomizeAndLimit(matchingProducts, 8);

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

            var fashionAdvice = await GetAIFashionAdvice(color, category, productName, question); 

            return new StyleRecommendationResponse
            {
                Message = fashionAdvice,
                RecommendedProducts = new List<Product>()
            };
        }

        // METHOD 3: Process chat messages 
        public async Task<StyleRecommendationResponse> ProcessChatMessage(int productId, string userMessage)
        {
            var selectedProduct = await GetProductById(productId);
            var lowerMessage = userMessage.ToLower().Trim();
            
            // Use AI to detect if user wants specific category recommendations
            var requestedCategory = await GetAICategoryRequest(userMessage, selectedProduct);
            if (!string.IsNullOrEmpty(requestedCategory))
            {
                return await GetMatchingProducts(productId, requestedCategory);
            }
            
            // Handle everything else through AI (greetings, fashion questions, non-fashion topics)
            var response = await GetAIResponse(selectedProduct.Color, selectedProduct.Category?.Name ?? string.Empty, selectedProduct.Name, userMessage);
            
            return new StyleRecommendationResponse
            {
                Message = response,
                RecommendedProducts = new List<Product>()
            };
        }

        // Helper method to get a product by ID, including its category
        private async Task<Product> GetProductById(int id)
        {
            return await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.ProductId == id)
                ?? throw new Exception("Product not found");
        }

        // Finds matching products in the database
        private async Task<List<Product>> FindMatchingProductsInDatabase(string selectedColor, List<string> aiColors, List<string> compatibleCategories, int excludeProductId)
        {
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

       /// AI INTEGRATION METHODS ///

        // Get compatible categories using AI (handles both general compatibility and specific category filtering)
        private async Task<List<string>> GetAICompatibleCategories(string selectedCategory, string? categoryFilter = null)
        {
             var prompt = $@"
        You are a fashion expert. Based on the following category compatibility rules, determine which categories are compatible with {selectedCategory}.

         Category Compatibility Rules:
            BAGS: Compatible with BLAZERS, DRESSES/JUMPSUITS, JACKETS, SHIRTS, SHOES, SWEATERS, SKIRTS, T-SHIRTS/TOPS
            BLAZERS: Compatible with BAGS, DRESSES/JUMPSUITS, SHIRTS, SHOES, SWEATERS, SKIRTS, T-SHIRTS/TOPS
            DRESSES/JUMPSUITS: Compatible with BAGS, JACKETS, SHOES, BLAZERS
            JACKETS: Compatible with BAGS, DRESSES/JUMPSUITS, SHIRTS, SHOES, SWEATERS, SKIRTS, T-SHIRTS/TOPS
            SHIRTS: Compatible with BAGS, BLAZERS, JACKETS, SHOES, SKIRTS
            SHOES: Compatible with BLAZERS, DRESSES/JUMPSUITS, JACKETS, SHIRTS, BAGS, SWEATERS, SKIRTS, T-SHIRTS/TOPS
            SWEATERS: Compatible with BAGS, BLAZERS, JACKETS, SHOES, SKIRTS
            SKIRTS: Compatible with BAGS, BLAZERS, JACKETS, SHIRTS, SHOES, SWEATERS, T-SHIRTS/TOPS
            T-SHIRTS/TOPS: Compatible with BAGS, BLAZERS, JACKETS, SHOES, SKIRTS

         Instructions:
            {(string.IsNullOrEmpty(categoryFilter) ? 
            $"- Return ALL categories that are listed as compatible with {selectedCategory}" :
            $"- Check if {categoryFilter.ToUpper()} is listed as compatible with {selectedCategory}. If YES, return ONLY {categoryFilter.ToUpper()}. If NO, return empty.")}
        - Format your response as a comma-separated list in UPPERCASE
        - Do NOT include {selectedCategory} itself in the response
        - If {selectedCategory} is not found in the rules, return an empty response

        Example response format: BAGS, SHOES, JACKETS, SHIRTS

        Respond with ONLY the compatible categories:
        ";

         try
        {
            var response = await CallOpenAI(prompt, maxTokens: 100, temperature: 0.1);
        
            // Clean and parse the response
            var categories = response
                .Split(',')
                .Select(c => c.Trim().ToUpper())
                .Where(c => !string.IsNullOrEmpty(c) && _availableCategories.Contains(c))
                .ToList();
        
            // If we have a specific category filter, ensure we only return that category if it's compatible
            if (!string.IsNullOrEmpty(categoryFilter))
            {
                var filterUpper = categoryFilter.ToUpper();
                return categories.Contains(filterUpper) ? new List<string> { filterUpper } : new List<string>();
            }
        
                return categories;
            }
                catch
        {
            // Fallback logic
            if (!string.IsNullOrEmpty(categoryFilter))
            {
                // If a specific category filter was requested but failed, return empty
                if (_availableCategories.Contains(categoryFilter.ToUpper()))
                    return new List<string> { categoryFilter.ToUpper() };
                
                // If the filter is not valid, return empty
            return new List<string>(); // Return empty for specific filter on error
        }
            return _availableCategories
            .Where(c => c != selectedCategory)
            .ToList();
            }
    }


        // AI category request detection
        private async Task<string> GetAICategoryRequest(string userMessage, Product selectedProduct)
        {
            var availableCategoriesString = string.Join(", ", _availableCategories);
            
            var prompt = $@"
            You are analyzing a user's message to detect if they want to see specific clothing category recommendations.

            User is currently viewing: {selectedProduct.Color} {selectedProduct.Name} from {selectedProduct.Category?.Name} category.

            Available categories in the system: {availableCategoriesString}

            User message: ""{userMessage}""

            Instructions:
            - If the user is asking for a specific category of items (like ""show me bags"", ""other shoes"", ""more dresses"", ""different jackets""), respond with ONLY the category name in UPPERCASE.
            - If the user is NOT asking for a specific category, respond with ""NONE""
            - Match variations like: ""other bags"", ""show me shoes"", ""more blazers"", ""different dresses"", ""what shoes"", ""any jackets""
            - Be flexible with plurals and variations

            Examples:
            - ""show me other bags"" → BAGS
            - ""what shoes would work?"" → SHOES
            - ""more dresses please"" → DRESSES/JUMPSUITS
            - ""hello"" → NONE
            - ""what colors match?"" → NONE

            Respond with ONLY the category name or NONE:
            ";

            try
            {
                var response = await CallOpenAI(prompt, maxTokens: 20, temperature: 0.1);
                var category = response.Trim().ToUpper();
                
                // Validate that the response is a valid category
                if (_availableCategories.Contains(category))
                {
                    return category.ToLower(); // Return lowercase for consistency with existing code
                }
                
                return "";
            }
            catch
            {
                return "";
            }
        }

        // Get AI color suggestions
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
                return new List<string> { "BLACK", "WHITE", "GRAY", "NAVY", "BEIGE" };
            }
        }

        // General AI response for chat messages
        private async Task<string> GetAIResponse(string color, string category, string productName, string userMessage)
        {
            var prompt = $@"
            You are a friendly professional fashion stylist and style assistant. You are helping with a {color} {productName} from the {category} category.

            IMPORTANT INSTRUCTIONS:
            - Always be warm, friendly, and conversational
            - Respond to greetings naturally (""Hello! I'm your style assistant..."")
            - Handle thank you messages gracefully
            - For goodbye messages, be warm and inviting
            - If the user asks about topics unrelated to fashion, style, clothing, or accessories, politely redirect them by saying: ""I'm a fashion and style assistant, so I specialize in clothing, accessories, and styling advice. Is there anything about fashion or styling I can help you with regarding your {color} {productName}?""
            - For fashion-related questions, provide helpful styling advice
            - Keep responses conversational and around 30-80 words
            - Always stay in character as a fashion expert

            User message: ""{userMessage}""

            Respond appropriately based on the message context.
            ";

            try
            {
                return await CallOpenAI(prompt, maxTokens: 200, temperature: 0.7);
            }
            catch
            {
                return "Hi! I'm your style assistant and I'm here to help you with fashion and styling advice. Is there anything about your outfit or styling that I can help you with?";
            }
        }

        // Provides detailed fashion advice for a specific product
        private async Task<string> GetAIFashionAdvice(string color, string category, string productName, string? question = null)
        {
            var userQuestion = string.IsNullOrEmpty(question) ? "Give me complete fashion styling advice for this item" : question;
            
            var prompt = $@"
            You are a professional fashion stylist and style assistant. Be friendly and conversational. Give detailed fashion advice for a {color} {productName} from the {category} category.

            User question: {userQuestion}
            
            Provide styling advice including:

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

        // Calls the OpenAI API to get a response based on the provided prompt
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

        // UTILITY METHODS
        
        private List<Product> RandomizeAndLimit(List<Product> products, int limit)
        {
            var random = new Random();
            return products
                .OrderBy(x => random.Next())
                .Take(limit)
                .ToList();
        }
        
        public async Task<bool> TestApiConnection()
        {
            try
            {
                var testResponse = await CallOpenAI("Hello, this is a test", maxTokens: 5, temperature: 0.5);
                return !string.IsNullOrEmpty(testResponse);
            }
            catch
            {
                return false;
            }
        }
    }
    
    // Models for the response from the style recommendation service
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