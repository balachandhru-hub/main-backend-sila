using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Queries.GetPredefinedMaterialDetail
{
    public class GetPredefinedMaterialDetailQuery
        : IRequest<PredefinedMaterialDetailDto>
    {
        public Guid PredefinedMaterialId { get; }

        public GetPredefinedMaterialDetailQuery(Guid predefinedMaterialId)
        {
            PredefinedMaterialId = predefinedMaterialId;
        }
    }
}
