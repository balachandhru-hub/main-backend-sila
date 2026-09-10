using Buyer.Domain.Dto;
using MediatR;

namespace Buyer.Application.Features.Queries.Message
{
    public class GetMessageHistoryQuery : IRequest<List<MessageResponseDto>>
    {
        public Guid ThreadId { get; set; }
        public Guid OrganizationId { get; set; }
        public string OrganizationType { get; set; }
        public int Index { get; set; } = 0;
        public int Limit { get; set; } = 10;
    }
}
