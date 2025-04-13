// Services/StyleRecommendationService.cs
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using ClothingWebApp.Data;
using ClothingWebApp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ClothingWebApp.Services
{
    public class StyleRecommendationService
    {
        private readonly ApplicationDbContext _context;
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _apiUrl = "https://api.openai.com/v1/chat/completions";
        
        public StyleRecommendationService(ApplicationDbContext context, string apiKey)
        {
            _context = context;
            _httpClient = new HttpClient();
            _apiKey = apiKey ?? throw new ArgumentNullException("API key is required");
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        }
        
        public async Task<Product> GetProductById(int id)
        {
            return await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.ProductId == id) ?? throw new Exception("Product not found");
        }
        
        public async Task<StyleRecommendationResponse> GetColorMatchingProducts(int selectedProductId)
        {
            // Get the selected product
            var selectedProduct = await GetProductById(selectedProductId);
            
            // Get all available products for potential matching
            var availableProducts = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.ProductId != selectedProductId) // Exclude the selected product
                .ToListAsync();
            
            Console.WriteLine($"Found {availableProducts.Count} potential matching products");
            
            // Create the inventory summary for the prompt
            var inventorySummary = FormatInventoryForPrompt(availableProducts);
            
            // Send request to OpenAI API
            var response = await GetGptRecommendations(selectedProduct, inventorySummary);
            
            // Parse the response and return matching products
            return ParseProductRecommendations(response, availableProducts);
        }
        
        private string FormatInventoryForPrompt(List<Product> products)
        {
            var summary = new StringBuilder();
            // Limit to 100 products to avoid token limits
            var limitedProducts = products.Take(100);
            
            foreach (var product in limitedProducts)
            {
                summary.AppendLine($"ID: {product.ProductId}, Name: {product.Name}, Category: {product.Category?.Name ?? "Unknown"}, Color: {product.Color}");
            }
            return summary.ToString();
        }
        
        private async Task<string> GetGptRecommendations(Product selectedProduct, string inventorySummary)
{
    Console.WriteLine("Preparing OpenAI recommendation request");
    
    var messages = new[]
    {
        new { role = "system", content = @"
You are a fashion styling assistant. 
Your task is to recommend products that would pair well with a selected item, primarily using color coordination principles.
You should provide specific product IDs from the available inventory.
DO NOT explicitly mention color theory in your response - just present good matches in a friendly, conversational way.
Format your response with ONLY category headings and product IDs. 
DO NOT include explanations for each product.
Focus on recommending complementary categories. For example:
- If the user selected a top/shirt/blouse, recommend bottoms (skirts, pants), accessories, and outerwear
- If the user selected bottoms (skirts, pants), recommend tops, accessories, and shoes
- If the user selected a dress, recommend accessories, shoes, and outerwear
- If the user selected shoes, recommend clothing and accessories that match
- If the user selected a bag, recommend clothing and shoes that coordinate with it

Format your response like this EXACTLY:
Here are some recommendations for your product:

Bottoms:
- ID: 123
- ID: 456

Tops:
- ID: 789
- ID: 012

Do not include any explanations about why items match, just list the categories and IDs.
" },
        new { role = "user", content = $@"
Selected product: {selectedProduct.Name} (ID: {selectedProduct.ProductId})
Color: {selectedProduct.Color}
Category: {selectedProduct.Category?.Name ?? "Unknown"}

Available inventory:
{inventorySummary}

Recommend specific product IDs from the inventory that would pair well with the selected {selectedProduct.Name}. 
Use color coordination principles in your selection.
Group recommendations by category (skirts, pants, accessories, etc.).
Prioritize recommending items from complementary categories, not the same category as the selected item.
Don't include explanations about why items match.
Just list the categories and product IDs.
" }
    };
    
    var requestData = new
    {
        model = "gpt-3.5-turbo",
        messages = messages,
        temperature = 0.7,
        max_tokens = 1000
    };
    
    Console.WriteLine("Sending OpenAI API request");
    
    var content = new StringContent(
        JsonSerializer.Serialize(requestData),
        Encoding.UTF8,
        "application/json");
        
    var response = await _httpClient.PostAsync(_apiUrl, content);
    
    if (!response.IsSuccessStatusCode)
    {
        var errorContent = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"OpenAI API error: {response.StatusCode}, {errorContent}");
        throw new Exception($"OpenAI API error: {response.StatusCode}, {errorContent}");
    }
    
    var responseContent = await response.Content.ReadAsStringAsync();
    var responseObject = JsonSerializer.Deserialize<OpenAIResponse>(responseContent);
    
    Console.WriteLine("Successfully received OpenAI response");
    return responseObject?.choices?[0]?.message?.content ?? "No recommendations found.";
}
        
        private StyleRecommendationResponse ParseProductRecommendations(string gptResponse, List<Product> availableProducts)
        {
            var response = new StyleRecommendationResponse
            {
                Message = gptResponse,
                RecommendedProducts = new List<Product>()
            };
            
            // Extract product IDs using regular expressions
            var regex = new Regex(@"ID: (\d+)");
            var matches = regex.Matches(gptResponse);
            
            foreach (Match match in matches)
            {
                if (int.TryParse(match.Groups[1].Value, out int productId))
                {
                    var product = availableProducts.FirstOrDefault(p => p.ProductId == productId);
                    if (product != null && !response.RecommendedProducts.Any(p => p.ProductId == productId))
                    {
                        response.RecommendedProducts.Add(product);
                    }
                }
            }
            
            return response;
        }
        
        // Helper method to process chat messages
        // Helper method to process chat messages
public async Task<StyleRecommendationResponse> ProcessChatMessage(int productId, string userMessage)
{
    // Get the selected product
    var selectedProduct = await GetProductById(productId);
    
    // Get all available products for potential matching
    var availableProducts = await _context.Products
        .Include(p => p.Category)
        .Where(p => p.ProductId != productId) // Exclude the selected product
        .ToListAsync();
        
    // Create the inventory summary for the prompt
    var inventorySummary = FormatInventoryForPrompt(availableProducts);
    
    var messages = new[]
    {
        new { role = "system", content = @"
You are a fashion styling assistant. 
Your task is to help users find products that pair well with their selected item.
Behind the scenes, use color coordination principles, but don't explicitly mention color theory unless the user asks about it.
Provide specific product IDs from the available inventory in a friendly, conversational tone.
When recommending products, focus on complementary categories that would create a complete outfit.

When recommending products, format your response like this:
[Your conversational response to the user]

Product recommendations:

Category Name:
- ID: 123
- ID: 456

Another Category:
- ID: 789
- ID: 012
" },
        new { role = "user", content = $@"
Selected product: {selectedProduct.Name} (ID: {selectedProduct.ProductId})
Color: {selectedProduct.Color}
Category: {selectedProduct.Category?.Name ?? "Unknown"}

Available inventory:
{inventorySummary}

User message: {userMessage}

Respond to the user's message and recommend products that would pair well with their {selectedProduct.Name}.
Include product IDs in your response.
Be natural and conversational in your reply.
" }
    };
    
    // Rest of the method remains the same...

            
            var requestData = new
            {
                model = "gpt-3.5-turbo",
                messages = messages,
                temperature = 0.7,
                max_tokens = 1000
            };
            
            var content = new StringContent(
                JsonSerializer.Serialize(requestData),
                Encoding.UTF8,
                "application/json");
                
            var response = await _httpClient.PostAsync(_apiUrl, content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"OpenAI API error in chat: {response.StatusCode}, {errorContent}");
                throw new Exception($"OpenAI API error: {response.StatusCode}, {errorContent}");
            }
            
            var responseContent = await response.Content.ReadAsStringAsync();
            var responseObject = JsonSerializer.Deserialize<OpenAIResponse>(responseContent);
            
            var gptResponse = responseObject?.choices?[0]?.message?.content ?? "No recommendations found.";
            
            return ParseProductRecommendations(gptResponse, availableProducts);
        }
        
        public async Task<bool> TestApiConnection()
        {
            try
            {
                var messages = new[]
                {
                    new { role = "system", content = "You are a helpful assistant." },
                    new { role = "user", content = "Hello, this is a test." }
                };
                
                var requestData = new
                {
                    model = "gpt-3.5-turbo",
                    messages = messages,
                    max_tokens = 5
                };
                
                var content = new StringContent(
                    JsonSerializer.Serialize(requestData),
                    Encoding.UTF8,
                    "application/json");
                
                Console.WriteLine("Testing OpenAI API connection...");
                var response = await _httpClient.PostAsync(_apiUrl, content);
                
                var success = response.IsSuccessStatusCode;
                Console.WriteLine($"OpenAI API test result: {(success ? "Success" : "Failed")}");
                
                if (!success)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error details: {response.StatusCode} - {errorContent}");
                }
                
                return success;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error testing API connection: {ex.Message}");
                return false;
            }
        }
    }
    
    // Response classes
    public class StyleRecommendationResponse
    {
        public string Message { get; set; } = string.Empty;
        public List<Product> RecommendedProducts { get; set; } = new List<Product>();
    }
    
    // OpenAI API response classes
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
        public string? role { get; set; }
        public string? content { get; set; }
    }
}