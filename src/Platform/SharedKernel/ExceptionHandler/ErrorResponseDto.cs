namespace Dto
{
    /// <summary>
    /// Standard error payload returned by <c>CustomExceptionMiddleware</c>.
    /// </summary>
    public class ErrorResponseDto
    {
        /// <summary>
        /// HTTP status code of the error response.
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Short human-readable error message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Longer description of what went wrong.
        /// </summary>
        public string Description { get; set; } = string.Empty;
    }
}
