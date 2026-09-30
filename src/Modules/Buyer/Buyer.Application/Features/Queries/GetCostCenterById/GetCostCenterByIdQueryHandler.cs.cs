using Buyer.Domain.Dto;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.GetCostCenterById
{
    public class GetCostCenterByIdQueryHandler
        : IRequestHandler<GetCostCenterByIdQuery, CostCenterDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetCostCenterByIdQueryHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<CostCenterDto> Handle(
            GetCostCenterByIdQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo(
                $"Fetching Cost Center for CostCenterId: {request.CostCenterId}");

            var costCenter = await _repository.BuyerCostCenter
                .FindByCondition(x =>
                    x.Id == request.CostCenterId &&
                    x.IsActive)
                .Select(x => new CostCenterDto
                {
                    Id = x.Id,
                    CostCenter = x.CostCenter
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (costCenter == null)
            {
                _logger.LogError(
                    $"Cost Center not found for CostCenterId: {request.CostCenterId}");

                throw new NotFoundCustomException(
                    "Cost center not found.",
                    $"No cost center exists with Id: {request.CostCenterId}");
            }

            _logger.LogInfo(
                $"Cost Center fetched successfully for CostCenterId: {request.CostCenterId}");

            return costCenter;
        }
    }
}