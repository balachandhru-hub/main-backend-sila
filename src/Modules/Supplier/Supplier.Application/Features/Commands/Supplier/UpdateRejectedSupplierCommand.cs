using MediatR;
using Supplier.Domain.Dto;

namespace Supplier.Application.Features.Commands.Supplier.UpdateRejectedSupplier
{
    public class UpdateRejectedSupplierCommand : IRequest<bool>
    {
        public UpdateRejectedSupplierDto Supplier { get; set; } = new();
    }
}