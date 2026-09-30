using Buyer.Application.Services;
using Buyer.Domain.Dtos;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.DecideWishlist
{
    public class DecideWishlistCommandHandler : IRequestHandler<DecideWishlistCommand, Unit>
    {
        private readonly WishlistWorkflow _workflow;
        public DecideWishlistCommandHandler(IRepositoryWrapper workflowRepository, ILoggerManager logger)
            => _workflow = new WishlistWorkflow(workflowRepository, logger);
        public async Task<Unit> Handle(DecideWishlistCommand request, CancellationToken cancellationToken)
        {
            await _workflow.DecideAsync(request.OrganizationId, request.UserId, request.WishlistId, request.Decision, cancellationToken);
            return Unit.Value;
        }
    }
}
