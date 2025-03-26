namespace ClothingWebApp.Models
{
    /// <summary>
    /// View model for displaying error information in the Error view
    /// </summary>
    public class ErrorViewModel
    {
        /// <summary>
        /// Identifier for the request that caused the error
        /// </summary>
        public string? RequestId { get; set; }
        
        /// <summary>
        /// Determines whether to show the request ID on the error page
        /// Only displays if RequestId is not null or empty
        /// </summary>
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}