using Buyer.Application.Services;
using Buyer.Domain.Dtos;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.CancelWishlist
{
    public class CancelWishlistCommandHandler : IRequestHandler<CancelWishlistCommand, Unit>
    {
        private readonly WishlistWorkflow _workflow;
        public CancelWishlistCommandHandler(IRepositoryWrapper workflowRepository, ILoggerManager logger)
            => _workflow = new WishlistWorkflow(workflowRepository, logger);
        public async Task<Unit> Handle(CancelWishlistCommand request, CancellationToken cancellationToken)
        {
            await _workflow.CancelAsync(request.OrganizationId, request.UserId, request.WishlistId, cancellationToken);
            return Unit.Value;
        }
    }
}
