namespace Identity.Application.Features.Auth.Commands.VerifyOtp
{
    public class VerifyOtpResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string? TemporaryVerificationToken { get; set; }
    }
}