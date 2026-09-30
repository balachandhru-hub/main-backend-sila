using Buyer.Domain.Dto;
using MediatR;
namespace Buyer.Application.Features.Commands.CostCenter
{

public class UpdateBuyerCostCenterCommand : IRequest<Guid>
{
    public Guid Id { get; set; }

    public UpdateBuyerCostCenterDto BuyerDepartmentDto { get; set; }

    public UpdateBuyerCostCenterCommand(Guid id, UpdateBuyerCostCenterDto dto)
    {
        Id = id;
        BuyerDepartmentDto = dto;
    }
}
}