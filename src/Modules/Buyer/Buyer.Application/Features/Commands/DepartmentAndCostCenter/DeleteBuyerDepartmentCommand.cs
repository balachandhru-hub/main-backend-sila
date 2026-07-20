using MediatR;
namespace Buyer.Application.Features.Commands.Department
{
public class DeleteBuyerDepartmentCommand : IRequest<Guid>
{
    public Guid Id { get; set; }

    public DeleteBuyerDepartmentCommand(Guid id)
    {
        Id = id;
    }
}
}