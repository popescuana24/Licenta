// Controllers/StyleAssistantController.cs
using ClothingWebApp.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
using System.Text.Json;

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
        
        [HttpPost("recommendations")]
        public async Task<IActionResult> GetRecommendations([FromBody] ProductRecommendationRequest request)
        {
            try
            {
                var result = await _styleService.GetColorMatchingProducts(request.ProductId);
                
                return Ok(new
                {
                    Success = true,
                    Message = result.Message,
                    RecommendedProducts = result.RecommendedProducts.Select(p => new {
                        p.ProductId,
                        p.Name,
                        p.Color,
                        p.Price,
                        p.ImageUrl,
                        CategoryName = p.Category?.Name
                    })
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = "Error getting recommendations: " + ex.Message
                });
            }
        }
        
        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            try
            {
                var result = await _styleService.ProcessChatMessage(request.ProductId, request.UserMessage);
                
                return Ok(new
                {
                    Success = true,
                    Message = result.Message,
                    RecommendedProducts = result.RecommendedProducts.Select(p => new {
                        p.ProductId,
                        p.Name,
                        p.Color,
                        p.Price,
                        p.ImageUrl,
                        CategoryName = p.Category?.Name
                    })
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = "Error processing chat message: " + ex.Message
                });
            }
        }
    }
    
    public class ProductRecommendationRequest
    {
        public int ProductId { get; set; }
    }
    
    public class ChatRequest
    {
        public int ProductId { get; set; }
        public string UserMessage { get; set; } = string.Empty;
    }
}