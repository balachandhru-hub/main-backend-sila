using Buyer.Domain.Dto;
using MediatR;

namespace Buyer.Application.Features.Queries.GetSupplierTermsConditionStatus
{
    public class GetSupplierTermsConditionStatusQuery
        : IRequest<SupplierTermsAndConditionStatusDto>
    {
        public Guid RFQId { get; set; }
    }
}
