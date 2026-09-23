using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Queries.CheckPredefinedMaterialSimilarity
{
    public class CheckPredefinedMaterialSimilarityQuery
        : IRequest<List<SimilarPredefinedMaterialDto>>
    {
        public string Description { get; set; }

        public string MaterialGroup { get; set; }

        // Not scoped by default - mirrors the existing MaterialCode
        // uniqueness check, which is global across buyers. Pass this to
        // narrow the check to one buyer instead.
        public Guid? BuyerId { get; set; }
    }
}
