using MediatR;
using Supplier.Domain.Dto;

namespace Supplier.Application.Features.Commands.Supplier
{
    public record CreateSupplierProfileCommand(
        SupplierProfileDto SupplierProfileDto
    ) : IRequest<Guid>;
}