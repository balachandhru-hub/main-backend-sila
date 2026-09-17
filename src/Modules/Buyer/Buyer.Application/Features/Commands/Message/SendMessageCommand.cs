using Buyer.Domain.Dto;
using MediatR;

namespace Buyer.Application.Features.Commands.CreateMessage
{
    public class SendMessageCommand : IRequest<MessageResponseDto>
    {
        public Guid OrganizationId { get; }
        public string OrganizationType { get; }
        public Guid UserId { get; }
        public SendMessageDto Message { get; }

        /// <summary>
        /// Set only when the caller is an ExternalSupplier authenticated via a
        /// session token (no JWT, so no OrganizationId/OrganizationType/UserId).
        /// </summary>
        public Guid? ExternalSupplierCallerId { get; }

        public SendMessageCommand(
            Guid organizationId,
            string organizationType,
            Guid userId,
            SendMessageDto message)
        {
            OrganizationId = organizationId;
            OrganizationType = organizationType;
            UserId = userId;
            Message = message;
        }

        public SendMessageCommand(Guid externalSupplierCallerId, SendMessageDto message)
        {
            ExternalSupplierCallerId = externalSupplierCallerId;
            Message = message;
        }
    }
}
