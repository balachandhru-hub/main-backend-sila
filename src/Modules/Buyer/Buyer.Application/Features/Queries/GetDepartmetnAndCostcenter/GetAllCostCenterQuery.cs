using Buyer.Domain.Dto;
using MediatR;

namespace Buyer.Application.Features.Queries.CostCenter
{
    public class GetBuyerCostCenterQuery : IRequest<List<BuyerCostCenterDto>>
{
    public Guid DepartmentId { get; set; }

    public int Index { get; set; }

    public int Limit { get; set; }

    public string? SearchTerm { get; set; }
}
}