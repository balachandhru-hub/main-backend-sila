using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Queries.GetPurchaseOrders
{
    public class GetPurchaseOrdersQuery : IRequest<List<PurchaseOrderListItemDto>>
    {
        public Guid OrganizationId { get; set; }
        public int Index { get; set; }
        public int Limit { get; set; }
    }
}
