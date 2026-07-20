using MediatR;
namespace Buyer.Application.Features.Commands.CostCenter
{
public class DeleteBuyerCostCenterCommand : IRequest<Guid>
{
    public Guid Id { get; set; }

    public DeleteBuyerCostCenterCommand(Guid id)
    {
        Id = id;
    }
}
}