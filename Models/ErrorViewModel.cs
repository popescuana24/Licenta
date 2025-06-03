namespace ClothingWebApp.Models
{
    public class ErrorViewModel
    {
        /// Identifier for the request that caused the error
        public string? RequestId { get; set; }
        
        /// Determines whether to show the request ID on the error page
        /// Only displays if RequestId is not null or empty
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}