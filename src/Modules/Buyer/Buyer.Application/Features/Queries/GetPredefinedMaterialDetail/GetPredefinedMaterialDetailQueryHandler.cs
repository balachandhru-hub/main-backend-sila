using Buyer.Application.Contracts;
using Buyer.Domain.Common;
using Buyer.Domain.Dtos;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Dto;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.GetPredefinedMaterialDetail
{
    public class GetPredefinedMaterialDetailQueryHandler
        : IRequestHandler<GetPredefinedMaterialDetailQuery, PredefinedMaterialDetailDto>
    {
        private readonly IRepositoryWrapper _repositoryWrapper;
        private readonly IIdentityApiClient _identityApiClient;
        private readonly ILoggerManager _logger;

        public GetPredefinedMaterialDetailQueryHandler(
            IRepositoryWrapper repositoryWrapper,
            IIdentityApiClient identityApiClient,
            ILoggerManager logger)
        {
            _repositoryWrapper = repositoryWrapper;
            _identityApiClient = identityApiClient;
            _logger = logger;
        }

        public async Task<PredefinedMaterialDetailDto> Handle(
            GetPredefinedMaterialDetailQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo(
                $"Fetching Predefined Material details. PredefinedMaterialId: {request.PredefinedMaterialId}");

            // ---------------------------------------------------------
            // 1. Get Material Approval Flow Mapping first - the incoming
            //    id is polymorphic: it's a PredefinedMaterial.Id for the
            //    manual flow, or an ExcelMaterialMaster.Id for the Excel
            //    bulk flow. UploadType tells us which, same as the
            //    sibling GetPendingApprovals list endpoint.
            // ---------------------------------------------------------

            var materialApprovalFlowMapping =
                await _repositoryWrapper.ApprovalFlowPredefinedMaterialMapping
                    .FindFirstByConditionAsync(x =>
                        x.PredefinedMaterialId == request.PredefinedMaterialId &&
                        x.IsActive);

            bool isExcel =
                materialApprovalFlowMapping != null &&
                materialApprovalFlowMapping.UploadType == Common.UPLOAD_TYPE_EXCEL;

            PredefinedMaterialDetailDto result;

            if (isExcel)
            {
                // -----------------------------------------------------
                // 2a. Excel batch - Title/Asset instead of material fields
                // -----------------------------------------------------

                var excelMaterialMaster =
                    await _repositoryWrapper.ExcelMaterialMaster
                        .FindFirstByConditionAsync(x =>
                            x.Id == request.PredefinedMaterialId &&
                            x.IsActive);

                if (excelMaterialMaster == null)
                {
                    _logger.LogError(
                        $"Excel material batch not found. ExcelMaterialMasterId: {request.PredefinedMaterialId}");
                    throw new NotFoundCustomException(
                        "Excel material batch not found.",
                        $"No Excel material batch found for Id: {request.PredefinedMaterialId}");
                }

                var excelAsset =
                    await _repositoryWrapper.Asset
                        .FindFirstByConditionAsync(x =>
                            x.Id == excelMaterialMaster.AssetId &&
                            x.IsActive);

                result = new ExcelPredefinedMaterialDetailDto
                {
                    Id = excelMaterialMaster.Id,
                    BuyerId = excelMaterialMaster.BuyerId,
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
                // -----------------------------------------------------
                // 2b. Manual - unchanged from the original behavior
                // -----------------------------------------------------

                var predefinedMaterial =
                    await _repositoryWrapper.PredefinedMaterial
                        .FindFirstByConditionAsync(x =>
                            x.Id == request.PredefinedMaterialId &&
                            x.IsActive);

                if (predefinedMaterial == null)
                {
                    _logger.LogError(
                        $"Predefined material not found. PredefinedMaterialId: {request.PredefinedMaterialId}");
                    throw new NotFoundCustomException(
                        "Predefined material not found.",
                        $"No predefined material found for Id: {request.PredefinedMaterialId}");
                }

                result = new ManualPredefinedMaterialDetailDto
                {
                    Id = predefinedMaterial.Id,
                    BuyerId = predefinedMaterial.BuyerId,
                    BaseUnitOfMeasure = predefinedMaterial.BaseUnitOfMeasure,
                    OrderUnitOfMeasure = predefinedMaterial.OrderUnitOfMeasure,
                    AlternateUnitOfMeasure = predefinedMaterial.AlternateUnitOfMeasure,
                    ValuationClass = predefinedMaterial.ValuationClass,
                    UnitOfMeasureMapping = predefinedMaterial.UnitOfMeasureMapping,
                    SubUnit = predefinedMaterial.SubUnit,
                    MicroUnit = predefinedMaterial.MicroUnit,
                    Status = predefinedMaterial.Status
                };
            }

            // ---------------------------------------------------------
            // 3. Get Approval Users (Order + Status from the mapping,
            //    UserName + Email from the Identity service) - same for
            //    both flows, keyed off the mapping's own Id.
            // ---------------------------------------------------------

            var approvalUsers = new List<PredefinedMaterialApprovalUserDto>();

            if (materialApprovalFlowMapping != null)
            {
                approvalUsers =
                    await _repositoryWrapper.PredefinedMaterialApprovalFlowUserMapping
                        .FindByCondition(x =>
                            x.ApprovalFlowPredefinedMaterialId ==
                                materialApprovalFlowMapping.Id &&
                            x.IsActive)
                        .OrderBy(x => x.Order)
                        .Select(x => new PredefinedMaterialApprovalUserDto
                        {
                            UserId = x.UserId,
                            Order = x.Order,
                            Status = x.Status
                        })
                        .ToListAsync(cancellationToken);

                if (approvalUsers.Any())
                {
                    var userIds = approvalUsers
                        .Select(x => x.UserId)
                        .Distinct()
                        .ToList();

                    var identityUsers = await _identityApiClient.GetUsersByIds(
                        userIds,
                        cancellationToken);

                    foreach (var approvalUser in approvalUsers)
                    {
                        var identity = identityUsers
                            .FirstOrDefault(x => x.UserId == approvalUser.UserId);

                        approvalUser.UserName = identity?.UserName;
                        approvalUser.Email = identity?.Email;
                    }
                }
            }

            result.ApprovalUsers = approvalUsers;

            _logger.LogInfo(
                $"Predefined Material details fetched successfully. PredefinedMaterialId: {request.PredefinedMaterialId}");

            return result;
        }
    }
}
