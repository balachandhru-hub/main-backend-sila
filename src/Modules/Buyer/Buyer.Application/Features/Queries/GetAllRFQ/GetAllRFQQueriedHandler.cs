using Buyer.Domain.Common;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Dto;
using SharedKernel.LoggerServices;
using SharedKernel.ExceptionHandler;
using Buyer.Domain.Dto;
using Buyer.Application.Contracts;

namespace Buyer.Application.Features.Queries.GetAllRFQ
{
    public class GetRFQByIdQueryHandler : IRequestHandler<GetRFQByIdQuery, GetRFQByIdDto>
    {
        private readonly IRepositoryWrapper _repositorywrapper;
        private readonly IMetadataApiClient _metadataClient;
        private readonly ILoggerManager _logger;
        private readonly ISupplierApiClient _supplierApiClient;
        private readonly IIdentityApiClient _identityApiClient;

        public GetRFQByIdQueryHandler(IRepositoryWrapper repository, IMetadataApiClient metadataApiClient, ILoggerManager logger, ISupplierApiClient supplierApiClient, IIdentityApiClient identityApiClient)
        {
            _repositorywrapper = repository;
            _metadataClient = metadataApiClient;
            _logger = logger;
            _supplierApiClient = supplierApiClient;
            _identityApiClient = identityApiClient;
        }

        public async Task<GetRFQByIdDto> Handle(
            GetRFQByIdQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching RFQ details for RFQId: {request.RFQId}");
            List<MetadataDto>? metadataList;

            try
            {
                metadataList = await _metadataClient.GetReferenceList(
                    new List<string>
                    {Common.ASSET_TYPE,Common.FILE_TYPE,Common.ENTITY_TYPE
                    });
            }
            catch
            {
                throw new PreConditionFailedCustomException(
                    "Unable to fetch metadata.",
                    "Unable to fetch AssetType, FileType and EntityType metadata."
                );
            }


            var rfq = await _repositorywrapper.RFQ
                .FindByCondition(x => x.Id == request.RFQId)
                .FirstOrDefaultAsync(cancellationToken);

            if (rfq == null)
            {
                throw new NotFoundCustomException(
                    "RFQ not found.",
                    $"No RFQ exists with RFQId: {request.RFQId}."
                );
            }

            var buyer = _repositorywrapper.BuyerBusinessProfile
                .FindFirstByCondition(x =>
                    x.OrganizationId == request.OrganizationId &&
                    x.IsActive);

            if (buyer == null)
            {
                throw new PreConditionFailedCustomException(
                    "Buyer Not Found",
                    $"Buyer Not Found for the given organization : {request.OrganizationId}");
            }

            if (request.RoleId != Common.BUYER_ADMIN_ROLE_ID && rfq.CreatedBy != request.UserId)
            {
                _logger.LogError($"User with UserId: {request.UserId} attempted to access RFQ with RFQId: {request.RFQId} without proper permissions.");
                throw new ForBiddenCustomException("Access denied.", "You are not the creator of this RFQ.");
            }

            var buyerId = rfq.BuyerId;

            GetAllSupplierQuotationDto? supplierQuotation = null;

            try
            {
                supplierQuotation = await _supplierApiClient.GetSupplierQuotation(
                    request.RFQId,
                    cancellationToken);
            }
            catch
            {
                // Ignore if quotation is not created yet
                supplierQuotation = new GetAllSupplierQuotationDto();
            }

            SupplierRFQAnswerDto? supplierAnswers = null;

            try
            {
                _logger.LogInfo($"Fetching Supplier RFQ Answers for BuyerRFQId: {request.RFQId}");
                supplierAnswers = await _supplierApiClient.GetSupplierRFQAnswers(
                    request.RFQId,
                    cancellationToken);
            }
            catch
            {
                _logger.LogInfo($"No Supplier RFQ Answers found for BuyerRFQId: {request.RFQId}");
                supplierAnswers = new SupplierRFQAnswerDto();
            }
            var questions = await _repositorywrapper.RFQQuestion
                .FindByCondition(x => x.RFQId == request.RFQId)
                .OrderBy(x => x.DisplayOrder)
                .Select(x => new RFQQuestionDto
                {
                    Id = x.Id,
                    Question = x.Question,
                    QuestionType = x.QuestionType,
                    IsRequired = x.IsRequired,
                    DisplayOrder = x.DisplayOrder,
                    Options = new List<string>()
                })
                .ToListAsync(cancellationToken);
            // STEP 2: Get options for these questions
            var questionIds = questions
                .Select(x => x.Id)
                .ToList();

            var questionOptions = await _repositorywrapper.RFQQuestionOption
                .FindByCondition(x =>
                    questionIds.Contains(x.RFQQuestionId) &&
                    x.IsActive)
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync(cancellationToken);





            foreach (var question in questions)
            {
                question.Options = questionOptions
                    .Where(x => x.RFQQuestionId == question.Id)
                    .OrderBy(x => x.DisplayOrder)
                    .Select(x => x.OptionText)
                    .ToList();
            }


            foreach (var supplier in supplierAnswers.Suppliers)
            {
                foreach (var supplierAnswer in supplier.Answers)
                {
                    var question = questions.FirstOrDefault(q =>
                        q.Id == supplierAnswer.RFQQuestionId);

                    if (question == null)
                        continue;

                    // RADIO
                    if (question.QuestionType.Equals(
                        Common.RADIO_BUTTON,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        var selectedOption = supplierAnswer.QuestionOptionId.HasValue
                            ? questionOptions.FirstOrDefault(x =>
                                x.Id == supplierAnswer.QuestionOptionId.Value &&
                                x.RFQQuestionId == supplierAnswer.RFQQuestionId)
                            : null;

                        supplierAnswer.Answer = selectedOption?.OptionText ?? string.Empty;
                        supplierAnswer.QuestionOptionIds = new List<Guid>();

                        continue;
                    }

                    // CHECKBOX
                    if (question.QuestionType.Equals(
                        Common.CHECKBOX,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        // Checkbox uses ONLY QuestionOptionIds
                        if (supplierAnswer.QuestionOptionIds != null &&
                            supplierAnswer.QuestionOptionIds.Any())
                        {
                            supplierAnswer.QuestionOptionIds =
                                supplierAnswer.QuestionOptionIds
                                    .Distinct()
                                    .ToList();

                            var selectedOptions = questionOptions
                                .Where(x =>
                                    x.RFQQuestionId == supplierAnswer.RFQQuestionId &&
                                    supplierAnswer.QuestionOptionIds.Contains(x.Id))
                                .OrderBy(x => x.DisplayOrder)
                                .Select(x => x.OptionText)
                                .ToList();

                            supplierAnswer.Answer =
                                string.Join(", ", selectedOptions);
                        }


                        supplierAnswer.QuestionOptionId = null;

                        continue;
                    }


                }
            }

            var rfqItems = await _repositorywrapper.RFQItem
    .FindByCondition(x => x.RFQId == request.RFQId)
    .ToListAsync(cancellationToken);

            // rfqaward_item rows only exist once the RFQ is awarded,
            // so their presence doubles as the "RFQ awarded" check.
            // Lot-wise awards write no item rows — an active award on a
            // lot RFQ means every line is awarded.
            RFQAward? award = null;
            var awardItems = new List<RFQAwardItem>();
            HashSet<Guid> awardedItemIds;

            if (rfq.AddLotOption)
            {
                award = await _repositorywrapper.RFQAward
                    .FindByCondition(x =>
                        x.RFQId == request.RFQId &&
                        x.IsActive)
                    .FirstOrDefaultAsync(cancellationToken);

                awardedItemIds = award != null
                    ? rfqItems.Select(x => x.Id).ToHashSet()
                    : new HashSet<Guid>();
            }
            else
            {
                awardItems = await _repositorywrapper.RFQAwardItem
                    .FindByCondition(x =>
                        x.RFQAward.RFQId == request.RFQId &&
                        x.RFQAward.IsActive &&
                        x.IsActive)
                    .ToListAsync(cancellationToken);

                awardedItemIds = awardItems
                    .Select(x => x.RFQItemId)
                    .ToHashSet();
            }

            // Flag the winning quotation/item rows inside the supplier
            // quotation payload: quotation level for lot awards, item level
            // for line-wise awards.
            if (supplierQuotation != null)
            {
                if (rfq.AddLotOption)
                {
                    foreach (var supplier in supplierQuotation.Suppliers)
                    {
                        supplier.IsAwarded = award != null &&
                            (supplier.QuotationId == award.SupplierQuotationId ||
                             supplier.SupplierId == award.SupplierId);
                    }
                }
                else
                {
                    var awardedQuoteItemIds = awardItems
                        .Select(x => x.SupplierQuotationItemId)
                        .ToHashSet();

                    foreach (var supplier in supplierQuotation.Suppliers)
                    {
                        foreach (var quoteItem in supplier.SupplierQuotationItems)
                        {
                            quoteItem.IsAwarded =
                                awardedQuoteItemIds.Contains(quoteItem.ItemQuotationId);
                        }
                    }
                }
            }

            var items = new List<GetRFQItemDto>();

            foreach (var item in rfqItems)
            {
                var attachmentData = await (
                from mapping in _repositorywrapper.RFQItemAttachmentMapping.FindByCondition(x =>
                    x.RFQItemId == item.Id &&
                    x.Type == Common.RFQ_ITEM_ATTACHMENT)

                join asset in _repositorywrapper.Asset.FindByCondition(x => x.IsActive)
                    on mapping.AssetId equals asset.Id

                select asset
                )
                .ToListAsync(cancellationToken);

                var attachments = attachmentData.Select(asset => new AssetDto
                {
                    Id = asset.Id,
                    AssetType = metadataList!.FirstOrDefault(x =>
                                    x.Type == Common.ASSET_TYPE &&
                                    x.Id == asset.AssetType)?.Key ?? string.Empty,

                    FileType = metadataList.FirstOrDefault(x =>
                                    x.Type == Common.FILE_TYPE &&
                                    x.Id == asset.FileType)?.Key ?? string.Empty,

                    AssetName = asset.AssetName,
                    FileName = asset.FileName
                }).ToList();
                string? costCenterName = null;

                if (!string.IsNullOrWhiteSpace(item.CostCenter) &&
                    Guid.TryParse(item.CostCenter, out Guid costCenterId))
                {
                    costCenterName = await _repositorywrapper.BuyerCostCenter
                        .FindByCondition(x =>
                            x.Id == costCenterId &&
                            x.IsActive)
                        .Select(x => x.CostCenter)
                        .FirstOrDefaultAsync(cancellationToken);
                }

                items.Add(new GetRFQItemDto
                {
                    Id = item.Id,
                    Description = item.Description,
                    Quantity = item.Quantity,
                    UOM = item.UOM,
                    MaterialCode = item.MaterialCode,
                    MaterialGroup = item.MaterialGroup,

                    CostCenter = costCenterName,
                    Attachments = attachments,
                    IsAwarded = awardedItemIds.Contains(item.Id)


                });
            }

            var technicalAssetData = await (
                from mapping in _repositorywrapper.RFQAttachmentMapping.FindByCondition(x =>
                    x.RFQId == request.RFQId &&
                    x.Type == Common.TECHNICAL_SPECIFICATION)

                join asset in _repositorywrapper.Asset.FindByCondition(x => x.IsActive)
                    on mapping.AssetId equals asset.Id

                select asset
            ).ToListAsync(cancellationToken);

            var technicalDocuments = technicalAssetData.Select(asset => new AssetDto
            {
                Id = asset.Id,
                AssetType = metadataList!.FirstOrDefault(x =>
                                x.Type == Common.ASSET_TYPE &&
                                x.Id == asset.AssetType)?.Key ?? string.Empty,
                AssetName = asset.AssetName,
                FileType = metadataList.FirstOrDefault(x =>
                                x.Type == Common.FILE_TYPE &&
                                x.Id == asset.FileType)?.Key ?? string.Empty,
                FileName = asset.FileName
            }).ToList();

            var termsAssetData = await (from mapping in _repositorywrapper.RFQAttachmentMapping.FindByCondition(x =>
                x.RFQId == request.RFQId &&
                x.Type == Common.TERMS_CONDITION)

                                        join asset in _repositorywrapper.Asset.FindByCondition(x => x.IsActive)
                                            on mapping.AssetId equals asset.Id

                                        select asset
        ).ToListAsync(cancellationToken);

            var termsDocuments = termsAssetData.Select(asset => new AssetDto
            {
                Id = asset.Id,
                AssetType = metadataList!.FirstOrDefault(x =>
                                x.Type == Common.ASSET_TYPE &&
                                x.Id == asset.AssetType)?.Key ?? string.Empty,
                AssetName = asset.AssetName,
                FileType = metadataList.FirstOrDefault(x =>
                                x.Type == Common.FILE_TYPE &&
                                x.Id == asset.FileType)?.Key ?? string.Empty,
                FileName = asset.FileName
            }).ToList();
            var esignAssetData = await (
                from mapping in _repositorywrapper.RFQAttachmentMapping.FindByCondition(x =>
                    x.RFQId == request.RFQId &&
                    x.Type == Common.ESIGN)

                join asset in _repositorywrapper.Asset.FindByCondition(x => x.IsActive)
                    on mapping.AssetId equals asset.Id

                select asset
            ).ToListAsync(cancellationToken);

            var esignDocuments = esignAssetData.Select(asset => new AssetDto
            {
                Id = asset.Id,
                AssetType = metadataList!.FirstOrDefault(x =>
                                x.Type == Common.ASSET_TYPE &&
                                x.Id == asset.AssetType)?.Key ?? string.Empty,
                AssetName = asset.AssetName,
                FileType = metadataList.FirstOrDefault(x =>
                                x.Type == Common.FILE_TYPE &&
                                x.Id == asset.FileType)?.Key ?? string.Empty,
                FileName = asset.FileName
            }).ToList();

            var supplierIds = await _repositorywrapper.RFQSupplierMapping
            .FindByCondition(x => x.RFQId == request.RFQId)
            .Select(x => x.SupplierId)
            .ToListAsync(cancellationToken);
            var suppliers = await _supplierApiClient.GetSupplierNamesByIds(
    supplierIds,
    cancellationToken);

            // Terms and Condition acceptance + uploaded document for every
            // supplier invited to this RFQ, in one call.
            List<RFQTermsConditionDto> supplierTermsConditions;

            try
            {
                supplierTermsConditions = await _supplierApiClient.GetRFQTermsCondition(
                    request.RFQId,
                    cancellationToken);
            }
            catch
            {
                _logger.LogInfo(
                    $"No Terms and Condition found for BuyerRFQId: {request.RFQId}");
                supplierTermsConditions = new List<RFQTermsConditionDto>();
            }

            // E-Sign status + uploaded document for every supplier invited
            // to this RFQ, in one call.
            List<RFQESignDto> supplierESigns;

            try
            {
                supplierESigns = await _supplierApiClient.GetRFQESign(
                    request.RFQId,
                    cancellationToken);
            }
            catch
            {
                _logger.LogInfo(
                    $"No E-Sign found for BuyerRFQId: {request.RFQId}");
                supplierESigns = new List<RFQESignDto>();
            }

            // Verified = this buyer already has an active BuyerSupplierMapping with the
            // supplier (same check CreateRFQCommandHandler uses to decide who gets a fresh
            // SupplierVerificationRequest). Everyone else invited on this RFQ is unverified.
            var verifiedSupplierIdsForRFQ = _repositorywrapper.BuyerSupplierMapping
                .FindByCondition(x => x.BuyerId == buyerId && x.IsActive)
                .Select(x => x.SupplierId)
                .ToHashSet();

            foreach (var supplier in suppliers)
            {
                supplier.VerificationStatus = verifiedSupplierIdsForRFQ.Contains(supplier.SupplierId)
                    ? Common.VERIFIED_STATUS
                    : Common.UNVERIFIED_STATUS;
            }

            var externalSupplierIds = await _repositorywrapper.RFQExternalSupplier
            .FindByCondition(x => x.RFQId == request.RFQId && x.IsActive)
            .Select(x => x.ExternalSupplierId)
            .ToListAsync(cancellationToken);

            var externalSupplierIdSet = externalSupplierIds.ToHashSet();

            foreach (var supplierQuote in supplierQuotation.Suppliers)
            {
                supplierQuote.VerificationStatus = externalSupplierIdSet.Contains(supplierQuote.SupplierId)
                    ? Common.EXTERNAL_SUPPLIER
                    : verifiedSupplierIdsForRFQ.Contains(supplierQuote.SupplierId)
                        ? Common.VERIFIED_STATUS
                        : Common.UNVERIFIED_STATUS;
            }

            var externalSuppliers = await _repositorywrapper.ExternalSupplier
            .FindByCondition(x => externalSupplierIds.Contains(x.Id) && x.IsActive)
            .Select(x => new ExternalSupplierNameDto
            {
                ExternalSupplierId = x.Id,
                ExternalSupplierName = x.SupplierName,
                SupplierType = Common.EXTERNAL_SUPPLIER
            })
            .ToListAsync(cancellationToken);
            var verificationTemplateId = await _repositorywrapper.VerificationTemplate
    .FindByCondition(x => x.BuyerId == buyerId)
    .Select(x => x.Id)
    .FirstOrDefaultAsync(cancellationToken);

            List<InvitedUserDto>? invitedUsers = null;

            if (request.RoleId == Common.BUYER_ADMIN_ROLE_ID)
            {
                var invitedUserMappings = await _repositorywrapper.RFQOrganizationUserMapping
                    .FindByCondition(x => x.RFQId == request.RFQId && x.IsActive)
                    .ToListAsync(cancellationToken);

                if (invitedUserMappings.Any())
                {
                    var userIds = invitedUserMappings
                        .Select(x => x.UserId)
                        .Distinct()
                        .ToList();

                    var identityUsers = await _identityApiClient.GetUsersByIds(userIds, cancellationToken);

                    var invitedSupplierIds = invitedUserMappings
                        .Select(x => x.SupplierId)
                        .Distinct()
                        .ToList();

                    var supplierNamesById = new Dictionary<Guid, string?>();
                    foreach (var invitedSupplierId in invitedSupplierIds)
                    {
                        var supplierProfile = await _supplierApiClient.GetSupplierById(
                            invitedSupplierId,
                            cancellationToken);

                        supplierNamesById[invitedSupplierId] = supplierProfile?.BusinessProfile?.OrganizationName;
                    }

                    invitedUsers = invitedUserMappings
                        .Select(x =>
                        {
                            var identity = identityUsers.FirstOrDefault(u => u.UserId == x.UserId);
                            return new InvitedUserDto
                            {
                                RFQId = x.RFQId,
                                SupplierId = x.SupplierId,
                                SupplierName = supplierNamesById.GetValueOrDefault(x.SupplierId),
                                OrganizationId = x.OrganizationId,
                                UserId = x.UserId,
                                Name = identity?.Name,
                                Email = identity?.Email,
                                UserName = identity?.UserName
                            };
                        })
                        .ToList();
                }
            }

            return new GetRFQByIdDto
            {
                Title = rfq.Title,
                Description = rfq.Description,
                Department = rfq.Department,
                Region = rfq.Region,
                Currency = string.Empty,
                DeliveryLocation = rfq.DeliveryLocation,
                StartDate = rfq.StartDate,
                EndDate = rfq.EndDate,
                DeliveryTargetDate = rfq.DeliveryTargetDate,
                Budget = rfq.Budget,
                AddLotOption = rfq.AddLotOption,
                Status=rfq.Status,
                TechnicalSpecificationDocuments = technicalDocuments,
                TermsConditionDocuments = termsDocuments,
                ESignDocuments = esignDocuments,
                Questions = questions,
                Items = items,
                SupplierIds = suppliers,
                ExternalSupplierIds = externalSuppliers,
                RFQVerificationTemplateId = verificationTemplateId,
                SupplierQuotation = supplierQuotation?.Suppliers
    ?? new List<SupplierQuotationBySupplierDto>(),

                SupplierAnswers = supplierAnswers,
                InvitedUsers = invitedUsers,
                SupplierTermsConditions = supplierTermsConditions,
                SupplierESigns = supplierESigns
            };
        }
    }
}
