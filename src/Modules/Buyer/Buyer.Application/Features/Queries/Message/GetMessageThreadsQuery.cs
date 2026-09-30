using Buyer.Domain.Dto;
using MediatR;

namespace Buyer.Application.Features.Queries.CreateMessage
{
    public class GetMessageThreadsQuery : IRequest<List<MessageThreadSummaryDto>>
    {
        public Guid RFQId { get; set; }
        public Guid OrganizationId { get; set; }
        public string OrganizationType { get; set; }

        /// <summary>Set only when the caller is a session-token-authenticated ExternalSupplier.</summary>
        public Guid? ExternalSupplierCallerId { get; set; }
    }
}
