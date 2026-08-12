    using Buyer.Domain.Common;
    using Buyer.Domain.Dtos;
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
            private readonly IRepositoryWrapper _repository;
            private readonly IMetadataApiClient _metadataClient;
            private readonly ILoggerManager _logger;
            private readonly ISupplierApiClient _supplierApiClient;

            public GetRFQByIdQueryHandler(IRepositoryWrapper repository, IMetadataApiClient metadataApiClient, ILoggerManager logger, ISupplierApiClient supplierApiClient)
            {
                _repository = repository;
                _metadataClient = metadataApiClient;
                _logger = logger;
                _supplierApiClient = supplierApiClient;
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


                var rfq = await _repository.RFQ
                    .FindByCondition(x => x.Id == request.RFQId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (rfq == null)
                {
                    throw new NotFoundCustomException(
                        "RFQ not found.",
                        $"No RFQ exists with RFQId: {request.RFQId}."
                    );
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
                var questions = await _repository.RFQQuestion
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

                var questionOptions = await _repository.RFQQuestionOption
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


    foreach (var supplier in supplierAnswers.SupplierAnswers)
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

                var rfqItems = await _repository.RFQItem
        .FindByCondition(x => x.RFQId == request.RFQId)
        .ToListAsync(cancellationToken);

                var items = new List<GetRFQItemDto>();

                foreach (var item in rfqItems)
                {
                    var attachmentData = await (
                    from mapping in _repository.RFQItemAttachmentMapping.FindByCondition(x =>
                        x.RFQItemId == item.Id &&
                        x.Type == Common.RFQ_ITEM_ATTACHMENT)

                    join asset in _repository.Asset.FindByCondition(x => x.IsActive)
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

                    items.Add(new GetRFQItemDto
                    {
                        Description = item.Description,
                        Quantity = item.Quantity,
                        UOM = item.UOM,
                        MaterialCode = item.MaterialCode,
                        MaterialGroup = item.MaterialGroup,
                        CostCenter = item.CostCenter,
                        Attachments = attachments,


                    });
                }

                var technicalAssetData = await (
                    from mapping in _repository.RFQAttachmentMapping.FindByCondition(x =>
                        x.RFQId == request.RFQId &&
                        x.Type == Common.TECHNICAL_SPECIFICATION)

                    join asset in _repository.Asset.FindByCondition(x => x.IsActive)
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

                var termsAssetData = await (from mapping in _repository.RFQAttachmentMapping.FindByCondition(x =>
                    x.RFQId == request.RFQId &&
                    x.Type == Common.TERMS_CONDITION)

                                            join asset in _repository.Asset.FindByCondition(x => x.IsActive)
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
                var supplierIds = await _repository.BuyerSupplierMapping
                .FindByCondition(x => x.BuyerId == buyerId)
                .Select(x => x.SupplierId)
                .ToListAsync(cancellationToken);
                var verificationTemplateId = await _repository.VerificationTemplate
        .FindByCondition(x => x.BuyerId == buyerId)
        .Select(x => x.Id)
        .FirstOrDefaultAsync(cancellationToken);

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

                    TechnicalSpecificationDocuments = technicalDocuments,
                    TermsConditionDocuments = termsDocuments,
                    Questions = questions,
                    Items = items,
                    SupplierIds = supplierIds,
                    RFQVerificationTemplateId = verificationTemplateId,
                    SupplierQuotation = supplierQuotation == null
            ? new List<GetAllSupplierQuotationDto>()
            : new List<GetAllSupplierQuotationDto>
            {
                new GetAllSupplierQuotationDto
                {
                    TotalPrice = supplierQuotation.TotalPrice,
                    DeliveryCharge = supplierQuotation.DeliveryCharge,
                    Tax = supplierQuotation.Tax,
                    Discount = supplierQuotation.Discount,
                    DeliveryType = supplierQuotation.DeliveryType,
                    Status = supplierQuotation.Status,
                    QuotationId=supplierQuotation.QuotationId
                }
            },

                    SupplierQuotationItems = supplierQuotation?.SupplierQuotationItems
            ?? new List<SupplierQuotationItemDto>(),
                    SupplierAnswers = supplierAnswers
                };
            }
        }
    }
