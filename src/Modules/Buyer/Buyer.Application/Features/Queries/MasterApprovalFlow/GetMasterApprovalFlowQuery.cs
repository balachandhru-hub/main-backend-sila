using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Queries.MasterApprovalFlow
{
    public class GetMasterApprovalFlowQuery : IRequest<List<MasterApprovalFlowDto>>
    {
        public int Index { get; set; }

        public int Limit { get; set; }

        public Guid? BuyerId { get; set; }
    }
}
