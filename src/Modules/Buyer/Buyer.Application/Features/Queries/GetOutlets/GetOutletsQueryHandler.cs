using Buyer.Application.Services;
using Buyer.Domain.Dtos;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.GetOutlets
{
    public class GetOutletsQueryHandler : IRequestHandler<GetOutletsQuery, List<OutletResponseDto>>
    {
        private readonly WishlistWorkflow _workflow;
        public GetOutletsQueryHandler(IRepositoryWrapper workflowRepository, ILoggerManager logger)
            => _workflow = new WishlistWorkflow(workflowRepository, logger);
        public Task<List<OutletResponseDto>> Handle(GetOutletsQuery request, CancellationToken cancellationToken) =>
            _workflow.ListOutletsAsync(request.OrganizationId, cancellationToken);
    }
}
