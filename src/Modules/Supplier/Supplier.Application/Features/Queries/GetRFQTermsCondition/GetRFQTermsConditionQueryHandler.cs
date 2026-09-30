using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Dto;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;
using Supplier.Domain.Common;
using Supplier.Domain.Dto;
using Supplier.Infrastructure.Contracts.IRepository;

namespace Supplier.Application.Features.Queries.GetRFQTermsCondition
{
    public class GetRFQTermsConditionQueryHandler
        : IRequestHandler<GetRFQTermsConditionQuery, List<RFQTermsConditionDto>>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly IMetadataApiClient _metadataClient;
        private readonly ILoggerManager _logger;

        public GetRFQTermsConditionQueryHandler(
            IRepositoryWrapper repository,
            IMetadataApiClient metadataClient,
            ILoggerManager logger)
        {
            _repository = repository;
            _metadataClient = metadataClient;
            _logger = logger;
        }

        public async Task<List<RFQTermsConditionDto>> Handle(
            GetRFQTermsConditionQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo(
                $"Fetching Terms and Condition for BuyerRFQId: {request.RFQId}");

            // One BuyerRFQ fans out to one SupplierRFQ per invited supplier,
            // so this returns every invited supplier's status in one call.
            var supplierRFQs = await _repository.SupplierRFQ
                .FindByCondition(x => x.BuyerRFQId == request.RFQId)
                .ToListAsync(cancellationToken);

            if (!supplierRFQs.Any())
            {
                _logger.LogError(
                    $"RFQ not found for BuyerRFQId: {request.RFQId}");
                throw new NotFoundCustomException(
                    "RFQ not found.",
                    $"No RFQ exists with BuyerRFQId: {request.RFQId}.");
            }

            var supplierRFQIds = supplierRFQs
                .Select(x => x.Id)
                .ToList();

            var attachmentMappings = await _repository.RFQAttachmentMapping
                .FindByCondition(x =>
                    supplierRFQIds.Contains(x.SupplierRFQId) &&
                    x.Type == Common.TERMS_CONDITION &&
                    x.IsActive)
                .ToListAsync(cancellationToken);

            var assetIds = attachmentMappings
                .Select(x => x.AssetId)
                .Distinct()
                .ToList();

            var assets = await _repository.Asset
                .FindByCondition(x =>
                    assetIds.Contains(x.Id) &&
                    x.IsActive)
                .ToListAsync(cancellationToken);

            List<MetadataDto>? metadataList = null;

            if (assets.Any())
            {
                metadataList = await _metadataClient.GetReferenceList(
                    new List<string> { Common.ASSET_TYPE, Common.FILE_TYPE });
            }

            var supplierIds = supplierRFQs
                .Select(x => x.SupplierId)
                .Distinct()
                .ToList();

            var supplierNames = await _repository.SupplierBusinessProfile
                .FindByCondition(x =>
                    supplierIds.Contains(x.Id) &&
                    x.IsActive)
                .ToDictionaryAsync(
                    x => x.Id,
                    x => x.OrganizationName,
                    cancellationToken);

            var result = supplierRFQs.Select(supplierRFQ =>
            {
                var attachments = attachmentMappings
                    .Where(x => x.SupplierRFQId == supplierRFQ.Id)
                    .Join(assets, x => x.AssetId, a => a.Id, (x, asset) => asset)
                    .Select(asset => new AssetDto
                    {
                        Id = asset.Id,

                        AssetType = metadataList?.FirstOrDefault(x =>
                            x.Type == Common.ASSET_TYPE &&
                            x.Id == asset.AssetType)?.Key ?? string.Empty,

                        AssetName = asset.AssetName,

                        FileType = metadataList?.FirstOrDefault(x =>
                            x.Type == Common.FILE_TYPE &&
                            x.Id == asset.FileType)?.Key ?? string.Empty,

                        FileName = asset.FileName
                    })
                    .ToList();

                return new RFQTermsConditionDto
                {
                    TermsAndCondition = supplierRFQ.TermsAndCondition,
                    SupplierId = supplierRFQ.SupplierId,
                    SupplierName = supplierNames.TryGetValue(supplierRFQ.SupplierId, out var name)
                        ? name
                        : null,
                    Attachments = attachments
                };
            }).ToList();

            _logger.LogInfo(
                $"Terms and Condition fetched for BuyerRFQId: {request.RFQId}. " +
                $"Suppliers: {result.Count}");

            return result;
        }
    }
}
