using MediatR;
using Supplier.Domain.Dto;

namespace Supplier.Application.Features.Queries.GetRFQTermsCondition
{
    public class GetRFQTermsConditionQuery : IRequest<List<RFQTermsConditionDto>>
    {
        public Guid RFQId { get; set; }
    }
}
