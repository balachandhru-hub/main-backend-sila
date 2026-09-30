using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;
using System.Security.Claims;
using SharedKernel.Dto;
using Buyer.Application.Features.Assets.Commands;
using Buyer.Domain.Common;
using Buyer.Application.Features.Commands.InviteSuppliers;
using Buyer.Application.Features.Commands.NotifyExternalSupplier;
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
        private readonly IIdentityApiClient _identityApiClient;


        public CreateRFQCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,

            IMediator mediator,
            ISupplierApiClient supplierApiClient,
            IIdentityApiClient identityApiClient
          )
        {
            _repository = repository;
            _logger = logger;

            _mediator = mediator;
            _supplierApiClient = supplierApiClient;
            _identityApiClient = identityApiClient;

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
                Currency = request.RFQ.Currency,
                SegmentId = request.RFQ.SegmentId,
                SegmentTitle = request.RFQ.SegmentTitle,
                FamilyId = request.RFQ.FamilyId,
                FamilyTitle = request.RFQ.FamilyTitle,
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
                _logger.LogInfo("Technical specification documents uploaded successfully.");
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
                _logger.LogInfo($"RFQ attachment mappings created successfully. Total Attachments: {attachments.Count}");
            }



            foreach (var question in request.RFQ.Questions)
            {
                _logger.LogInfo($"Creating {request.RFQ.Questions.Count} RFQ question(s).");
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

                if (question.Attachments != null)
                {
                    foreach (var attachment in question.Attachments)
                    {
                        Guid assetId = await _mediator.Send(
                            new UploadAssetCommand(attachment));

                        await _repository.RFQQuestionAttachmentMapping.CreateAsync(
                            new RFQQuestionAttachmentMapping
                            {
                                Id = Guid.NewGuid(),
                                RFQQuestionId = rfqQuestion.Id,
                                AssetId = assetId,
                                Type = attachment.AssetType
                            });
                    }
                }
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
                _logger.LogInfo($"Creating {request.RFQ.Items.Count} RFQ item(s).");
                int lineNumber = 1;
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
                        CostCenter = item.CostCenter,
                        LineNumber = lineNumber++
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
            // Validate invited suppliers & their invited users, then
            // save org-level and user-level invitation mappings
            // ======================================================

            var supplierIds = request.RFQ.SupplierInvites
                .Select(x => x.SupplierId)
                .ToList();

            var supplierOrganizationIds = new Dictionary<Guid, Guid>();

            foreach (var invite in request.RFQ.SupplierInvites)
            {
                Guid supplierOrganizationId = Guid.Empty;

                if (invite.UserIds != null && invite.UserIds.Any())
                {
                    var supplierProfile = await _supplierApiClient.GetSupplierById(
                        invite.SupplierId,
                        cancellationToken);

                    supplierOrganizationId = supplierProfile.OrganizationId;
                    supplierOrganizationIds[invite.SupplierId] = supplierOrganizationId;

                    var organizationUsers = await _identityApiClient.GetOrganizationUserRFQ(
                        supplierOrganizationId);

                    var validUserIds = organizationUsers
                        .Select(x => x.UserId)
                        .ToHashSet();

                    var invalidUserIds = invite.UserIds
                        .Where(x => !validUserIds.Contains(x))
                        .ToList();

                    if (invalidUserIds.Any())
                    {
                        _logger.LogError(
                            $"Invalid supplier user(s) for SupplierId {invite.SupplierId}: {string.Join(",", invalidUserIds)}");
                        throw new BadRequestCustomException(
                            "Invalid supplier user.",
                            $"The following users do not belong to supplier organization {supplierOrganizationId}: {string.Join(",", invalidUserIds)}");
                    }
                }

                await _repository.RFQSupplierMapping.CreateAsync(
                    new RFQSupplierMapping
                    {
                        Id = Guid.NewGuid(),
                        RFQId = rfq.Id,
                        RFQNumber = rfq.RFQNumber,
                        BuyerId = buyer.Id,
                        SupplierId = invite.SupplierId,
                        SupplierTermsAndConditionAccepted = Common.PENDING
                    });

                if (invite.UserIds != null)
                {
                    foreach (var userId in invite.UserIds)
                    {
                        await _repository.RFQOrganizationUserMapping.CreateAsync(
                            new RFQOrganizationUserMapping
                            {
                                Id = Guid.NewGuid(),
                                RFQId = rfq.Id,
                                RFQNumber = rfq.RFQNumber,
                                BuyerId = buyer.Id,
                                SupplierId = invite.SupplierId,
                                OrganizationId = supplierOrganizationId,
                                UserId = userId
                            });
                    }
                }

                _logger.LogInfo(
                    $"Supplier mapping completed successfully for SupplierId {invite.SupplierId}. Invited users: {invite.UserIds?.Count ?? 0}");
            }


            // ======================================================
            // Save all external (unregistered) suppliers - reuse an
            // existing ExternalSupplier by email instead of always
            // creating a new one, and never duplicate the RFQ mapping.
            // ======================================================

            var createdExternalSuppliers = new List<ExternalSupplier>();

            if (request.RFQ.ExternalSuppliers.Any())
            {
                var normalizedEmails = request.RFQ.ExternalSuppliers
                    .Select(x => x.Email.Trim().ToLower())
                    .Distinct()
                    .ToList();

                var existingExternalSuppliers = await _repository.ExternalSupplier
                    .FindByCondition(x =>
                        x.IsActive &&
                        normalizedEmails.Contains(x.Email.ToLower()))
                    .ToListAsync(cancellationToken);

                var externalSupplierByEmail = existingExternalSuppliers
                    .GroupBy(x => x.Email.Trim().ToLower())
                    .ToDictionary(g => g.Key, g => g.First());

                foreach (var externalSupplierDto in request.RFQ.ExternalSuppliers)
                {
                    var normalizedEmail = externalSupplierDto.Email.Trim();
                    var normalizedEmailKey = normalizedEmail.ToLower();

                    if (!externalSupplierByEmail.TryGetValue(normalizedEmailKey, out var externalSupplier))
                    {
                        externalSupplier = new ExternalSupplier
                        {
                            Id = Guid.NewGuid(),
                            SupplierName = externalSupplierDto.SupplierName,
                            Email = normalizedEmail,
                            PhoneNumber = externalSupplierDto.PhoneNumber,
                            Address = externalSupplierDto.Address
                        };

                        await _repository.ExternalSupplier.CreateAsync(externalSupplier);
                        externalSupplierByEmail[normalizedEmailKey] = externalSupplier;

                        _logger.LogInfo(
                            $"Created new external supplier. ExternalSupplierId: {externalSupplier.Id}, Email: {normalizedEmail}");
                    }
                    else
                    {
                        _logger.LogInfo(
                            $"Reusing existing external supplier. ExternalSupplierId: {externalSupplier.Id}, Email: {normalizedEmail}");
                    }

                    if (!createdExternalSuppliers.Any(x => x.Id == externalSupplier.Id))
                    {
                        createdExternalSuppliers.Add(externalSupplier);
                    }

                    var existingMapping = await _repository.RFQExternalSupplier
                        .FindByCondition(x =>
                            x.RFQId == rfq.Id &&
                            x.ExternalSupplierId == externalSupplier.Id &&
                            x.IsActive)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (existingMapping == null)
                    {
                        await _repository.RFQExternalSupplier.CreateAsync(
                            new RFQExternalSupplier
                            {
                                Id = Guid.NewGuid(),
                                RFQId = rfq.Id,
                                ExternalSupplierId = externalSupplier.Id,
                                Status = Common.EXTERNAL_SUPPLIER_INVITED_STATUS
                            });
                    }
                }

                _logger.LogInfo($"External supplier mapping completed successfully. Total External Suppliers: {createdExternalSuppliers.Count}");
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

            var unVerifiedSuppliers = supplierIds
                .Where(x => !verifiedSupplierIds.Contains(x))
                .ToList();

            // ExternalSuppliers have no BuyerSupplierMapping concept - they are always
            // "unverified" and get the same verification-template invitation (a
            // SupplierVerificationRequest row) as any other unverified supplier, keyed by
            // ExternalSupplier.Id stored in the same SupplierOrganizationId field.
            unVerifiedSuppliers.AddRange(createdExternalSuppliers.Select(x => x.Id));

            // ======================================================
            // Call InviteSuppliers only if required
            // ======================================================

            if (unVerifiedSuppliers.Any())
            {
                _logger.LogInfo($"Inviting {unVerifiedSuppliers.Count} unverified supplier(s) for RFQ: {rfq.RFQNumber}");
                await _mediator.Send(new InviteSuppliersCommand(
                new InviteSuppliersDto
                {
                    RFQId = rfq.Id,
                    RFQNumber = rfq.RFQNumber,
                    BuyerId = buyer.Id,
                    RFQVerificationTemplateId = request.RFQ.RFQVerificationTemplateId,
                    SupplierInvites = unVerifiedSuppliers,
                    EndDate = request.RFQ.EndDate,
                    TemplateId = request.RFQ.TemplateId
                }));
            }

            await _repository.SaveAsync();
            foreach (var invite in request.RFQ.SupplierInvites)
            {
                var supplierId = invite.SupplierId;

                var supplierRequest = new CreateSupplierRFQRequestDto
                {
                    BuyerRFQId = rfq.Id,
                    RFQNumber = rfq.RFQNumber,
                    BuyerId = buyer.Id,
                    SupplierId = supplierId,
                    OrganizationId = supplierOrganizationIds.TryGetValue(supplierId, out var orgId) ? orgId : Guid.Empty,
                    InvitedUserIds = invite.UserIds ?? new List<Guid>(),
                    BuyerName = buyer.OrganizationName,
                    Title = rfq.Title,
                    Description = rfq.Description,
                    StartDate = rfq.StartDate,
                    EndDate = rfq.EndDate,
                    DeliveryLocation = rfq.DeliveryLocation,
                    AddLotOption = rfq.AddLotOption,
                    Currency = rfq.Currency,
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
                            CostCenter = x.CostCenter,
                            LineNumber = x.LineNumber
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

            // ======================================================
            // Create Supplier RFQ (Supplier microservice) for each
            // external supplier, keyed by ExternalSupplierId so the
            // existing bidding infrastructure picks it up.
            // ======================================================

            var sessionTokensByExternalSupplierId = new Dictionary<Guid, string>();

            foreach (var externalSupplier in createdExternalSuppliers)
            {
                var sessionToken = Guid.NewGuid().ToString();
                sessionTokensByExternalSupplierId[externalSupplier.Id] = sessionToken;

                var supplierRequest = new CreateSupplierRFQRequestDto
                {
                    BuyerRFQId = rfq.Id,
                    RFQNumber = rfq.RFQNumber,
                    BuyerId = buyer.Id,
                    Currency = rfq.Currency,
                    SupplierId = externalSupplier.Id,
                    BuyerName = buyer.OrganizationName,
                    Title = rfq.Title,
                    Description = rfq.Description,
                    StartDate = rfq.StartDate,
                    EndDate = rfq.EndDate,
                    DeliveryLocation = rfq.DeliveryLocation,
                    AddLotOption = rfq.AddLotOption,
                    Status = rfq.Status,
                    SessionToken = sessionToken,
                    Items = createdItems.Select(x =>
                        new CreateSupplierRFQItemRequestDto
                        {
                            BuyerRFQItemId = x.Id,
                            Description = x.Description,
                            Quantity = x.Quantity,
                            UOM = x.UOM,
                            MaterialCode = x.MaterialCode,
                            MaterialGroup = x.MaterialGroup,
                            CostCenter = x.CostCenter,
                            LineNumber = x.LineNumber
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
                        $"Failed to create Supplier RFQ for External Supplier {externalSupplier.Id}. Error: {ex.Message}");
                    throw new BadRequestCustomException(
                        "Unable to create Supplier RFQ.",
                        $"Failed to create Supplier RFQ for External Supplier {externalSupplier.Id}. Error: {ex.Message}");
                }
            }

            // ======================================================
            // Notify external suppliers by email, only after the RFQ
            // and their Supplier RFQ records have been created
            // successfully.
            // ======================================================

            foreach (var externalSupplier in createdExternalSuppliers)
            {
                await _mediator.Send(
                    new NotifyExternalSupplierCommand(
                        rfq.Id,
                        externalSupplier.Id,
                        sessionTokensByExternalSupplierId[externalSupplier.Id]),
                    cancellationToken);
            }

            _logger.LogInfo($"RFQ created successfully. RFQ Id : {rfq.Id}");

            return rfq.Id;
        }
    }
}