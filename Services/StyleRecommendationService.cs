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
        // dictionary that defines which categories are compatible with each other
        // This is used to filter products based on category compatibility
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
            //HTTP request to an external API
            _httpClient = new HttpClient();
            //If apiKey is null, throw an ArgumentNullException better for debugging
            _apiKey = apiKey ?? throw new ArgumentNullException("API key is required"); // Ensure API key is provided
            //automatically include this Authorization header in every HTTP request it makes
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}"); // Set the API key in the request headers
        }


        // METHOD 1: Get matching products for "Show Matching Items" button
        //category filter fi the user wants to see products from a specific category
        //? is used to indicate that the categoryFilter parameter is optional
        // If not provided, recommendations will not be filtered by category
        public async Task<StyleRecommendationResponse> GetMatchingProducts(int productId, string? categoryFilter = null)
        {
            // Get the selected product by ID
            var selectedProduct = await GetProductById(productId);
            // If the product has no category or color, return an empty response
            var selectedCategory = selectedProduct.Category?.Name?.ToUpper() ?? "";
            var selectedColor = selectedProduct.Color?.ToUpper() ?? "";

            // Get categories that work with this product
            var compatibleCategories = GetCompatibleCategories(selectedCategory);

            // Filter to specific category if requested
            if (!string.IsNullOrEmpty(categoryFilter))
            {
                compatibleCategories = FilterToRequestedCategory(compatibleCategories, categoryFilter);
                // If no compatible categories remain after filtering, return a message indicating no matches
                if (!compatibleCategories.Any())
                {
                    return new StyleRecommendationResponse
                    {
                        Message = $"Sorry, {categoryFilter} don't typically pair well with {selectedProduct.Category?.Name ?? "this item"}.", 
                        RecommendedProducts = new List<Product>()
                    };
                }
            }

            // Get AI color suggestions FRom the method
            // This method generates a list of colors that coordinate well with the selected color using AI
            var aiColors = await GetAIColorSuggestions(selectedColor);

            // Find matching products in database
            var matchingProducts = await FindMatchingProductsInDatabase(selectedColor, aiColors, compatibleCategories, productId);

            // Randomize and limit results
            var finalProducts = RandomizeAndLimit(matchingProducts, 8);

            // If no matching products are found, return a message indicating no matches
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
        //? means that the question parameter is optional
        public async Task<StyleRecommendationResponse> GetFashionAdvice(int productId, string? question = null)
        {
            var selectedProduct = await GetProductById(productId);
            var color = selectedProduct.Color ?? "";  // Get the color of the selected product ?? // If null, use an empty string
            // If the product has no category, use an empty string
            var category = selectedProduct.Category?.Name ?? ""; // ? // If null, use an empty string
            // If the product has no name, use an empty string
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
            var lowerMessage = userMessage.ToLower().Trim(); // Normalize the message to lowercase for easier matching 
            
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

            // Handle specific category requests (for example when the client says "other bags", "show me shoes")
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

         // Helper method to get a product by ID, including its category
        // This method retrieves a product from the database by its ID, including its related category
        private async Task<Product> GetProductById(int id)
        {
            return await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.ProductId == id) // Find the product by ID, including its category
                ?? throw new Exception("Product not found"); // If no product is found, throw an exception
        }

        // Finds matching products in the database based on selected color, AI colors, and compatible categories
        private async Task<List<Product>> FindMatchingProductsInDatabase(string selectedColor, List<string> aiColors, List<string> compatibleCategories, int excludeProductId)
        {
            // Combine selected color with AI-suggested colors
            var allColors = new List<string> { selectedColor }; // Start with the selected color
            allColors.AddRange(aiColors); // Add AI-suggested colors
            // Normalize colors to uppercase for case-insensitive comparison
            allColors = allColors.Distinct().ToList();
            //data query to find products that match the selected color, AI colors, and compatible categories
            return await _context.Products
                .Include(p => p.Category)
                .Where(p => p.ProductId != excludeProductId && // Exclude the product being viewed
                           p.Category != null && // Ensure the product has a category
                           compatibleCategories.Contains(p.Category.Name.ToUpper()) && // Check if the product's category is compatible
                           allColors.Contains(p.Color.ToUpper())) // Check if the product's color matches any of the selected or AI-suggested colors
                .ToListAsync(); // Execute the query and return the list of matching products
        }

     
        /// CATEGORY LOGIC ///
        
        
        //dictionary CategoryCompatibility maps a category to a list of categories that go well 
        private List<string> GetCompatibleCategories(string selectedCategory)
        {
            if (CategoryCompatibility.ContainsKey(selectedCategory))
            {
                //If the selectedCategory is found in the dictionary as a key, then return the list of compatible categories
                return CategoryCompatibility[selectedCategory]; // If found, return the compatible categories
            }

            // in case the selectedCategory is not found in the dictionary, return all categories except the selected one
            return CategoryCompatibility.Values
                .SelectMany(x => x) //flatten the list of lists into a single list
                .Where(c => c != selectedCategory) //different from the selected category
                .Distinct() //remove duplicates
                .ToList();
        }

        // Filter compatible categories based on user request
        // This method checks if the requested category is compatible with the selected product's categories
        //list of category strings that are compatible with the selected product's category
        private List<string> FilterToRequestedCategory(List<string> compatibleCategories, string requestedCategory)
        {
            //transform the requested category to uppercase for case-insensitive comparison
            var categoryUpper = requestedCategory.ToUpper();
            return compatibleCategories
                .Where(c => c.Contains(categoryUpper) || categoryUpper.Contains(c)) // Check if the requested category is part of the compatible categories
                .ToList();
        }

        /// AI INTEGRATION METHODS ///

        //Get AI color suggestions
        // This method generates a list of colors that coordinate well with the selected color using AI
        private async Task<List<string>> GetAIColorSuggestions(string selectedColor)
        {
            //this is the prompt sent to the AI model to generate color suggestions
            // It asks the AI to provide a list of colors that go well with the selected color
            var prompt = $@" 
            You are a color theory expert. List 6-8 colors that coordinate beautifully with {selectedColor} in fashion.
            Use standard color names like: BLACK, WHITE, GRAY, NAVY, BLUE, RED, GREEN, BROWN, BEIGE, CREAM, GOLD, SILVER, etc.
            Respond with ONLY a comma-separated list of colors.
            Example: BLACK, WHITE, NAVY, BEIGE, CREAM
";

            try
            {
                //calls the OpenAI API with the prompt to get color suggestions
                // The maxTokens and temperature parameters control the response length 
                var aiResponse = await CallOpenAI(prompt, maxTokens: 60, temperature: 0.3);
                // Process the AI response to extract color suggestions
                return aiResponse
                    .Split(',')
                    .Select(c => c.Trim().ToUpper()) // Normalize colors to uppercase and trim whitespace
                    .Where(c => !string.IsNullOrEmpty(c) && c.Length > 1) // Filter out empty or very short responses
                    .ToList();
            }
            catch
            {
                // Fallback to basic neutrals inCASE AI fails
                return new List<string> { "BLACK", "WHITE", "GRAY", "NAVY", "BEIGE" };
            }
        }

        //provides detailed fashion advice for a specific product
        private async Task<string> GetAIFashionAdvice(string color, string category, string productName, string? question = null)
        {
            // If the question is null or empty, provide a default question
            var userQuestion = string.IsNullOrEmpty(question) ? "Give me complete fashion styling advice for this item" : question;
            
            var prompt = $@"
            You are a professional fashion stylist . Give detailed fashion advice for a {color} {productName} from the {category} category.

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
            //in case of an error, return a fallback message with basic styling advice
            // This fallback message provides general styling advice for the product
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

        //calls the OpenAI API to get a response based on the provided prompt
        private async Task<string> CallOpenAI(string prompt, int maxTokens = 150, double temperature = 0.7)
        {
            var requestData = new
            {
                model = "gpt-3.5-turbo",
                messages = new[] { new { role = "user", content = prompt } }, // The message format for the OpenAI API
                max_tokens = maxTokens, // Maximum number of tokens in the response
                temperature = temperature
            };

            var content = new StringContent(
                JsonSerializer.Serialize(requestData), // Serialize the request data to JSON
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(_apiUrl, content); // Send the POST request to the OpenAI API

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(); // Read the response content as a string
                var responseObject = JsonSerializer.Deserialize<OpenAIResponse>(responseContent); // Deserialize the response into an OpenAIResponse object
                return responseObject?.choices?[0]?.message?.content?.Trim() ?? "I couldn't provide advice right now."; // Return the content of the first choice, or a fallback message if null
            }

            throw new Exception($"OpenAI API error: {response.StatusCode}");
        }
        
        //CHAT PROCESSING HELPER METHODS

        // Extracts the requested category from the user's message
        // This method checks the user's message for specific keywords that indicate a request for a category
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
        
        

        //methods for chat processing
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

        //UTILITY METHODS
        
        // Randomizes the list of products and limits the result to a specified number
        // This method shuffles the products randomly and returns a limited number of them
        private List<Product> RandomizeAndLimit(List<Product> products, int limit)
        {
            var random = new Random();
            return products
                .OrderBy(x => random.Next())
                .Take(limit)
                .ToList();
        }
        
        
        
        //Test API connection (ca sa fiu sigura ca OpenAI API is working)
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