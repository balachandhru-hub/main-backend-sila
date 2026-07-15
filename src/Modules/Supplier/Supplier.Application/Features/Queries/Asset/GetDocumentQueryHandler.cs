using MediatR;
using Supplier.Infrastructure.Contracts.IRepository;
using Microsoft.Extensions.Configuration;
using SharedKernel.Dto;
using SharedKernel.ExceptionHandler;
using Supplier.Domain.Common;
using SharedKernel.LoggerServices;

namespace Supplier.Application.Features.Queries.Asset.GetDocument
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
                _logger.LogError($"Asset not found. AssetId: {request.AssetId}");
                throw new NotFoundCustomException(
                    "Asset not found",
                    $"Asset with Id {request.AssetId} not found.");
            }

          var basePath = _configuration[Common.BASE_FOLDER_PATH]!;

var filePath = Path.Combine(
    basePath,
    request.AssetId.ToString(),
    asset.FileName);

// Debug logs
_logger.LogInfo($"Base Folder Path: {basePath}");
_logger.LogInfo($"Asset Id: {request.AssetId}");
_logger.LogInfo($"File Name: {asset.FileName}");
_logger.LogInfo($"Expected File Path: {filePath}");

if (!File.Exists(filePath))
{
    _logger.LogError($"File does not exist at path: {filePath}");

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