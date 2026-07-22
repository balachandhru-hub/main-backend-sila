using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.AspNetCore.Http;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;
using System.Security.Claims;
using SharedKernel.Dto;
using Buyer.Application.Features.Assets.Commands;
using Buyer.Domain.Common;

namespace Buyer.Application.Features.Commands.CreateRFQ
{
    public class CreateRFQCommandHandler
        : IRequestHandler<CreateRFQCommand, bool>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMediator _mediator;

        public CreateRFQCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            IHttpContextAccessor httpContextAccessor,
            IMediator mediator)
        {
            _repository = repository;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _mediator = mediator;
        }

        public async Task<bool> Handle(
     CreateRFQCommand request,
     CancellationToken cancellationToken)
        {
            var organizationIdClaim = _httpContextAccessor.HttpContext?
                .User
                .FindFirst("OrganizationId")?.Value;

            if (string.IsNullOrWhiteSpace(organizationIdClaim))
            {
                throw new UnAuthorizedCustomException(
                    "Unauthorized.",
                    "Organization claim not found.");
            }

            var organizationId = Guid.Parse(organizationIdClaim);

            var buyer = _repository.BuyerBusinessProfile
                .FindFirstByCondition(x =>
                    x.OrganizationId == organizationId &&
                    x.IsActive);

            if (buyer == null)
            {
                throw new NotFoundCustomException(
                    "Buyer not found.",
                    "Buyer does not exist.");
            }

            var existingRFQ = _repository.RFQ
                .FindFirstByCondition(x =>
                    x.BuyerId == buyer.Id &&
                    x.Department == request.RFQ.Department &&
                    x.IsActive);

            if (existingRFQ != null)
            {
                throw new BadRequestCustomException(
                    "Department and Cost Center already exists.",
                    "The selected Department and Cost Center combination already exists for this buyer.");
            }

            var rfqNumber = $"RFQ-{DateTime.UtcNow:yyyyMMddHHmmss}";

            var rfq = new RFQ
            {
                Id = Guid.NewGuid(),
                RFQNumber = rfqNumber,

                BuyerId = buyer.Id,

                Title = request.RFQ.Title,
                Description = request.RFQ.Description,

                Department = request.RFQ.Department,
               

                Region = request.RFQ.Region,
                DeliveryLocation = request.RFQ.DeliveryLocation,

                StartDate = request.RFQ.StartDate,
                EndDate = request.RFQ.EndDate,
                DeliveryTargetDate = request.RFQ.DeliveryTargetDate,

                Budget = request.RFQ.Budget,
                AddLotOption = request.RFQ.AddLotOption,

                Status = Common.RFQ_OPEN_STATUS,

               
            };

            _repository.RFQ.Create(rfq);

            await _repository.SaveAsync();

            List<RFQAttachmentMapping> attachments = new();

            // Technical Specification Documents
            if (request.RFQ.TechnicalSpecificationDocuments != null)
            {
                foreach (AssetUploadDto document in request.RFQ.TechnicalSpecificationDocuments)
                {
                    

                    Guid assetId = await _mediator.Send(new UploadAssetCommand(document));

                    attachments.Add(new RFQAttachmentMapping
                    {
                        Id = Guid.NewGuid(),
                        RFQId = rfq.Id,
                        AssetId = assetId,
                        Type = Common.TECHNICAL_SPECIFICATION
                    });
                }
            }

            // Terms & Conditions Documents
            if (request.RFQ.TermsConditionDocuments != null)
            {
                foreach (AssetUploadDto document in request.RFQ.TermsConditionDocuments)
                {
                   

                    Guid assetId = await _mediator.Send(new UploadAssetCommand(document));

                    attachments.Add(new RFQAttachmentMapping
                    {
                        Id = Guid.NewGuid(),
                        RFQId = rfq.Id,
                        AssetId = assetId,
                        Type = Common.TERMS_CONDITION
                    });
                }
            }

            if (attachments.Any())
            {
                await _repository.RFQAttachmentMapping.CreateRangeAsync(attachments);
            }

            await _repository.SaveAsync();

            foreach (var question in request.RFQ.Questions)
            {
                var rfqQuestion = new RFQQuestion
                {
                    Id = Guid.NewGuid(),
                    RFQId = rfq.Id,
                    RFQNumber = rfq.RFQNumber,
                    Question = question.Question,
                    QuestionType = question.QuestionType,
                    IsRequired = question.IsRequired,
                    DisplayOrder = question.DisplayOrder
                };

                await _repository.RFQQuestion.CreateAsync(rfqQuestion);

                if (question.Options != null)
                {
                    int order = Common.DISPLAY_ORDER;

                    foreach (var option in question.Options)
                    {
                        await _repository.RFQQuestionOption.CreateAsync(new RFQQuestionOption
                        {
                            Id = Guid.NewGuid(),
                            RFQQuestionId = rfqQuestion.Id,
                            OptionText = option,
                            DisplayOrder = order++
                        });
                    }
                }
            }
            await _repository.SaveAsync();
            if (request.RFQ.Items != null)
{
    foreach (var item in request.RFQ.Items)
    {
        var rfqItem = new RFQItem
        {
            Id = Guid.NewGuid(),
            RFQId = rfq.Id,
            Description = item.Description,
            Quantity = item.Quantity,
            UOM = item.UOM,
            MaterialCode = item.MaterialCode,
            MaterialGroup = item.MaterialGroup,
             CostCenter = item.CostCenter
        };

        await _repository.RFQItem.CreateAsync(rfqItem);

        if (item.Attachments != null)
        {
            foreach (var attachment in item.Attachments)
            {
                Guid assetId = await _mediator.Send(
                    new UploadAssetCommand(attachment));

                await _repository.RFQItemAttachmentMapping.CreateAsync(
                    new RFQItemAttachmentMapping
                    {
                        Id = Guid.NewGuid(),
                        RFQItemId = rfqItem.Id,
                        AssetId = assetId,
                        Type = attachment.AssetType
                    });
            }
        }
    }

    await _repository.SaveAsync();
}
            _logger.LogInfo($"RFQ created successfully. RFQ Id : {rfq.Id}");

            return true;
        }
    }
}