using MediatR;
using Supplier.Domain.Dto;

namespace Supplier.Application.Features.Commands.UpdateSupplierDispatchLocation
{
    public class UpdateSupplierDispatchLocationCommand : IRequest<Guid>
    {
        public Guid Id { get; set; }

        public SupplierDispatchLocationUpdateDto Data { get; set; } = new();
    }
}