using Buyer.Domain.Common;
using Buyer.Domain.Dtos;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.GetPendingApprovals
{
    public class GetPendingApprovalsQueryHandler
        : IRequestHandler<
            GetPendingApprovalsQuery,
            List<PendingApprovalDto>>
    {
        private readonly IRepositoryWrapper _repositoryWrapper;
        private readonly ILoggerManager _logger;

        public GetPendingApprovalsQueryHandler(
            IRepositoryWrapper repositoryWrapper,
            ILoggerManager logger)
        {
            _repositoryWrapper = repositoryWrapper;
            _logger = logger;
        }

        public async Task<List<PendingApprovalDto>> Handle(
            GetPendingApprovalsQuery request,
            CancellationToken cancellationToken)
        {
            // ---------------------------------------------------------
            // 1. Get Pending Approvals for Logged-In User
            // ---------------------------------------------------------
            _logger.LogInfo(
                $"Fetching pending approvals for user. " +
                $"UserId: {request.UserId}");
            var pendingApprovals =
                await _repositoryWrapper
                    .PredefinedMaterialApprovalFlowUserMapping
                    .FindByCondition(x =>
                        x.UserId == request.UserId &&
                        x.Status == Common.PENDING &&
                        x.IsActive)
                    .OrderBy(x => x.Order)
                    .ToListAsync(cancellationToken);

            if (!pendingApprovals.Any())
            {
                _logger.LogError(
                    $"No pending approvals found for user. " +
                    $"UserId: {request.UserId}");
                throw new NotFoundCustomException(
                    "No pending approvals found.",
                    "The logged-in user has no pending approvals.");
            }

            var result = new List<PendingApprovalDto>();

            // ---------------------------------------------------------
            // 2. Get Material Information
            // ---------------------------------------------------------

            foreach (var approval in pendingApprovals)
            {
              
                var materialApprovalFlowMapping =
                    await _repositoryWrapper
                        .ApprovalFlowPredefinedMaterialMapping
                        .FindFirstByConditionAsync(x =>
                            x.Id ==
                            approval.ApprovalFlowPredefinedMaterialId &&
                            x.IsActive);

                if (materialApprovalFlowMapping == null)
                {
                   throw new NotFoundCustomException(
                    "No  approvals found.",
                    "The logged-in user has no  approvals.");
                }

                // -----------------------------------------------------
                // 3. Get Predefined Material
                // -----------------------------------------------------

                var predefinedMaterial =
                    await _repositoryWrapper
                        .PredefinedMaterial
                        .FindFirstByConditionAsync(x =>
                            x.Id ==
                            materialApprovalFlowMapping.PredefinedMaterialId &&
                            x.IsActive);

                if (predefinedMaterial == null)
                {
                    {
                   throw new NotFoundCustomException(
                    "No  predefinedmaterial found.",
                    "The logged-in user has no predefinedmaterial  .");
                }
                }

                // -----------------------------------------------------
                // 4. Add Result
                // -----------------------------------------------------

                result.Add(new PendingApprovalDto
                {
                    PredefinedMaterialId =
                        predefinedMaterial.Id,

                    ApprovalFlowPredefinedMaterialId =
                        approval.ApprovalFlowPredefinedMaterialId,

                    ApprovalMappingId =
                        approval.Id,

                    Order =
                        approval.Order,

                    ApprovalStatus =
                        approval.Status,

                    MaterialCode =
                        predefinedMaterial.MaterialCode,

                    ProductType =
                        predefinedMaterial.ProductType,

                    Description =
                        predefinedMaterial.Description,

                    MaterialGroup =
                        predefinedMaterial.MaterialGroup,

                    Status =
                        predefinedMaterial.Status
                });
            }

            return result;
        }
    }
}