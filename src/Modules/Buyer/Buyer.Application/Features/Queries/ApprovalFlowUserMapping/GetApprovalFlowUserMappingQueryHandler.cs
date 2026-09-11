using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;

namespace Buyer.Application.Features.Queries.ApprovalFlowUserMapping
{
    public class GetApprovalFlowUserMappingQueryHandler
        : IRequestHandler<GetApprovalFlowUserMappingQuery, List<Guid>>
    {
        private readonly IRepositoryWrapper _repositoryWrapper;

        public GetApprovalFlowUserMappingQueryHandler(
            IRepositoryWrapper repositoryWrapper)
        {
            _repositoryWrapper = repositoryWrapper;
        }

        public async Task<List<Guid>> Handle(
            GetApprovalFlowUserMappingQuery request,
            CancellationToken cancellationToken)
        {
            var userIds = _repositoryWrapper.ApprovalFlowUserMapping
                .FindByCondition(x => x.ApprovalFlowId == request.ApprovalId && x.IsActive)
                .OrderBy(x => x.Order)
                .Select(x => x.UserId)
                .ToList();

            return await Task.FromResult(userIds);
        }
    }
}
