using MediatR;
using Identity.Domain.Dto;

namespace Identity.Application.Features.Auth.Commands.SendEmailVerification
{
    public class SendEmailVerificationCommand : IRequest<SendEmailVerificationResponseDto>
    {
        public string Email { get; set; } 
        public string? IpAddress { get; set; }
    }
}