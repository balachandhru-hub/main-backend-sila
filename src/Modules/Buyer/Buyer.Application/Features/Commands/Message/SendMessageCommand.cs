using Buyer.Domain.Dto;
using MediatR;

namespace Buyer.Application.Features.Commands.Message
{
    public class SendMessageCommand : IRequest<MessageResponseDto>
    {
        public Guid OrganizationId { get; }
        public string OrganizationType { get; }
        public Guid UserId { get; }
        public SendMessageDto Message { get; }

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
    }
}
