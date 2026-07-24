using MediatR;
using Buyer.Domain.Dto;

namespace Buyer.Application.Features.Queries.GetRFQAttachments
{
    public class GetRFQAttachmentsQuery : IRequest<GetRFQAttachmentsDto>
    {
        public Guid RFQId { get; set; }
    }
}