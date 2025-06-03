using ClothingWebApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClothingWebApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StyleAssistantController : ControllerBase
    {
        private readonly StyleRecommendationService _styleService;
        
        public StyleAssistantController(StyleRecommendationService styleService)
        {
            _styleService = styleService;
        }
        
        // ENDPOINT 1: show matching items
        [HttpPost("recommendations")]
        public async Task<IActionResult> GetRecommendations([FromBody] RecommendationRequest request)
        {
            try
            {
                var result = await _styleService.GetMatchingProducts(request.ProductId, request.CategoryFilter);
                
                return Ok(new
                {
                    Success = true,
                    Message = result.Message,
                    RecommendedProducts = result.RecommendedProducts.Select(p => new {
                        ProductId = p.ProductId,
                        Name = p.Name,
                        Color = p.Color,
                        Price = p.Price,
                        ImageUrl = p.ImageUrl,
                        CategoryName = p.Category?.Name
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = "Sorry, I encountered an error while finding matching items: " + ex.Message,
                    RecommendedProducts = new object[0]
                });
            }
        }
        
        // ENDPOINT 2: Chat (for "Style tips" button and general chat)
        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            try
            {
                StyleRecommendationResponse result;
                
                // Check if this is a request for fashion tips (from the button)
                if (string.IsNullOrEmpty(request.UserMessage) || 
                    request.UserMessage.Contains("style tips") || 
                    request.UserMessage.Contains("fashion tips"))
                {
                    // Use fashion advice method (NO products)
                    result = await _styleService.GetFashionAdvice(request.ProductId, request.UserMessage);
                }
                else
                {
                    // Use chat processing (handles greetings, category requests, etc.)
                    result = await _styleService.ProcessChatMessage(request.ProductId, request.UserMessage);
                }
                
                return Ok(new
                {
                    Success = true,
                    Message = result.Message,
                    RecommendedProducts = result.RecommendedProducts.Select(p => new {
                        ProductId = p.ProductId,
                        Name = p.Name,
                        Color = p.Color,
                        Price = p.Price,
                        ImageUrl = p.ImageUrl,
                        CategoryName = p.Category?.Name
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
    
    // REQUEST MODELS that match your frontend JavaScript
    public class RecommendationRequest
    {
        public int ProductId { get; set; }
        public string? CategoryFilter { get; set; } = null;
    }
    
    public class ChatRequest
    {
        public int ProductId { get; set; }
        public string UserMessage { get; set; } = string.Empty;
    }
}