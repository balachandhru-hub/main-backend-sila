using MediatR;

namespace Supplier.Application.Features.Auth.Commands.VerifyOtp
{
    public class VerifyOtpCommand : IRequest<VerifyOtpResponse>
    {
        public Guid UserId { get; set; }
        public string Otp { get; set; }
    }
}