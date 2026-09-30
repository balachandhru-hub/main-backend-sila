using Buyer.Application.Services;
using Buyer.Domain.Dtos;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.GetWishlist
{
    public class GetWishlistQueryHandler : IRequestHandler<GetWishlistQuery, WishlistResponseDto>
    {
        private readonly WishlistWorkflow _workflow;
        public GetWishlistQueryHandler(IRepositoryWrapper workflowRepository, ILoggerManager logger)
            => _workflow = new WishlistWorkflow(workflowRepository, logger);
        public Task<WishlistResponseDto> Handle(GetWishlistQuery request, CancellationToken cancellationToken) =>
            _workflow.GetAsync(request.OrganizationId, request.WishlistId, cancellationToken);
    }
}
