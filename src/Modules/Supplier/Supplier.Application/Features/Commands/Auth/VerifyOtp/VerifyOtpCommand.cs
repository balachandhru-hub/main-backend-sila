using MediatR;

namespace Supplier.Application.Features.Auth.Commands.VerifyOtp
{
    public class VerifyOtpCommand : IRequest<VerifyOtpResponse>
    {
        public string Email { get; set; }
        public string Otp { get; set; }
    }
}