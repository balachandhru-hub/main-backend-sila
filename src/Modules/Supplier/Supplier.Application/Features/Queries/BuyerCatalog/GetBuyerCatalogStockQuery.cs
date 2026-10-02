using MediatR;
using Supplier.Domain.Dto;

namespace Supplier.Application.Features.Queries.BuyerCatalog
{
    public class GetBuyerCatalogStockQuery : IRequest<List<BuyerCatalogStockDto>>
    {
        public List<Guid> CatalogIds { get; set; } = new();
    }
}
