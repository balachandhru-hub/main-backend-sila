using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Queries.GetSupplierPurchaseOrders
{
    /// <summary>
    /// The purchase orders addressed to the supplier of the signed-in user, from every buyer.
    /// </summary>
    public class GetSupplierPurchaseOrdersQuery : IRequest<List<PurchaseOrderListItemDto>>
    {
        public int Index { get; set; }
        public int Limit { get; set; }
    }
}
