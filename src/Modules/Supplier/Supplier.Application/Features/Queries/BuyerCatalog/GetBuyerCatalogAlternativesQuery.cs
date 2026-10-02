using MediatR;
using Supplier.Domain.Dto;

namespace Supplier.Application.Features.Queries.BuyerCatalog
{
    public class GetBuyerCatalogAlternativesQuery : IRequest<List<BuyerCatalogStockDto>>
    {
        public Guid CatalogId { get; set; }

        /// <summary>
        /// Quantity the alternative must be able to supply.
        /// </summary>
        public decimal Quantity { get; set; }
    }
}
