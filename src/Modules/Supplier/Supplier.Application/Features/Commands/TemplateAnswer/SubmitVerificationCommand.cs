using MediatR;
using Supplier.Domain.Dto;

namespace Supplier.Application.Features.Commands.SubmitVerification
{
    public class SubmitVerificationCommand : IRequest<bool>
    {
        public SubmitVerificationDto Verification { get; set; }

        public SubmitVerificationCommand(SubmitVerificationDto verification)
        {
            Verification = verification;
        }
    }
}