using Buyer.Domain.Common;
using Buyer.Domain.Dtos;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Dto;
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
            // 1. Get Approvals for Logged-In User
            // ---------------------------------------------------------

            _logger.LogInfo(
                $"Fetching approvals for user. " +
                $"UserId: {request.UserId}, " +
                $"Status: {request.Status}, " +
                $"SearchTerm: {request.SearchTerm}, " +
                $"Index: {request.Index}, Limit: {request.Limit}");

            bool filterByStatus =
                !string.IsNullOrWhiteSpace(request.Status) &&
                !request.Status.Equals("ALL", StringComparison.OrdinalIgnoreCase);

            var query =
                _repositoryWrapper
                    .PredefinedMaterialApprovalFlowUserMapping
                    .FindByCondition(x =>
                        x.UserId == request.UserId &&
                        x.IsActive);

            // ---------------------------------------------------------
            // 2. Apply Status Filter
            // ---------------------------------------------------------

            if (filterByStatus)
            {
                query = query.Where(x =>
                    x.Status == request.Status);
            }

            var approvals =
                await query
                    .OrderBy(x => x.Order)
                    .Skip(request.Index)
                    .Take(request.Limit)
                    .ToListAsync(cancellationToken);

            // ---------------------------------------------------------
            // 3. No Approval Records
            // ---------------------------------------------------------

            if (!approvals.Any())
            {
                _logger.LogError(
                    $"No approvals found for user. " +
                    $"UserId: {request.UserId}");

                throw new NotFoundCustomException(
                    "No approvals found.",
                    "The logged-in user has no approvals matching the given criteria.");
            }

            var result = new List<PendingApprovalDto>();

            // ---------------------------------------------------------
            // 4. Get Material Information
            // ---------------------------------------------------------

            foreach (var approval in approvals)
            {
                var materialApprovalFlowMapping =
                    await _repositoryWrapper
                        .ApprovalFlowPredefinedMaterialMapping
                        .FindFirstByConditionAsync(x =>
                            x.Id == approval.ApprovalFlowPredefinedMaterialId &&
                            x.IsActive);

                if (materialApprovalFlowMapping == null)
                {
                    continue;
                }

                // Excel bulk uploads have ONE approval mapping for the
                // whole batch, pointing at an ExcelMaterialMaster row
                // instead of a PredefinedMaterial row - handle both so
                // Excel approvals still show up here.
                bool isExcel =
                    materialApprovalFlowMapping.UploadType ==
                    Common.UPLOAD_TYPE_EXCEL;

                PendingApprovalDto? dto = null;

                if (isExcel)
                {
                    var excelMaterialMaster =
                        await _repositoryWrapper
                            .ExcelMaterialMaster
                            .FindFirstByConditionAsync(x =>
                                x.Id == materialApprovalFlowMapping.PredefinedMaterialId &&
                                x.IsActive);

                    if (excelMaterialMaster == null)
                    {
                        continue;
                    }

                    // Excel batches have no Description/MaterialCode/etc of
                    // their own (the actual rows live in the uploaded
                    // file), so search-term filtering does not apply to
                    // them - only skip them when a search term is given.
                    if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                    {
                        continue;
                    }

                    var excelAsset =
                        await _repositoryWrapper.Asset
                            .FindFirstByConditionAsync(x =>
                                x.Id == excelMaterialMaster.AssetId &&
                                x.IsActive);

                    dto = new ExcelPendingApprovalDto
                    {
                        PredefinedMaterialId = excelMaterialMaster.Id,

                        ApprovalId = materialApprovalFlowMapping.ApprovalFlowId,

                        ApprovalFlowPredefinedMaterialId =
                            approval.ApprovalFlowPredefinedMaterialId,

                        ApprovalMappingId = approval.Id,

                        Order = approval.Order,

                        ApprovalStatus = approval.Status,

                        Title = excelMaterialMaster.Title,

                        Asset = excelAsset == null
                            ? null
                            : new AssetDto
                            {
                                Id = excelAsset.Id,
                                AssetName = excelAsset.AssetName,
                                FileName = excelAsset.FileName
                            },

                        Status = excelMaterialMaster.Status
                    };
                }
                else
                {
                    var predefinedMaterial =
                        await _repositoryWrapper
                            .PredefinedMaterial
                            .FindFirstByConditionAsync(x =>
                                x.Id == materialApprovalFlowMapping.PredefinedMaterialId &&
                                x.IsActive);

                    if (predefinedMaterial == null)
                    {
                        continue;
                    }

                    // -----------------------------------------------------
                    // 5. Apply Search Term Filter
                    // -----------------------------------------------------

                    if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                    {
                        bool matchesSearchTerm =
                            (predefinedMaterial.MaterialCode?.Contains(
                                request.SearchTerm,
                                StringComparison.OrdinalIgnoreCase) ?? false)
                            ||
                            (predefinedMaterial.MaterialGroup?.Contains(
                                request.SearchTerm,
                                StringComparison.OrdinalIgnoreCase) ?? false)
                            ||
                            (predefinedMaterial.ProductType?.Contains(
                                request.SearchTerm,
                                StringComparison.OrdinalIgnoreCase) ?? false)
                            ||
                            (predefinedMaterial.Description?.Contains(
                                request.SearchTerm,
                                StringComparison.OrdinalIgnoreCase) ?? false);

                        // If this material does not match,
                        // skip it and check the next approval.
                        if (!matchesSearchTerm)
                        {
                            continue;
                        }
                    }

                    dto = new ManualPendingApprovalDto
                    {
                        PredefinedMaterialId =
                            predefinedMaterial.Id,

                        ApprovalId =
                            materialApprovalFlowMapping.ApprovalFlowId,

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
                    };
                }

                // -----------------------------------------------------
                // 6. Add Matching Approval
                // -----------------------------------------------------

                result.Add(dto);
            }

            // ---------------------------------------------------------
            // 7. No Results After Search Filter
            // ---------------------------------------------------------

            if (!result.Any())
            {
                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    _logger.LogError(
                        $"No approvals found matching search term. " +
                        $"UserId: {request.UserId}, " +
                        $"SearchTerm: {request.SearchTerm}");

                    throw new NotFoundCustomException(
                        "No approvals found matching the search term.",
                        $"No approvals found for the logged-in user that match the search term: {request.SearchTerm}");
                }

                throw new NotFoundCustomException(
                    "No approvals found.",
                    "No matching approval records were found.");
            }

            // ---------------------------------------------------------
            // 8. Return Result
            // ---------------------------------------------------------

            return result;
        }
    }
}
