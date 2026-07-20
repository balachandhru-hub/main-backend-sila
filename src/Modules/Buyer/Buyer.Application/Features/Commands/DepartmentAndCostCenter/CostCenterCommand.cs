
using MediatR;
using Buyer.Domain.Dto;

namespace Buyer.Application.Features.Commands.CostCenter
{
    public class CreateBuyerCostCenterCommand : IRequest<Guid>
{
    public CreateBuyerCostCenterDto BuyerCostCenterDto { get; set; }

    public CreateBuyerCostCenterCommand(CreateBuyerCostCenterDto dto)
    {
        BuyerCostCenterDto = dto;
    }
}
}