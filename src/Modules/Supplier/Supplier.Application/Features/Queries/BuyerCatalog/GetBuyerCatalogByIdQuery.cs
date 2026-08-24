using MediatR;
using Supplier.Domain.Dto;

namespace Supplier.Application.Features.Queries.BuyerCatalog
{
    public class GetBuyerCatalogByIdQuery : IRequest<List<BuyerCatalogByIdDto>>
    {
        public Guid? CatalogId { get; set; }
    }
}
