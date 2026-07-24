using Buyer.Domain.Common;
using Buyer.Domain.Dtos;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Dto;
using SharedKernel.LoggerServices;
using SharedKernel.ExceptionHandler;

namespace Buyer.Application.Features.Queries.GetAllRFQ
{
    public class GetRFQByIdQueryHandler : IRequestHandler<GetRFQByIdQuery, GetRFQByIdDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILoggerManager _logger;

        public GetRFQByIdQueryHandler(IRepositoryWrapper repository, IHttpContextAccessor httpContextAccessor, ILoggerManager logger)
        {
            _repository = repository;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<GetRFQByIdDto> Handle(
            GetRFQByIdQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching RFQ details for RFQId: {request.RFQId}");


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

            var questions = await _repository.RFQQuestion
                .FindByCondition(x => x.RFQId == request.RFQId)
                .OrderBy(x => x.DisplayOrder)
                .Select(x => new RFQQuestionDto
                {
                    Question = x.Question,
                    QuestionType = x.QuestionType,
                    IsRequired = x.IsRequired,
                    DisplayOrder = x.DisplayOrder,
                    Options = new List<string>()
                })
                .ToListAsync(cancellationToken);

            var rfqItems = await _repository.RFQItem
     .FindByCondition(x => x.RFQId == request.RFQId)
     .ToListAsync(cancellationToken);

            var items = new List<GetRFQItemDto>();

            foreach (var item in rfqItems)
            {
                var attachments = await (
                    from mapping in _repository.RFQItemAttachmentMapping.FindByCondition(x =>
                        x.RFQItemId == item.Id &&
                        x.Type == Common.RFQ_ITEM_ATTACHMENT)

                    join asset in _repository.Asset.FindByCondition(x => x.IsActive)
                        on mapping.AssetId equals asset.Id

                    select new AssetDto
                    {
                        Id = asset.Id,
                        AssetType = asset.AssetType.ToString(),
                        AssetName = asset.AssetName,
                        FileType = asset.FileType.ToString(),
                        FileName = asset.FileName
                    })
                    .ToListAsync(cancellationToken);

                items.Add(new GetRFQItemDto
                {
                    Description = item.Description,
                    Quantity = item.Quantity,
                    UOM = item.UOM,
                    MaterialCode = item.MaterialCode,
                    MaterialGroup = item.MaterialGroup,
                    CostCenter = item.CostCenter,
                    Attachments = attachments
                });
            }

            // Load these from your respective tables if available
            var technicalDocuments = await (
              from mapping in _repository.RFQAttachmentMapping.FindByCondition(x =>
                  x.RFQId == request.RFQId &&
                  x.Type == Common.TECHNICAL_SPECIFICATION)

              join asset in _repository.Asset.FindByCondition(x => x.IsActive)
                  on mapping.AssetId equals asset.Id

              select new AssetDto
              {
                  Id = asset.Id,
                  AssetType = asset.AssetType.ToString(),
                  AssetName = asset.AssetName,
                  FileType = asset.FileType.ToString(),
                  FileName = asset.FileName
              })
              .ToListAsync(cancellationToken);

            var termsDocuments = await (
    from mapping in _repository.RFQAttachmentMapping.FindByCondition(x =>
        x.RFQId == request.RFQId &&
        x.Type == Common.TERMS_CONDITION)

    join asset in _repository.Asset.FindByCondition(x => x.IsActive)
        on mapping.AssetId equals asset.Id

    select new AssetDto
    {
        Id = asset.Id,
        AssetType = asset.AssetType.ToString(),
        AssetName = asset.AssetName,
        FileType = asset.FileType.ToString(),
        FileName = asset.FileName
    })
    .ToListAsync(cancellationToken);
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
                RFQVerificationTemplateId = verificationTemplateId
            };
        }
    }
}