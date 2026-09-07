using Buyer.Domain.Entities;
using Buyer.Domain.Dto;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Application.Contracts;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.UpdateRFQ
{
    public class UpdateRFQCommandHandler
        : IRequestHandler<UpdateRFQCommand, Guid>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly ISupplierApiClient _supplierApiClient;
        private readonly IIdentityApiClient _identityApiClient;

        public UpdateRFQCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            ISupplierApiClient supplierApiClient,
            IIdentityApiClient identityApiClient)
        {
            _repository = repository;
            _logger = logger;
            _supplierApiClient = supplierApiClient;
            _identityApiClient = identityApiClient;
        }

        public async Task<Guid> Handle(
            UpdateRFQCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo(
                $"Update RFQ process started. RFQId: {request.RFQId}, OrganizationId: {request.OrganizationId}");

            // ============================================================
            // 1. GET BUYER
            // ============================================================

            var buyer = _repository.BuyerBusinessProfile
                .FindFirstByCondition(x =>
                    x.OrganizationId == request.OrganizationId &&
                    x.IsActive);

            if (buyer == null)
            {
                _logger.LogError(
                    $"Buyer not found. OrganizationId: {request.OrganizationId}");

                throw new NotFoundCustomException(
                    "Buyer not found.",
                    "Buyer does not exist.");
            }

            // ============================================================
            // 2. GET RFQ
            // ============================================================

            var rfq = _repository.RFQ
                .FindFirstByCondition(x =>
                    x.Id == request.RFQId &&
                    x.BuyerId == buyer.Id);

            if (rfq == null)
            {
                _logger.LogError(
                    $"RFQ not found. RFQId: {request.RFQId}");

                throw new NotFoundCustomException(
                    "RFQ not found.",
                    "The requested RFQ does not exist or does not belong to this buyer.");
            }

            // ============================================================
            // 3. UPDATE RFQ BASIC DETAILS
            // ============================================================
 

            rfq.Description = request.RFQ.Description;
            rfq.Department = request.RFQ.Department;
            rfq.Region = request.RFQ.Region;
            rfq.DeliveryLocation = request.RFQ.DeliveryLocation;
            rfq.DeliveryTargetDate = request.RFQ.DeliveryTargetDate;
            rfq.Budget = request.RFQ.Budget;
            rfq.AddLotOption = request.RFQ.AddLotOption;

            _repository.RFQ.Update(rfq);

            // ============================================================
            // 4. UPDATE RFQ ITEMS
            // ============================================================

            var existingItems = _repository.RFQItem
                .FindByCondition(x => x.RFQId == rfq.Id)
                .ToList();

            var requestItemIds = request.RFQ.Items
                .Where(x => x.Id.HasValue)
                .Select(x => x.Id!.Value)
                .ToHashSet();

            // ------------------------------------------------------------
            // DELETE ITEMS WHICH ARE NOT IN REQUEST
            // ------------------------------------------------------------

            var itemsToDelete = existingItems
                .Where(x => !requestItemIds.Contains(x.Id))
                .ToList();

            foreach (var item in itemsToDelete)
            {
                _repository.RFQItem.Delete(item);
            }

            // ------------------------------------------------------------
            // UPDATE EXISTING / CREATE NEW ITEMS
            // ------------------------------------------------------------

            foreach (var itemDto in request.RFQ.Items)
            {
                if (itemDto.Id.HasValue)
                {
                    var existingItem = existingItems
                        .FirstOrDefault(x => x.Id == itemDto.Id.Value);

                    if (existingItem == null)
                    {
                        throw new BadRequestCustomException(
                            "Invalid RFQ item.",
                            $"RFQ item {itemDto.Id} does not belong to RFQ {rfq.RFQNumber}.");
                    }

                    existingItem.Description = itemDto.Description;
                    existingItem.Quantity = itemDto.Quantity;
                    existingItem.UOM = itemDto.UOM;
                    existingItem.MaterialCode = itemDto.MaterialCode;
                    existingItem.MaterialGroup = itemDto.MaterialGroup;
                    existingItem.CostCenter = itemDto.CostCenter;

                    _repository.RFQItem.Update(existingItem);
                }
                else
                {
                    var newItem = new RFQItem
                    {
                        Id = Guid.NewGuid(),
                        RFQId = rfq.Id,
                        Description = itemDto.Description,
                        Quantity = itemDto.Quantity,
                        UOM = itemDto.UOM,
                        MaterialCode = itemDto.MaterialCode,
                        MaterialGroup = itemDto.MaterialGroup,
                        CostCenter = itemDto.CostCenter
                    };

                    await _repository.RFQItem.CreateAsync(newItem);
                }
            }

            _logger.LogInfo(
                $"RFQ items updated successfully. RFQId: {rfq.Id}");

            // ============================================================
            // 5. UPDATE RFQ QUESTIONS
            // ============================================================

            var existingQuestions = _repository.RFQQuestion
                .FindByCondition(x => x.RFQId == rfq.Id)
                .ToList();

            var requestQuestionIds = request.RFQ.Questions
                .Where(x => x.Id.HasValue)
                .Select(x => x.Id!.Value)
                .ToHashSet();

            // ------------------------------------------------------------
            // DELETE QUESTIONS WHICH ARE NOT IN REQUEST
            // AND DELETE THEIR OPTIONS
            // ------------------------------------------------------------

            var questionsToDelete = existingQuestions
                .Where(x => !requestQuestionIds.Contains(x.Id))
                .ToList();

            foreach (var question in questionsToDelete)
            {
                // Get options belonging to this question
                var optionsToDelete = _repository.RFQQuestionOption
                    .FindByCondition(x =>
                        x.RFQQuestionId == question.Id)
                    .ToList();

                // Delete all options first
                foreach (var option in optionsToDelete)
                {
                    _repository.RFQQuestionOption.Delete(option);
                }

                // Delete question
                _repository.RFQQuestion.Delete(question);

                _logger.LogInfo(
                    $"Question deleted. QuestionId: {question.Id}");
            }

            // ============================================================
            // 6. UPDATE / CREATE QUESTIONS
            // ============================================================

            foreach (var questionDto in request.RFQ.Questions)
            {
                RFQQuestion question;

                // ========================================================
                // EXISTING QUESTION
                // ========================================================

                if (questionDto.Id.HasValue)
                {
                    question = existingQuestions
                        .FirstOrDefault(x =>
                            x.Id == questionDto.Id.Value);

                    if (question == null)
                    {
                        throw new BadRequestCustomException(
                            "Invalid RFQ question.",
                            $"RFQ question {questionDto.Id} does not belong to RFQ {rfq.RFQNumber}.");
                    }

                    // Update question fields
                    question.Question = questionDto.Question;
                    question.QuestionType = questionDto.QuestionType;
                    question.IsRequired = questionDto.IsRequired;
                    question.DisplayOrder = questionDto.DisplayOrder;

                    _repository.RFQQuestion.Update(question);

                    // ====================================================
                    // UPDATE OPTIONS
                    // ====================================================

                    var existingOptions = _repository.RFQQuestionOption
                        .FindByCondition(x =>
                            x.RFQQuestionId == question.Id)
                        .ToList();

                    var requestOptionIds = questionDto.Options
                        .Where(x => x.Id.HasValue)
                        .Select(x => x.Id!.Value)
                        .ToHashSet();

                    // ----------------------------------------------------
                    // DELETE OPTIONS NOT PRESENT IN REQUEST
                    // ----------------------------------------------------

                    var optionsToDelete = existingOptions
                        .Where(x =>
                            !requestOptionIds.Contains(x.Id))
                        .ToList();

                    foreach (var option in optionsToDelete)
                    {
                        _repository.RFQQuestionOption.Delete(option);

                        _logger.LogInfo(
                            $"Option deleted. OptionId: {option.Id}, QuestionId: {question.Id}");
                    }

                    // ----------------------------------------------------
                    // UPDATE EXISTING / CREATE NEW OPTIONS
                    // ----------------------------------------------------

                    foreach (var optionDto in questionDto.Options)
                    {
                        if (optionDto.Id.HasValue)
                        {
                            // ============================================
                            // EXISTING OPTION
                            // ============================================

                            var existingOption = existingOptions
                                .FirstOrDefault(x =>
                                    x.Id == optionDto.Id.Value);

                            if (existingOption == null)
                            {
                                throw new BadRequestCustomException(
                                    "Invalid question option.",
                                    $"Option {optionDto.Id} does not belong to question {question.Id}.");
                            }

                            existingOption.OptionText =
                                optionDto.OptionText;

                            existingOption.DisplayOrder =
                                optionDto.DisplayOrder;

                            _repository.RFQQuestionOption
                                .Update(existingOption);

                            _logger.LogInfo(
                                $"Option updated. OptionId: {existingOption.Id}");
                        }
                        else
                        {
                            // ============================================
                            // NEW OPTION
                            // ============================================

                            var newOption = new RFQQuestionOption
                            {
                                Id = Guid.NewGuid(),
                                RFQQuestionId = question.Id,
                                OptionText = optionDto.OptionText,
                                DisplayOrder = optionDto.DisplayOrder
                            };

                            await _repository.RFQQuestionOption
                                .CreateAsync(newOption);

                            _logger.LogInfo(
                                $"New option created. OptionId: {newOption.Id}");
                        }
                    }
                }
                else
                {
                    // ====================================================
                    // NEW QUESTION
                    // ====================================================

                    question = new RFQQuestion
                    {
                        Id = Guid.NewGuid(),
                        RFQId = rfq.Id,
                        RFQNumber = rfq.RFQNumber,
                        Question = questionDto.Question,
                        QuestionType = questionDto.QuestionType,
                        IsRequired = questionDto.IsRequired,
                        DisplayOrder = questionDto.DisplayOrder
                    };

                    await _repository.RFQQuestion
                        .CreateAsync(question);

                    _logger.LogInfo(
                        $"New question created. QuestionId: {question.Id}");

                    // ====================================================
                    // CREATE OPTIONS FOR NEW QUESTION
                    // ====================================================

                    if (questionDto.Options != null)
                    {
                        foreach (var optionDto in questionDto.Options)
                        {
                            var newOption = new RFQQuestionOption
                            {
                                Id = Guid.NewGuid(),
                                RFQQuestionId = question.Id,
                                OptionText = optionDto.OptionText,
                                DisplayOrder = optionDto.DisplayOrder
                            };

                            await _repository.RFQQuestionOption
                                .CreateAsync(newOption);

                            _logger.LogInfo(
                                $"New option created. OptionId: {newOption.Id}");
                        }
                    }
                }
            }

            // ============================================================
            // 7. UPDATE SUPPLIER MAPPINGS
            // ============================================================

            var existingSupplierMappings =
                _repository.RFQSupplierMapping
                    .FindByCondition(x => x.RFQId == rfq.Id)
                    .ToList();

            var requestedSupplierIds =
                request.RFQ.SupplierInvites
                    .Select(x => x.SupplierId)
                    .ToHashSet();

            // ------------------------------------------------------------
            // DELETE REMOVED SUPPLIERS
            // ------------------------------------------------------------

            var suppliersToRemove = existingSupplierMappings
                .Where(x =>
                    !requestedSupplierIds.Contains(x.SupplierId))
                .ToList();

            foreach (var mapping in suppliersToRemove)
            {
                _repository.RFQSupplierMapping.Delete(mapping);
            }

            // ------------------------------------------------------------
            // ADD NEW SUPPLIERS
            // ------------------------------------------------------------

            var existingSupplierIds = existingSupplierMappings
                .Select(x => x.SupplierId)
                .ToHashSet();

            foreach (var supplierId in requestedSupplierIds)
            {
                if (!existingSupplierIds.Contains(supplierId))
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
                }
            }

            _logger.LogInfo(
                $"RFQ supplier mappings updated. RFQId: {rfq.Id}");

            // ============================================================
            // 7b. UPDATE USER-LEVEL INVITATIONS
            // ============================================================

            var existingUserMappings = _repository.RFQOrganizationUserMapping
                .FindByCondition(x => x.RFQId == rfq.Id)
                .ToList();

            var requestedUserMappings = new HashSet<(Guid SupplierId, Guid UserId)>();
            var supplierOrganizationIds = new Dictionary<Guid, Guid>();

            foreach (var invite in request.RFQ.SupplierInvites)
            {
                if (invite.UserIds == null || !invite.UserIds.Any())
                {
                    continue;
                }

                var supplierProfile = await _supplierApiClient.GetSupplierById(
                    invite.SupplierId,
                    cancellationToken);

                supplierOrganizationIds[invite.SupplierId] = supplierProfile.OrganizationId;

                var organizationUsers = await _identityApiClient.GetOrganizationUsers(
                    supplierProfile.OrganizationId);

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
                        $"The following users do not belong to supplier organization {supplierProfile.OrganizationId}: {string.Join(",", invalidUserIds)}");
                }

                foreach (var userId in invite.UserIds)
                {
                    requestedUserMappings.Add((invite.SupplierId, userId));
                }
            }

            var userMappingsToRemove = existingUserMappings
                .Where(x => !requestedUserMappings.Contains((x.SupplierId, x.UserId)))
                .ToList();

            foreach (var mapping in userMappingsToRemove)
            {
                _repository.RFQOrganizationUserMapping.Delete(mapping);
            }

            var existingUserMappingKeys = existingUserMappings
                .Select(x => (x.SupplierId, x.UserId))
                .ToHashSet();

            foreach (var (supplierId, userId) in requestedUserMappings)
            {
                if (!existingUserMappingKeys.Contains((supplierId, userId)))
                {
                    await _repository.RFQOrganizationUserMapping.CreateAsync(
                        new RFQOrganizationUserMapping
                        {
                            Id = Guid.NewGuid(),
                            RFQId = rfq.Id,
                            RFQNumber = rfq.RFQNumber,
                            BuyerId = buyer.Id,
                            SupplierId = supplierId,
                            OrganizationId = supplierOrganizationIds.TryGetValue(supplierId, out var orgId) ? orgId : Guid.Empty,
                            UserId = userId
                        });
                }
            }

            _logger.LogInfo(
                $"RFQ user-level invitation mappings updated. RFQId: {rfq.Id}");

            // ============================================================
            // 8. SAVE CHANGES
            // ============================================================

            await _repository.SaveAsync();

            _logger.LogInfo(
                $"RFQ updated successfully. RFQId: {rfq.Id}, RFQNumber: {rfq.RFQNumber}");

            return rfq.Id;
        }
    }
}