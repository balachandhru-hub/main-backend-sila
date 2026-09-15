using MediatR;
using SharedKernel.Dto;

namespace Buyer.Application.Features.Queries.Asset.GetDocument
{
    public class GetDocumentQuery : IRequest<AssetDownloadDto>
    {
        public Guid AssetId { get; set; }
        public string? ExternalSessionToken { get; set; }
        public Guid? ExternalRFQId { get; set; }

        public GetDocumentQuery(Guid assetId)
        {
            AssetId = assetId;
        }

        public GetDocumentQuery(Guid assetId, string externalSessionToken, Guid externalRFQId)
        {
            AssetId = assetId;
            ExternalSessionToken = externalSessionToken;
            ExternalRFQId = externalRFQId;
        }
    }
}