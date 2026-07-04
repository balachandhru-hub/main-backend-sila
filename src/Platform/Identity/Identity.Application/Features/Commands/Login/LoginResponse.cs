namespace Identity.Application.Features.Commands.Login
{
    public class LoginResponse
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        // This will be stored in HttpOnly Cookie by the Controller
        public string? AccessToken { get; set; }
    }
}