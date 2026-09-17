using Buyer.Domain.Dto;
using MediatR;

namespace Buyer.Application.Features.Queries.CreateMessage
{
    public class GetMessageHistoryQuery : IRequest<List<MessageResponseDto>>
    {
        public Guid ThreadId { get; set; }
        public Guid OrganizationId { get; set; }
        public string OrganizationType { get; set; }
        public int Index { get; set; } = 0;
        public int Limit { get; set; } = 10;

        /// <summary>Set only when the caller is a session-token-authenticated ExternalSupplier.</summary>
        public Guid? ExternalSupplierCallerId { get; set; }
    }
}
