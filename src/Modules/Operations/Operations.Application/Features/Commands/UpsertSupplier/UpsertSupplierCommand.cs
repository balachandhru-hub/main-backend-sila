using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Commands.UpsertSupplier
{
    /// <summary>
    /// Creates a supplier (no id) or updates one, with its aliases.
    /// </summary>
    public class UpsertSupplierCommand : IRequest<SupplierResponseDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid? SupplierId { get; set; }
        public SupplierUpsertRequestDto Request { get; set; } = new();
    }
}
