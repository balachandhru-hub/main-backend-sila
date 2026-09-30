using Buyer.Application.Services;
using Buyer.Domain.Dtos;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.UpdateWishlist
{
    public class UpdateWishlistCommandHandler : IRequestHandler<UpdateWishlistCommand, Unit>
    {
        private readonly WishlistWorkflow _workflow;
        public UpdateWishlistCommandHandler(IRepositoryWrapper workflowRepository, ILoggerManager logger)
            => _workflow = new WishlistWorkflow(workflowRepository, logger);
        public async Task<Unit> Handle(UpdateWishlistCommand request, CancellationToken cancellationToken)
        {
            await _workflow.UpdateAsync(request.OrganizationId, request.UserId, request.WishlistId, request.Request, cancellationToken);
            return Unit.Value;
        }
    }
}
