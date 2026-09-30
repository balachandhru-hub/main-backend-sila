using MediatR;
using Identity.Domain.Enum;

namespace Identity.Application.Features.Commands.Register
{
    public class RegisterCommand : IRequest<Guid>
    {
        public string OrganizationName { get; set; }

        public OrganizationType OrganizationType { get; set; }

        public string Email { get; set; }

        public string Phone { get; set; }

        public string Country { get; set; }

        public string AddressLine1 { get; set; }

        public string? AddressLine2 { get; set; }

        public string City { get; set; }

        public string State { get; set; }

        public string PinCode { get; set; }

        // Token received after OTP verification
        public string? VerificationToken { get; set; }
        public string PersonName { get; set; }
        public string PersonEmail { get; set; }
    
        // User
        public string UserName { get; set; }
        public string Password { get; set; }
    }
}