using MediatR;

namespace Buyer.Application.Features.Queries.ApprovalFlowUserMapping
{
    public class GetApprovalFlowUserMappingQuery : IRequest<List<Guid>>
    {
        public Guid ApprovalId { get; set; }

        public GetApprovalFlowUserMappingQuery(Guid approvalId)
        {
            ApprovalId = approvalId;
        }
    }
}
