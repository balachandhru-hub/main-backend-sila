using Buyer.Domain.Common;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.PredefinedMaterialMaster
{
    public class CreatePredefinedMaterialCommandHandler
        : IRequestHandler<CreatePredefinedMaterialCommand, Guid>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public CreatePredefinedMaterialCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<Guid> Handle(
            CreatePredefinedMaterialCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo(
                $"Creating Predefined Material. OrganizationId: {request.OrganizationId}");

            var dto = request.PredefinedMaterial;

            // =========================================================
            // 1. Check duplicate Material Code
            // =========================================================

            var existingMaterial =
                await _repository.PredefinedMaterial
                    .FindFirstByConditionAsync(x =>
                        x.MaterialCode == dto.MaterialCode &&
                        x.IsActive);

            if (existingMaterial != null)
            {
                _logger.LogError(
                    $"Material Code already exists. " +
                    $"MaterialCode: {dto.MaterialCode}");
                throw new PreConditionFailedCustomException(
                    "Material Code already exists.",
                    $"Material Code '{dto.MaterialCode}' already exists.");
            }

            // =========================================================
            // 2. Find Buyer using OrganizationId only
            // =========================================================

            var buyer =
                await _repository.BuyerBusinessProfile
                    .FindFirstByConditionAsync(x =>
                        x.OrganizationId == request.OrganizationId &&
                        x.IsActive);

            if (buyer == null)
            {
                _logger.LogError(
                    $"Buyer not found. OrganizationId: {request.OrganizationId}");
                throw new NotFoundCustomException(
                    "Buyer not found.",
                    "Buyer profile not found for the organization.");
            }

            // =========================================================
            // 3. Check Approval Flow
            // =========================================================

            var approvalFlow =
                await _repository.MasterApprovalFlow
                    .FindFirstByConditionAsync(x =>
                        x.Id == dto.ApprovalFlowId &&
                        x.IsActive);

            if (approvalFlow == null)
            {
                _logger.LogError(
                    $"Approval flow not found. " +
                    $"ApprovalFlowId: {dto.ApprovalFlowId}");
                throw new NotFoundCustomException(
                    "Approval flow not found.",
                    "The selected approval flow does not exist.");
            }

            // =========================================================
            // 4. Create Predefined Material
            // =========================================================

            var predefinedMaterial = new PredefinedMaterial
            {
                Id = Guid.NewGuid(),

                BuyerId = buyer.Id,

                Description = dto.Description,

                MaterialCode = dto.MaterialCode,

                MaterialGroup = dto.MaterialGroup,

                ProductType = dto.ProductType,

                BaseUnitOfMeasure = dto.BaseUnitOfMeasure,

                OrderUnitOfMeasure = dto.OrderUnitOfMeasure,

                AlternateUnitOfMeasure = dto.AlternateUnitOfMeasure,

                ValuationClass = dto.ValuationClass,

                UnitOfMeasureMapping = dto.UnitOfMeasureMapping,

                SubUnit = dto.SubUnit,

                MicroUnit = dto.MicroUnit,

                Status = Common.ITEM_MASTER_STATUS
            };

            await _repository.PredefinedMaterial
                .CreateAsync(predefinedMaterial);

            // =========================================================
            // 5. Create Material -> Approval Flow Mapping
            // =========================================================

            var materialApprovalFlowMapping =
                new ApprovalFlowPredefinedMaterialMapping
                {
                    Id = Guid.NewGuid(),

                    ApprovalFlowId = dto.ApprovalFlowId,

                    PredefinedMaterialId = predefinedMaterial.Id
                };

            await _repository
                .ApprovalFlowPredefinedMaterialMapping
                .CreateAsync(materialApprovalFlowMapping);

            // =========================================================
            // 6. Get Approval Flow Users
            // =========================================================

            var approvalFlowUsers =
                await _repository.ApprovalFlowUserMapping
                    .FindByCondition(x =>
                        x.ApprovalFlowId == dto.ApprovalFlowId &&
                        x.IsActive)
                    .ToListAsync(cancellationToken);

            if (!approvalFlowUsers.Any())
            {
                _logger.LogError(
                    $"Approval flow users not found. " +
                    $"ApprovalFlowId: {dto.ApprovalFlowId}");
                throw new PreConditionFailedCustomException(
                    "Approval flow users not found.",
                    "The selected approval flow has no users configured.");
            }

            // =========================================================
            // 7. Create Material -> Approval Flow -> User Mappings
            // =========================================================

            var materialApprovalFlowUserMappings =
                approvalFlowUsers
                    .Select(user =>
                        new PredefinedMaterialApprovalFlowUserMapping
                        {
                            Id = Guid.NewGuid(),

                            ApprovalFlowPredefinedMaterialId =
                                materialApprovalFlowMapping.Id,

                            ApprovalFlowId =
                                dto.ApprovalFlowId,

                            UserId =
                                user.UserId,

                            Order =
                                user.Order,

                            Comment =
                                dto.Comment,

                            Status =
                                Common.PENDING
                        })
                    .ToList();

            if (materialApprovalFlowUserMappings.Any())
            {
                _logger.LogInfo(
                    $"Creating {materialApprovalFlowUserMappings.Count} " +
                    $"Material -> Approval Flow -> User Mappings.");
                await _repository
                    .PredefinedMaterialApprovalFlowUserMapping
                    .CreateRangeAsync(
                        materialApprovalFlowUserMappings);
            }

            // =========================================================
            // 8. Save Everything
            // =========================================================

            await _repository.SaveAsync();

            _logger.LogInfo(
                $"Item Buyer Master created successfully. " +
                $"PredefinedMaterialId: {predefinedMaterial.Id}");

            return predefinedMaterial.Id;
        }
    }
}