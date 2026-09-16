using Buyer.Domain.Common;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.ApproveRejectMaster
{
    public class ApproveRejectMasterCommandHandler
        : IRequestHandler<ApproveRejectMasterCommand, Guid>
    {
        private readonly IRepositoryWrapper _repositoryWrapper;
        private readonly ILoggerManager _logger;

        public ApproveRejectMasterCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repositoryWrapper = repository;
            _logger = logger;
        }

        public async Task<Guid> Handle(
            ApproveRejectMasterCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo(
                $"Processing item master approval. " +
                $"PredefinedMaterialId: {request.PredefinedMaterialId}, " +
                $"UserId: {request.UserId}");

            // ---------------------------------------------------------
            // 1. Validate Status
            // ---------------------------------------------------------

            if (string.IsNullOrWhiteSpace(request.Approval.Status))
            {
                throw new BadRequestCustomException(
                    "Approval status is required.",
                    "Please provide APPROVE or REJECT.");
            }

            if (request.Approval.Status != Common.APPROVED &&
                request.Approval.Status != Common.REJECTED)
            {
                throw new BadRequestCustomException(
                    "Invalid approval status.",
                    "Status must be APPROVE or REJECT.");
            }

            // ---------------------------------------------------------
            // 2. Get Predefined Material
            // ---------------------------------------------------------

            var predefinedMaterial =
                await _repositoryWrapper.PredefinedMaterial
                    .FindFirstByConditionAsync(x =>
                        x.Id == request.PredefinedMaterialId &&
                        x.IsActive);

            if (predefinedMaterial == null)
            {
                _logger.LogError(
                    $"Predefined material not found. " +
                    $"PredefinedMaterialId: {request.PredefinedMaterialId}");
                throw new NotFoundCustomException(
                    "Predefined material not found.",
                    $"No predefined material found for Id: {request.PredefinedMaterialId}");
            }

            // ---------------------------------------------------------
            // 3. Get Material Approval Flow Mapping
            // ---------------------------------------------------------

            var materialApprovalFlowMapping =
                await _repositoryWrapper
                    .ApprovalFlowPredefinedMaterialMapping
                    .FindFirstByConditionAsync(x =>
                        x.PredefinedMaterialId ==
                        request.PredefinedMaterialId &&
                        x.IsActive);

            if (materialApprovalFlowMapping == null)
            {
                _logger.LogError(
                    $"Approval flow mapping not found for predefined material. " +
                    $"PredefinedMaterialId: {request.PredefinedMaterialId}");
                throw new NotFoundCustomException(
                    "Approval flow mapping not found.",
                    "No approval flow is configured for this material.");
            }

            // ---------------------------------------------------------
            // 4. Get Material Specific Approval Users
            // ---------------------------------------------------------

            var approvalUsers =
                await _repositoryWrapper
                    .PredefinedMaterialApprovalFlowUserMapping
                    .FindByCondition(x =>
                        x.ApprovalFlowPredefinedMaterialId ==
                        materialApprovalFlowMapping.Id &&
                        x.IsActive)
                    .OrderBy(x => x.Order)
                    .ToListAsync(cancellationToken);

            if (!approvalUsers.Any())
            {
                _logger.LogError(
                    $"Approval users not found for predefined material. " +
                    $"PredefinedMaterialId: {request.PredefinedMaterialId}");
                throw new NotFoundCustomException(
                    "Approval users not found.",
                    "No approval users are configured for this material.");
            }

            // ---------------------------------------------------------
            // 5. Find Current Logged-In User
            // ---------------------------------------------------------

            var currentApproval =
                approvalUsers.FirstOrDefault(x =>
                    x.UserId == request.UserId);

            if (currentApproval == null)
            {
                _logger.LogError(
                    $"Current user is not part of the approval flow. " +
                    $"PredefinedMaterialId: {request.PredefinedMaterialId}, " +
                    $"UserId: {request.UserId}");
                throw new ForBiddenCustomException(
                     "You are not authorized to approve this material.",
                     "The current user is not part of the approval flow.");
            }

            // ---------------------------------------------------------
            // 6. Check Whether Current User Already Approved/Rejected
            // ---------------------------------------------------------

            if (currentApproval.Status == Common.APPROVED ||
                currentApproval.Status == Common.REJECTED)
            {
                _logger.LogError(
                    $"Current user has already completed approval. " +
                    $"PredefinedMaterialId: {request.PredefinedMaterialId}, " +
                    $"UserId: {request.UserId}, " +
                    $"Status: {currentApproval.Status}");
                throw new PreConditionFailedCustomException(
                    "Approval already completed.",
                    "You have already approved or rejected this material.");
            }

            // ---------------------------------------------------------
            // 7. Check Previous Approval Level
            // ---------------------------------------------------------

            var previousApprovals =
                approvalUsers
                    .Where(x => x.Order < currentApproval.Order)
                    .ToList();

            bool previousLevelsApproved =
                previousApprovals.All(x =>
                    x.Status == Common.APPROVED);

            if (!previousLevelsApproved)
            {
                _logger.LogError(
                    $"Previous approval level is pending. " +
                    $"PredefinedMaterialId: {request.PredefinedMaterialId}, " +
                    $"UserId: {request.UserId}");
                throw new PreConditionFailedCustomException(
                    "Previous approval is pending.",
                    "The previous approval level must be approved first.");
            }

            // ---------------------------------------------------------
            // 8. Update Current Approval
            // ---------------------------------------------------------

            currentApproval.Status =
                request.Approval.Status;

            currentApproval.Comment =
                request.Approval.Comment;

            _repositoryWrapper
                .PredefinedMaterialApprovalFlowUserMapping
                .Update(currentApproval);

            // ---------------------------------------------------------
            // 9. Find Last Approval Level
            // ---------------------------------------------------------

            int lastApprovalOrder =
                approvalUsers.Max(x => x.Order);

            // ---------------------------------------------------------
            // 10. If Rejected
            // ---------------------------------------------------------

            if (request.Approval.Status == Common.REJECTED)
            {
                // If the last approver rejects,
                // the approval process is completed.
                if (currentApproval.Order == lastApprovalOrder)
                {
                    predefinedMaterial.Status = Common.COMPLETE;
                }
                else
                {
                    // Keep existing logic for rejection
                    predefinedMaterial.Status = Common.REJECTED;
                }

                _repositoryWrapper
                    .PredefinedMaterial
                    .Update(predefinedMaterial);

                await _repositoryWrapper.SaveAsync();

                _logger.LogInfo(
                    $"Predefined material rejected. " +
                    $"PredefinedMaterialId: {predefinedMaterial.Id}, " +
                    $"UserId: {request.UserId}, " +
                    $"MaterialStatus: {predefinedMaterial.Status}");

                return predefinedMaterial.Id;
            }

            // ---------------------------------------------------------
            // 11. If Current User Is Last Approver
            // ---------------------------------------------------------

            if (currentApproval.Order == lastApprovalOrder)
            {
                // -----------------------------------------------------
                // 12. Verify All Approval Levels Are Approved
                // -----------------------------------------------------

                bool allApproved =
                    approvalUsers.All(x =>
                        x.Status == Common.APPROVED);

                if (!allApproved)
                {
                    _logger.LogError(
                        $"Not all approval levels are approved. " +
                        $"PredefinedMaterialId: {request.PredefinedMaterialId}, " +
                        $"UserId: {request.UserId}");

                    throw new PreConditionFailedCustomException(
                        "Approval flow is incomplete.",
                        "All approval levels must be approved before creating the item master.");
                }

                // -----------------------------------------------------
                // 13. Check Item Buyer Master Already Exists
                // -----------------------------------------------------

                var existingItemMaster =
                    await _repositoryWrapper.ItemBuyerMaster
                        .FindFirstByConditionAsync(x =>
                            x.MaterialCode ==
                            predefinedMaterial.MaterialCode &&
                            x.IsActive);

                // -----------------------------------------------------
                // 14. Create Item Buyer Master
                // -----------------------------------------------------

                if (existingItemMaster == null)
                {
                    var itemMaster = new ItemBuyerMaster
                    {
                        Id = Guid.NewGuid(),

                        BuyerId =
                            predefinedMaterial.BuyerId,

                        Description =
                            predefinedMaterial.Description,

                        MaterialCode =
                            predefinedMaterial.MaterialCode,

                        MaterialGroup =
                            predefinedMaterial.MaterialGroup,

                        ProductType =
                            predefinedMaterial.ProductType,

                        BaseUnitOfMeasure =
                            predefinedMaterial.BaseUnitOfMeasure,

                        OrderUnitOfMeasure =
                            predefinedMaterial.OrderUnitOfMeasure,

                        AlternateUnitOfMeasure =
                            predefinedMaterial.AlternateUnitOfMeasure,

                        ValuationClass =
                            predefinedMaterial.ValuationClass,

                        UnitOfMeasureMapping =
                            predefinedMaterial.UnitOfMeasureMapping,

                        SubUnit =
                            predefinedMaterial.SubUnit,

                        MicroUnit =
                            predefinedMaterial.MicroUnit
                    };

                    await _repositoryWrapper.ItemBuyerMaster
                        .CreateAsync(itemMaster);

                    _logger.LogInfo(
                        $"Item Buyer Master created successfully. " +
                        $"MaterialCode: {predefinedMaterial.MaterialCode}");
                }

                // -----------------------------------------------------
                // 15. Final Approval Completed
                // -----------------------------------------------------

                predefinedMaterial.Status =
                    Common.COMPLETE;

                _repositoryWrapper
                    .PredefinedMaterial
                    .Update(predefinedMaterial);
            }
            else
            {
                // -----------------------------------------------------
                // 16. Approval Is Still In Progress
                // -----------------------------------------------------

                predefinedMaterial.Status =
                    Common.PROCESSING;

                _repositoryWrapper
                    .PredefinedMaterial
                    .Update(predefinedMaterial);
            }

            // ---------------------------------------------------------
            // 17. Save Changes
            // ---------------------------------------------------------

            await _repositoryWrapper.SaveAsync();

            _logger.LogInfo(
                $"Material approval processed successfully. " +
                $"PredefinedMaterialId: {predefinedMaterial.Id}, " +
                $"UserId: {request.UserId}, " +
                $"Status: {request.Approval.Status}, " +
                $"MaterialStatus: {predefinedMaterial.Status}");

            return predefinedMaterial.Id;
        }
    }
}