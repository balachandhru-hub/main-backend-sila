using MediatR;
using SharedKernel.Dto;

namespace Supplier.Application.Features.Queries.Asset.GetDocument
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