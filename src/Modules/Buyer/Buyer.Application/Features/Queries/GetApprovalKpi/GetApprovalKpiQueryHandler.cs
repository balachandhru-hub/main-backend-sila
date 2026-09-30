using Buyer.Domain.Common;
using Buyer.Domain.Dto;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.GetApprovalKpi
{
    public class GetApprovalKpiQueryHandler
        : IRequestHandler<GetApprovalKpiQuery, ApprovalKpiDto>
    {
        private readonly IRepositoryWrapper _repositoryWrapper;
        private readonly ILoggerManager _logger;

        public GetApprovalKpiQueryHandler(
            IRepositoryWrapper repositoryWrapper,
            ILoggerManager logger)
        {
            _repositoryWrapper = repositoryWrapper;
            _logger = logger;
        }

        public async Task<ApprovalKpiDto> Handle(
            GetApprovalKpiQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo(
                $"Fetching approval KPI counts for UserId: {request.UserId}");

            var approvals =
                await _repositoryWrapper
                    .PredefinedMaterialApprovalFlowUserMapping
                    .FindByCondition(x =>
                        x.UserId == request.UserId &&
                        x.IsActive)
                    .ToListAsync(cancellationToken);

            var result = new ApprovalKpiDto
            {
                TotalCount = approvals.Count,
                PendingCount = approvals.Count(x => x.Status == Common.PENDING),
                ApprovedCount = approvals.Count(x => x.Status == Common.APPROVED),
                RejectedCount = approvals.Count(x => x.Status == Common.REJECTED)
            };

            _logger.LogInfo(
                $"Approval KPI counts for UserId: {request.UserId}. " +
                $"Total: {result.TotalCount}, Pending: {result.PendingCount}, " +
                $"Approved: {result.ApprovedCount}, Rejected: {result.RejectedCount}");

            return result;
        }
    }
}
