using Buyer.Domain.Dto;
using MediatR;
namespace Buyer.Application.Features.Commands.Department
{

public class UpdateBuyerDepartmentCommand : IRequest<Guid>
{
    public Guid Id { get; set; }

    public UpdateBuyerDepartmentDto BuyerDepartmentDto { get; set; }

    public UpdateBuyerDepartmentCommand(Guid id, UpdateBuyerDepartmentDto dto)
    {
        Id = id;
        BuyerDepartmentDto = dto;
    }
}
}