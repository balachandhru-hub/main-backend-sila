using Buyer.Domain.Dto;
using MediatR;

namespace Buyer.Application.Features.Queries.GetCostCenterById
{
    public class GetCostCenterByIdQuery : IRequest<CostCenterDto>
    {
        public Guid CostCenterId { get; set; }
    }
}