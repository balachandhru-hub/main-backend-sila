namespace Identity.Domain.Dto
{
    public class SendEmailVerificationResponseDto
{
    public bool Success { get; set; }

    public string Otp { get; set; } 

    public int ValidityMinutes { get; set; }
}
}