using MediatR;

namespace Supplier.Application.Features.Commands.DeleteDispatchLocation
{
    public class DeleteDispatchLocationCommand : IRequest<Guid>
    {
        public Guid Id { get; set; }
        public Guid SupplierId { get; set; }
    }
}
