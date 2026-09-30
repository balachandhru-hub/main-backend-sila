using Buyer.Domain.Common;
using Buyer.Domain.Dto;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Dto;

namespace Buyer.Application.Features.Queries.GetRFQAttachments
{
    public class GetRFQAttachmentsQueryHandler
        : IRequestHandler<GetRFQAttachmentsQuery, GetRFQAttachmentsDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly IMetadataApiClient _metadataClient;

        public GetRFQAttachmentsQueryHandler(
            IRepositoryWrapper repository,
            IMetadataApiClient metadataClient)
        {
            _repository = repository;
            _metadataClient = metadataClient;
        }

        public async Task<GetRFQAttachmentsDto> Handle(
            GetRFQAttachmentsQuery request,
            CancellationToken cancellationToken)
        {
            var metadataList = await _metadataClient.GetReferenceList(
                new List<string>
                {
                    Common.ASSET_TYPE,
                    Common.FILE_TYPE
                });

            var technicalAssets = await (
                from mapping in _repository.RFQAttachmentMapping.FindByCondition(x =>
                    x.RFQId == request.RFQId &&
                    x.Type == Common.TECHNICAL_SPECIFICATION)

                join asset in _repository.Asset.FindByCondition(x => x.IsActive)
                    on mapping.AssetId equals asset.Id

                select asset
            ).ToListAsync(cancellationToken);

            var technicalDocuments = technicalAssets.Select(asset => new AssetDto
            {
                Id = asset.Id,
                AssetType = metadataList.FirstOrDefault(x =>
                    x.Type == Common.ASSET_TYPE &&
                    x.Id == asset.AssetType)?.Key ?? string.Empty,
                AssetName = asset.AssetName,
                FileType = metadataList.FirstOrDefault(x =>
                    x.Type == Common.FILE_TYPE &&
                    x.Id == asset.FileType)?.Key ?? string.Empty,
                FileName = asset.FileName
            }).ToList();

            var termAssets = await (
                from mapping in _repository.RFQAttachmentMapping.FindByCondition(x =>
                    x.RFQId == request.RFQId &&
                    x.Type == Common.TERMS_CONDITION)

                join asset in _repository.Asset.FindByCondition(x => x.IsActive)
                    on mapping.AssetId equals asset.Id

                select asset
            ).ToListAsync(cancellationToken);

            var termsDocuments = termAssets.Select(asset => new AssetDto
            {
                Id = asset.Id,
                AssetType = metadataList.FirstOrDefault(x =>
                    x.Type == Common.ASSET_TYPE &&
                    x.Id == asset.AssetType)?.Key ?? string.Empty,
                AssetName = asset.AssetName,
                FileType = metadataList.FirstOrDefault(x =>
                    x.Type == Common.FILE_TYPE &&
                    x.Id == asset.FileType)?.Key ?? string.Empty,
                FileName = asset.FileName
            }).ToList();
            var esignAssets = await (
                from mapping in _repository.RFQAttachmentMapping.FindByCondition(x =>
                    x.RFQId == request.RFQId &&
                    x.Type == Common.ESIGN)

                join asset in _repository.Asset.FindByCondition(x => x.IsActive)
                    on mapping.AssetId equals asset.Id

                select asset
            ).ToListAsync(cancellationToken);

            var esignDocuments = esignAssets.Select(asset => new AssetDto
            {
                Id = asset.Id,
                AssetType = metadataList.FirstOrDefault(x =>
                    x.Type == Common.ASSET_TYPE &&
                    x.Id == asset.AssetType)?.Key ?? string.Empty,
                AssetName = asset.AssetName,
                FileType = metadataList.FirstOrDefault(x =>
                    x.Type == Common.FILE_TYPE &&
                    x.Id == asset.FileType)?.Key ?? string.Empty,
                FileName = asset.FileName
            }).ToList();

            // RFQ's SegmentId -> the buyer's ContractTemplate for that segment -> its asset.
            var rfqOwner = await _repository.RFQ
                .FindByCondition(x => x.Id == request.RFQId)
                .Select(x => new { x.BuyerId, x.SegmentId })
                .FirstOrDefaultAsync(cancellationToken);

            var contractTemplateAssets = rfqOwner == null
                ? new List<Buyer.Domain.Entities.Asset>()
                : await (
                    from template in _repository.ContractTemplate.FindByCondition(x =>
                        x.BuyerId == rfqOwner.BuyerId &&
                        x.SegmentId == rfqOwner.SegmentId &&
                        x.IsActive)

                    join asset in _repository.Asset.FindByCondition(x => x.IsActive)
                        on template.AssetId equals asset.Id

                    select asset
                ).ToListAsync(cancellationToken);

            var contractTemplateDocuments = contractTemplateAssets.Select(asset => new AssetDto
            {
                Id = asset.Id,
                AssetType = metadataList.FirstOrDefault(x =>
                    x.Type == Common.ASSET_TYPE &&
                    x.Id == asset.AssetType)?.Key ?? string.Empty,
                AssetName = asset.AssetName,
                FileType = metadataList.FirstOrDefault(x =>
                    x.Type == Common.FILE_TYPE &&
                    x.Id == asset.FileType)?.Key ?? string.Empty,
                FileName = asset.FileName
            }).ToList();

            var rfqItems = await _repository.RFQItem
    .FindByCondition(x => x.RFQId == request.RFQId)
    .ToListAsync(cancellationToken);

            var itemAttachments = new List<RFQItemAttachmentDto>();

            foreach (var item in rfqItems)
            {
                var attachmentAssets = await (
                    from mapping in _repository.RFQItemAttachmentMapping.FindByCondition(x =>
                        x.RFQItemId == item.Id &&
                        x.Type == Common.RFQ_ITEM_ATTACHMENT)

                    join asset in _repository.Asset.FindByCondition(x => x.IsActive)
                        on mapping.AssetId equals asset.Id

                    select asset
                ).ToListAsync(cancellationToken);

                var attachments = attachmentAssets.Select(asset => new AssetDto
                {
                    Id = asset.Id,
                    AssetType = metadataList.FirstOrDefault(x =>
                        x.Type == Common.ASSET_TYPE &&
                        x.Id == asset.AssetType)?.Key ?? string.Empty,

                    AssetName = asset.AssetName,

                    FileType = metadataList.FirstOrDefault(x =>
                        x.Type == Common.FILE_TYPE &&
                        x.Id == asset.FileType)?.Key ?? string.Empty,

                    FileName = asset.FileName
                }).ToList();

                itemAttachments.Add(new RFQItemAttachmentDto
                {

                    Attachments = attachments
                });
            }

            return new GetRFQAttachmentsDto
            {
                TechnicalSpecificationDocuments = technicalDocuments,
                TermsConditionDocuments = termsDocuments,
                ESignDocuments = esignDocuments,
                ContractTemplateDocuments = contractTemplateDocuments,
                ItemAttachments = itemAttachments
            };
        }
    }
}