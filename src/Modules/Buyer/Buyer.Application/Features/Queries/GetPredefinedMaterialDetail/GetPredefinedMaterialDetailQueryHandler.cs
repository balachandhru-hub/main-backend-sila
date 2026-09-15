using Buyer.Domain.Dtos;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.GetPredefinedMaterialDetail
{
    public class GetPredefinedMaterialDetailQueryHandler
        : IRequestHandler<GetPredefinedMaterialDetailQuery, PredefinedMaterialDetailDto>
    {
        private readonly IRepositoryWrapper _repositoryWrapper;
        private readonly ILoggerManager _logger;

        public GetPredefinedMaterialDetailQueryHandler(
            IRepositoryWrapper repositoryWrapper,
            ILoggerManager logger)
        {
            _repositoryWrapper = repositoryWrapper;
            _logger = logger;
        }

        public async Task<PredefinedMaterialDetailDto> Handle(
            GetPredefinedMaterialDetailQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo(
                $"Fetching Predefined Material details. PredefinedMaterialId: {request.PredefinedMaterialId}");

            // ---------------------------------------------------------
            // 1. Get Predefined Material
            // ---------------------------------------------------------

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

            // ---------------------------------------------------------
            // 2. Get Material Approval Flow Mapping
            // ---------------------------------------------------------

            var materialApprovalFlowMapping =
                await _repositoryWrapper.ApprovalFlowPredefinedMaterialMapping
                    .FindFirstByConditionAsync(x =>
                        x.PredefinedMaterialId == predefinedMaterial.Id &&
                        x.IsActive);

            // ---------------------------------------------------------
            // 3. Get Approval User Ids
            // ---------------------------------------------------------

            var approvalUserIds = new List<Guid>();

            if (materialApprovalFlowMapping != null)
            {
                approvalUserIds =
                    await _repositoryWrapper.PredefinedMaterialApprovalFlowUserMapping
                        .FindByCondition(x =>
                            x.ApprovalFlowPredefinedMaterialId ==
                                materialApprovalFlowMapping.Id &&
                            x.IsActive)
                        .OrderBy(x => x.Order)
                        .Select(x => x.UserId)
                        .ToListAsync(cancellationToken);
            }

            // ---------------------------------------------------------
            // 4. Build Result
            // ---------------------------------------------------------
        _logger.LogInfo(
                $"Predefined Material details fetched successfully. PredefinedMaterialId: {request.PredefinedMaterialId}");
            return new PredefinedMaterialDetailDto
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
                Status = predefinedMaterial.Status,
                ApprovalUserIds = approvalUserIds
                
            };
            
        }
        
    }
}
