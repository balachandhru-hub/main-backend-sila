using MediatR;

namespace Identity.Application.Features.Auth.Commands.SendEmailVerification
{
    public class SendEmailVerificationCommand : IRequest<bool>
    {
        public string Email { get; set; } 
        public string? IpAddress { get; set; }
    }
}