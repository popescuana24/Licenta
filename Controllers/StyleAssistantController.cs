using ClothingWebApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClothingWebApp.Controllers
{
    //Base route is api/StyleAssistant
    [Route("api/[controller]")]
    [ApiController]
    public class StyleAssistantController : ControllerBase
    {
        //the logic of style recommendations and chat
        private readonly StyleRecommendationService _styleService;
        
        public StyleAssistantController(StyleRecommendationService styleService)
        {
            _styleService = styleService;
        }

        // ENDPOINT 1: show matching items
        // This endpoint is used to get product recommendations based on a specific product ID and optional category filter
        // The endpoint is accessed via POST request to api/StyleAssistant/recommendations
        [HttpPost("recommendations")]
        //The request body is deserialized into a RecommendationRequest object
        public async Task<IActionResult> GetRecommendations([FromBody] RecommendationRequest request)
        {
            try
            {
                //Calls an asynchronous service method GetMatchingProducts passing the product ID and category filter from the request
                var result = await _styleService.GetMatchingProducts(request.ProductId, request.CategoryFilter);

                return Ok(new
                {
                    Success = true,
                    Message = result.Message, // Message from the service about the recommendations
                    // Maps the recommended products to an anonymous object with the required properties
                    RecommendedProducts = result.RecommendedProducts.Select(p => new
                    {
                        ProductId = p.ProductId,
                        Name = p.Name,
                        Color = p.Color,
                        Price = p.Price,
                        ImageUrl = p.ImageUrl,
                        CategoryName = p.Category?.Name
                    }).ToList() // Converts each product to an anonymous object with the required properties
                });
            }
            catch (Exception ex)
            {
                // If an error occurs, return a BadRequest response with the error message
                return BadRequest(new
                {
                    Success = false,
                    Message = "Sorry, I encountered an error while finding matching items: " + ex.Message,
                    RecommendedProducts = new object[0] // Empty list of products in case of error
                });
            }
        }
        
        // ENDPOINT 2: Chat (for "Style tips" button and general tips )
        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            try
            {
                // Validate the request
                StyleRecommendationResponse result;
                
                //Check if this is a request for fashion tips (from the button)
                if (string.IsNullOrEmpty(request.UserMessage) || 
                    request.UserMessage.Contains("style tips") ||  // Check for "style tips" in the message
                    request.UserMessage.Contains("fashion tips")) // Check for "fashion tips" in the message
                {
                    // Use fashion advice method (NO products)
                    result = await _styleService.GetFashionAdvice(request.ProductId, request.UserMessage); // Pass the product ID and user message to get fashion advice
                }
                else
                {
                    // Use chat processing (handles greetings, category requests)
                    result = await _styleService.ProcessChatMessage(request.ProductId, request.UserMessage);
                }
                
                return Ok(new // Return a successful response with the result
                {
                    Success = true, // Indicates the request was successful
                    Message = result.Message, // Message from the service about the chat response
                    RecommendedProducts = result.RecommendedProducts.Select(p => new {
                        ProductId = p.ProductId,
                        Name = p.Name,
                        Color = p.Color,
                        Price = p.Price,
                        ImageUrl = p.ImageUrl,
                        CategoryName = p.Category?.Name //? null-conditional operator to avoid null reference exceptions
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = "Sorry, I encountered an error while processing your request: " + ex.Message,
                    RecommendedProducts = new object[0]
                });
            }
        }
    }

    // Models for requests
    // These classes represent the structure of the requests sent to the API endpoints
    public class RecommendationRequest
    {
        public int ProductId { get; set; }
        public string? CategoryFilter { get; set; } = null; // Optional category filter for recommendations 
        // If null, recommendations will not be filtered by category
    }
    
    public class ChatRequest
    {
        public int ProductId { get; set; } // ID of the product related to the chat request
        public string UserMessage { get; set; } = string.Empty; // User's message to the style assistant
    }
}
