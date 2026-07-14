using MediatR;
using SharedKernel.Dto;

namespace Buyer.Application.Features.Queries.Asset.GetDocument
{
    public class GetDocumentQuery : IRequest<AssetDownloadDto>
    {
        public Guid AssetId { get; set; }

        public GetDocumentQuery(Guid assetId)
        {
            AssetId = assetId;
        }
    }
}