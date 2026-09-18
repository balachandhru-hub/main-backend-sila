using MediatR;
using Supplier.Domain.Dto;

namespace Supplier.Application.Features.Queries.GetBuyerTermsConditionStatus
{
    public class GetBuyerTermsConditionStatusQuery
        : IRequest<List<BuyerTermsAndConditionStatusDto>>
    {
        public Guid RFQId { get; set; }
    }
}
