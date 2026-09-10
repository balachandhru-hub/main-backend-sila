using Buyer.Domain.Dto;
using MediatR;

namespace Buyer.Application.Features.Queries.Message
{
    public class GetMessageAttachmentQuery : IRequest<MessageAttachmentFileDto>
    {
        public Guid AttachmentId { get; set; }
        public Guid OrganizationId { get; set; }
        public string OrganizationType { get; set; }
    }
}
