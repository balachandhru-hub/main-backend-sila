using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Queries.SearchPurchaseOrders
{
    /// <summary>
    /// Searches the purchase orders of the organization.
    /// </summary>
    public class SearchPurchaseOrdersQuery : IRequest<List<PurchaseOrderResponseDto>>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public PurchaseOrderSearchRequestDto Request { get; set; } = new();
    }
}
