using Buyer.Application.Services;
using Buyer.Domain.Dtos;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.GetWishlists
{
    public class GetWishlistsQueryHandler : IRequestHandler<GetWishlistsQuery, List<WishlistListItemDto>>
    {
        private readonly WishlistWorkflow _workflow;
        public GetWishlistsQueryHandler(IRepositoryWrapper workflowRepository, ILoggerManager logger)
            => _workflow = new WishlistWorkflow(workflowRepository, logger);
        public Task<List<WishlistListItemDto>> Handle(GetWishlistsQuery request, CancellationToken cancellationToken) =>
            _workflow.ListAsync(request.OrganizationId, request.Index, request.Limit, cancellationToken);
    }
}
