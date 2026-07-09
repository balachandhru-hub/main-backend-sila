using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.LoggerServices;
using Buyer.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Buyer.Domain.Common;
using SharedKernel.ExceptionHandler;
using Buyer.Domain.Dto;

namespace Buyer.Application.Features.Assets.Commands
{
    public class UploadAssetCommandHandler : IRequestHandler<UploadAssetCommand, Guid>
    {
        private readonly IRepositoryWrapper _repositoryWrapper;
        private readonly ILoggerManager _logger;
        private readonly IConfiguration _configuration;
        private readonly IMetadataApiClient _metadataClient;

        public UploadAssetCommandHandler(IRepositoryWrapper repositoryWrapper, ILoggerManager logger, IConfiguration configuration, IMetadataApiClient metadataClient)
        {
            _repositoryWrapper = repositoryWrapper;
            _logger = logger;
            _configuration = configuration;
            _metadataClient = metadataClient;
        }

        public async Task<Guid> Handle(UploadAssetCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Uploading document for file : {request.assetUploadDto.FileName}");
            List<MetadataDto>? metadataList;
            try
            {
                _logger.LogInfo($"Fetching metadata for AssetType: {request.assetUploadDto.AssetType}, EntityType: {request.assetUploadDto.EntityType}");
                metadataList = await _metadataClient.GetReferenceList(new List<string> { Common.ASSET_TYPE, Common.ENTITY_TYPE, Common.FILE_TYPE });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error occurred while fetching metadata: {ex.Message}");
                throw new PreConditionFailedCustomException("Unable to find Metadata for the given AssetType, EntityType or FileType. Please check the values provided.", "Unable to find Metadata for the given AssetType, EntityType or FileType. Please check the values provided.");
            }
            Guid assetId = Guid.NewGuid();
            if (request.assetUploadDto.IsSingletonAsset)
            {
                var assetTypeId = metadataList?.FirstOrDefault(x => x.Type == Common.ASSET_TYPE && x.Key == request.assetUploadDto.AssetType)?.Id;
                Asset oldAsset = _repositoryWrapper.Asset.FindFirstByCondition(x => x.IsActive && x.EntityId == request.assetUploadDto.EntityId && x.AssetType == assetTypeId);

                if (oldAsset != null)
                {
                    oldAsset.IsActive = false;
                    _repositoryWrapper.Asset.Update(oldAsset);
                    _repositoryWrapper.Save();

                    string oldFilePath = Path.Combine($"{_configuration[Common.BASE_FOLDER_PATH]}", oldAsset.Id.ToString());
                    if (File.Exists(oldFilePath))
                    {
                        File.Delete(oldFilePath);
                        _logger.LogInfo($"Deleted The existing Asset with Id : {oldAsset.Id}");
                    }
                }
            }

            string filePath = Path.Combine($"{_configuration[Common.BASE_FOLDER_PATH]}", assetId.ToString());

            string fileExtension = Path.GetExtension(request.assetUploadDto.FileName!).TrimStart('.');
            string fullFilePath = Path.Combine(filePath, request.assetUploadDto.FileName!);

            // Log the directory and file path
            _logger.LogInfo($"Directory: {filePath}");
            _logger.LogInfo($"Full file path: {fullFilePath}");

            // Check if the directory exists, and create it if it doesn't
            if (!Directory.Exists(filePath))
            {
                Directory.CreateDirectory(filePath);
                _logger.LogInfo($"Directory created: {filePath}");
            }
            else
            {
                _logger.LogInfo($"Directory already exists: {filePath}");
            }

            // Write the file and log the action
            File.WriteAllBytes(fullFilePath, request.assetUploadDto.FileBytes!);
            _logger.LogInfo($"File created: {fullFilePath}");


            _logger.LogInfo($"Adding Assets for the {request.assetUploadDto.FileName}");
            Asset asset = new Asset
            {
                Id = assetId,
                AssetType = request.assetUploadDto.AssetType == null ? null : metadataList.FirstOrDefault(x => x.Key == Common.ASSET_TYPE)?.Id,
                AssetName = request.assetUploadDto.FileName,
                EntityType = request.assetUploadDto.EntityType == null ? null : metadataList.FirstOrDefault(x => x.Key == Common.ENTITY_TYPE)?.Id,
                EntityId = request.assetUploadDto.EntityId,
                FileName = request.assetUploadDto.FileName!,
                FileType = (Guid)metadataList?.FirstOrDefault(x => x.Type == Common.FILE_TYPE && x.Key == fileExtension)?.Id,

            };
            _repositoryWrapper.Asset.Create(asset);
            _repositoryWrapper.Save();
            _logger.LogInfo($"Asset with ID {assetId} uploaded successfully.");
            return assetId;
        }
    }
}