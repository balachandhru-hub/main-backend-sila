using MediatR;
using Buyer.Domain.Dto;

namespace Buyer.Application.Features.Commands.Department
{
   public class CreateBuyerDepartmentCommand : IRequest<Guid>
{
    public CreateBuyerDepartmentDto BuyerDepartmentDto { get; set; }

    public CreateBuyerDepartmentCommand(CreateBuyerDepartmentDto buyerDepartmentDto)
    {
        BuyerDepartmentDto = buyerDepartmentDto;
    }
}
}