using Buyer.Domain.Dtos;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.ApprovalFlowUserMapping
{
    public class GetApprovalFlowUserMappingQueryHandler
        : IRequestHandler<
            GetApprovalFlowUserMappingQuery,
            List<ApprovalFlowUserMappingDto>>
    {
        private readonly IRepositoryWrapper _repositoryWrapper;
        private readonly ILoggerManager _logger;

        public GetApprovalFlowUserMappingQueryHandler(
            IRepositoryWrapper repositoryWrapper,
            ILoggerManager logger)
        {
            _repositoryWrapper = repositoryWrapper;
            _logger = logger;
        }

        public async Task<List<ApprovalFlowUserMappingDto>> Handle(
            GetApprovalFlowUserMappingQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo(
                $"Fetching user mappings for Approval Flow. " +
                $"ApprovalId: {request.ApprovalId}");

            var userMappings =
                _repositoryWrapper.ApprovalFlowUserMapping
                    .FindByCondition(x =>
                        x.ApprovalFlowId == request.ApprovalId &&
                        x.IsActive)
                    .OrderBy(x => x.Order)
                    .Select(x => new ApprovalFlowUserMappingDto
                    {
                        UserId = x.UserId,
                        Order = x.Order
                    })
                    .ToList();

            if (!userMappings.Any())
            {
                _logger.LogError(
                    $"No user mappings found for Approval Flow. " +
                    $"ApprovalId: {request.ApprovalId}");

                throw new NotFoundCustomException(
                    "No user mappings found for the specified approval flow.",
                    $"No active user mappings found for Approval Flow ID: {request.ApprovalId}");
            }

            _logger.LogInfo(
                $"Found {userMappings.Count} user mapping(s) for Approval Flow. " +
                $"ApprovalId: {request.ApprovalId}");

            return await Task.FromResult(userMappings);
        }
    }
}