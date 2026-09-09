using MediatR;
using Supplier.Domain.Dto;

namespace Supplier.Application.Features.Auth.Commands.SendEmailVerification
{
    public class SendEmailVerificationCommand : IRequest<SendEmailVerificationResponseDto>
    {
       public Guid OrganizationId { get; }
       public Guid UserId { get; }

        public SendEmailVerificationCommand(Guid organizationId, Guid userId)
        {
            OrganizationId = organizationId;
            UserId = userId;
        }

    }
}