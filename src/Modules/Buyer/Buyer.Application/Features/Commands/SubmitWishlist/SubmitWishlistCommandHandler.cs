using Buyer.Application.Services;
using Buyer.Domain.Dtos;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.SubmitWishlist
{
    public class SubmitWishlistCommandHandler : IRequestHandler<SubmitWishlistCommand, Unit>
    {
        private readonly WishlistWorkflow _workflow;
        public SubmitWishlistCommandHandler(IRepositoryWrapper workflowRepository, ILoggerManager logger)
            => _workflow = new WishlistWorkflow(workflowRepository, logger);
        public async Task<Unit> Handle(SubmitWishlistCommand request, CancellationToken cancellationToken)
        {
            await _workflow.SubmitAsync(request.OrganizationId, request.UserId, request.WishlistId, cancellationToken);
            return Unit.Value;
        }
    }
}
