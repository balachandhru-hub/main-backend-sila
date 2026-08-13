using MediatR;
using Supplier.Domain.Dto;

namespace Supplier.Application.Features.Commands.CreateSupplierDispatchLocation
{
    public class CreateSupplierDispatchLocationCommand : IRequest<Guid>
    {
        public Guid SupplierId { get; set; }
        public SupplierDispatchLocationDto Data { get; set; } = new();
    }
}
