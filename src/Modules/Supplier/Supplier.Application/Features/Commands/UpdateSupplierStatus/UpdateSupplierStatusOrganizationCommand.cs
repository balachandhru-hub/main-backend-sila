using MediatR;
using Supplier.Domain.Dto;

namespace Supplier.Application.Features.Commands.Supplier.UpdateSupplierStatusOrganization
{
    public class UpdateSupplierStatusOrganizationCommand : IRequest<bool>
    {
        public UpdateSupplierStatusDto Supplier { get; set; } = default!;
    }
}