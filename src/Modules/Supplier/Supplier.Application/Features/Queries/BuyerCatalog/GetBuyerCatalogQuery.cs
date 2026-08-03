using MediatR;
using Supplier.Domain.Dto;

namespace Supplier.Application.Features.Queries.BuyerCatalog
{
    public class GetBuyerCatalogQuery : IRequest<List<BuyerCatalogDto>>
    {
        public long? Segment { get; set; }

        public long? Family { get; set; }

        public long? Class { get; set; }

        public long? Commodity { get; set; }

        public string? Search { get; set; }

        public int Index { get; set; }

        public int Limit { get; set; }
    }
}
