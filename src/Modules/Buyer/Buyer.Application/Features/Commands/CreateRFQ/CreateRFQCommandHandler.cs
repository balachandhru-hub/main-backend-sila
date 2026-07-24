using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;
using System.Security.Claims;
using SharedKernel.Dto;
using Buyer.Application.Features.Assets.Commands;
using Buyer.Domain.Common;
using Buyer.Application.Features.Commands.InviteSuppliers;
using Buyer.Domain.Dto;
using Buyer.Application.Contracts;

namespace Buyer.Application.Features.Commands.CreateRFQ
{
    public class CreateRFQCommandHandler
        : IRequestHandler<CreateRFQCommand, Guid>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        private readonly IMediator _mediator;
        private readonly ISupplierApiClient _supplierApiClient;


        public CreateRFQCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,

            IMediator mediator,
            ISupplierApiClient supplierApiClient
          )
        {
            _repository = repository;
            _logger = logger;

            _mediator = mediator;
            _supplierApiClient = supplierApiClient;
      
        }

        public async Task<Guid> Handle(
    CreateRFQCommand request,
    CancellationToken cancellationToken)
        {


            _logger.LogInfo(
                $"Create RFQ process started. OrganizationId: {request.OrganizationId}");

            var buyer = _repository.BuyerBusinessProfile
        .FindFirstByCondition(x =>
            x.OrganizationId == request.OrganizationId &&
            x.IsActive);

            if (buyer == null)
            {
                _logger.LogError($"Buyer not found for OrganizationId: {request.OrganizationId}");
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
                _logger.LogError(
    $"Duplicate RFQ creation attempted. BuyerId: {buyer.Id}, Department: {request.RFQ.Department}");
                throw new BadRequestCustomException(
    "Department already exists.",
    "The selected Department already exists for this buyer.");
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
            _logger.LogInfo(
                $"RFQ entity created. RFQ Id: {rfq.Id}, RFQ Number: {rfq.RFQNumber}");


            List<RFQAttachmentMapping> attachments = new();

            // Technical Specification Documents
            if (request.RFQ.TechnicalSpecificationDocuments != null)
            {
                _logger.LogInfo(
        $"Uploading {request.RFQ.TechnicalSpecificationDocuments.Count} technical specification document(s).");
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
                _logger.LogInfo(
   "Technical specification documents uploaded successfully.");
            }

            // Terms & Conditions Documents
            if (request.RFQ.TermsConditionDocuments != null)
            {
                _logger.LogInfo(
        $"Uploading {request.RFQ.TermsConditionDocuments.Count} Terms & Conditions document(s).");
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
                _logger.LogInfo(
    $"RFQ attachment mappings created successfully. Total Attachments: {attachments.Count}");
            }



            foreach (var question in request.RFQ.Questions)
            {
                _logger.LogInfo(
    $"Creating {request.RFQ.Questions.Count} RFQ question(s).");
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
            _logger.LogInfo("RFQ questions created successfully.");
            var createdItems = new List<RFQItem>();
            if (request.RFQ.Items != null)
            {
                _logger.LogInfo(
        $"Creating {request.RFQ.Items.Count} RFQ item(s).");
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
                    createdItems.Add(rfqItem);

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
                _logger.LogInfo("RFQ items created successfully.");

            }

            // ======================================================
            // Save all invited suppliers
            // ======================================================

            foreach (var supplierId in request.RFQ.SupplierIds)
            {
                await _repository.RFQSupplierMapping.CreateAsync(
                    new RFQSupplierMapping
                    {
                        Id = Guid.NewGuid(),
                        RFQId = rfq.Id,
                        RFQNumber = rfq.RFQNumber,
                        BuyerId = buyer.Id,
                        SupplierId = supplierId
                    });
                _logger.LogInfo(
$"Supplier mapping completed successfully. Total Suppliers: {request.RFQ.SupplierIds.Count}");
            }



            // ======================================================
            // Get verified suppliers
            // ======================================================

            var verifiedSupplierIds = _repository.BuyerSupplierMapping
                .FindByCondition(x =>
                    x.BuyerId == buyer.Id &&
                    x.IsActive)
                .Select(x => x.SupplierId)
                .ToList();

            // ======================================================
            // Find unverified suppliers
            // ======================================================

            var unVerifiedSuppliers = request.RFQ.SupplierIds
                .Where(x => !verifiedSupplierIds.Contains(x))
                .ToList();

            // ======================================================
            // Call InviteSuppliers only if required
            // ======================================================

            if (unVerifiedSuppliers.Any())
            {
                _logger.LogInfo(
$"Inviting {unVerifiedSuppliers.Count} unverified supplier(s) for RFQ: {rfq.RFQNumber}");
                await _mediator.Send(
    new InviteSuppliersCommand(
        new InviteSuppliersDto
        {
            RFQId = rfq.Id,
            RFQNumber = rfq.RFQNumber,
            BuyerOrganizationId = buyer.OrganizationId,
            RFQVerificationTemplateId = request.RFQ.RFQVerificationTemplateId,
            SupplierInvites = unVerifiedSuppliers
        }));
            }
        
            await _repository.SaveAsync();
            foreach (var supplierId in request.RFQ.SupplierIds)
            {
                var supplierRequest = new CreateSupplierRFQRequestDto
                {
                    BuyerRFQId = rfq.Id,
                    RFQNumber = rfq.RFQNumber,

                    BuyerId = buyer.Id,
                    SupplierId = supplierId,

                    BuyerName = buyer.OrganizationName,

                    Title = rfq.Title,
                    Description = rfq.Description,

                    StartDate = rfq.StartDate,
                    EndDate = rfq.EndDate,

                    AddLotOption = rfq.AddLotOption,
                    Status = rfq.Status,

                    Items = createdItems.Select(x =>
                        new CreateSupplierRFQItemRequestDto
                        {
                            BuyerRFQItemId = x.Id,
                            Description = x.Description,
                            Quantity = x.Quantity,
                            UOM = x.UOM,
                            MaterialCode = x.MaterialCode,
                            MaterialGroup = x.MaterialGroup,
                            CostCenter = x.CostCenter
                        }).ToList()
                };

                try
                {
                    await _supplierApiClient.CreateSupplierRFQ(
                        supplierRequest,
                     
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        $"Failed to create Supplier RFQ for Supplier {supplierId}. Error: {ex.Message}");
                        throw new BadRequestCustomException(
                            "Unable to create Supplier RFQ.",
                            $"Failed to create Supplier RFQ for Supplier {supplierId}. Error: {ex.Message}");
                }
            }
            
            _logger.LogInfo($"RFQ created successfully. RFQ Id : {rfq.Id}");

            return rfq.Id;
        }
    }
}