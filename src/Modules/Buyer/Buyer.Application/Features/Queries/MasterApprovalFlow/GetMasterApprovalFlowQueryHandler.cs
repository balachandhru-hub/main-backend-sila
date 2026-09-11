using Buyer.Domain.Dtos;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.LoggerServices;
using SharedKernel.ExceptionHandler;

namespace Buyer.Application.Features.Queries.MasterApprovalFlow
{
    public class GetMasterApprovalFlowQueryHandler
        : IRequestHandler<GetMasterApprovalFlowQuery, List<MasterApprovalFlowDto>>
    {
        private readonly IRepositoryWrapper _repositoryWrapper;
         private readonly ILoggerManager _logger;

        public GetMasterApprovalFlowQueryHandler(
            IRepositoryWrapper repositoryWrapper,
            ILoggerManager logger)
        {
            _repositoryWrapper = repositoryWrapper;
            _logger = logger;
        }

        public async Task<List<MasterApprovalFlowDto>> Handle(
            GetMasterApprovalFlowQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo(
                $"Fetching Master Approval Flows. " +
                $"Index: {request.Index}, Limit: {request.Limit}, " +
                $"BuyerId: {request.BuyerId}");
            var query = _repositoryWrapper.MasterApprovalFlow
                .FindByCondition(x => x.IsActive);

            if (request.BuyerId.HasValue)
            {
                _logger.LogInfo(
                    $"Filtering by BuyerId: {request.BuyerId.Value}");
                query = query.Where(x => x.BuyerId == request.BuyerId.Value);
            }

            var items = query
                .OrderByDescending(x => x.DateUpdated)
                .Skip(request.Index)
                .Take(request.Limit)
                .ToList();

            var result = items.Select(x => new MasterApprovalFlowDto
            {
                Id = x.Id,
                ApprovalCode = x.ApprovalCode,
                ApprovalName = x.ApprovalName,
                BuyerId = x.BuyerId
            }).ToList();
            if (result == null)
            {
                _logger.LogError("No Master Approval Flows found.");
                throw new NotFoundCustomException(
                    "Master Approval Flow not found.",
                    "No Master Approval Flow records exist for the given criteria.");
            }
            _logger.LogInfo(
                $"Returning Master Approval Flows. Count: {result.Count}");
            return await Task.FromResult(result);
        }
    }
}
