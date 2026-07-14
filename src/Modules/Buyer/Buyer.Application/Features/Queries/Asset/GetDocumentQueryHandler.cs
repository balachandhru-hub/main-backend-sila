using MediatR;
using Buyer.Infrastructure.Contracts.IRepository;
using Microsoft.Extensions.Configuration;
using SharedKernel.Dto;
using SharedKernel.ExceptionHandler;
using Buyer.Domain.Common;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.Asset.GetDocument
{
    public class GetDocumentQueryHandler : IRequestHandler<GetDocumentQuery, AssetDownloadDto>
    {
        private readonly IRepositoryWrapper _repositoryWrapper;
        private readonly IConfiguration _configuration;
        private readonly IMetadataApiClient _metadataClient;
        private readonly ILoggerManager _logger;

        public GetDocumentQueryHandler(
            IRepositoryWrapper repositoryWrapper,
            IConfiguration configuration,
            IMetadataApiClient metadataClient,
            ILoggerManager logger)
        {
            _repositoryWrapper = repositoryWrapper;
            _configuration = configuration;
            _metadataClient = metadataClient;
            _logger = logger;
        }

        public async Task<AssetDownloadDto> Handle(
            GetDocumentQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Retrieving document for asset Id: {request.AssetId}");

            var asset = _repositoryWrapper.Asset
                .FindFirstByCondition(x => x.Id == request.AssetId);

            if (asset == null)
            {
                throw new NotFoundCustomException(
                    "Asset not found",
                    $"Asset with Id {request.AssetId} not found.");
            }

            var filePath = Path.Combine(
                _configuration[Common.BASE_FOLDER_PATH]!,
                request.AssetId.ToString(),
                asset.FileName);

            if (!File.Exists(filePath))
            {
                throw new NotFoundCustomException(
                    "File not found",
                    $"File for asset {request.AssetId} not found.");
            }

            var fileBytes = await File.ReadAllBytesAsync(filePath, cancellationToken);

            // Fetch content type using Metadata API Client
            var metadata = await _metadataClient.GetRefTermKeyById(asset.FileType);

            return new AssetDownloadDto
            {
                AssetId = asset.Id,
                FileName = asset.FileName,
                ContentType = metadata,
                FileBytes = fileBytes
            };
        }
    }
}