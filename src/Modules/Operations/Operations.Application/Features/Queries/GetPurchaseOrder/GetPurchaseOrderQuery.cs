using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Queries.GetPurchaseOrder
{
    /// <summary>
    /// Returns a purchase order of the organization by its number.
    /// </summary>
    public class GetPurchaseOrderQuery : IRequest<PurchaseOrderResponseDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public string PoNumber { get; set; } = string.Empty;
        public string? EntityCode { get; set; }
    }
}
