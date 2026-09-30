using Buyer.Application.Services;
using Buyer.Domain.Dtos;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.RetryWishlistIntegration
{
    public class RetryWishlistIntegrationCommandHandler : IRequestHandler<RetryWishlistIntegrationCommand, Unit>
    {
        private readonly WishlistWorkflow _workflow;
        public RetryWishlistIntegrationCommandHandler(IRepositoryWrapper workflowRepository, ILoggerManager logger)
            => _workflow = new WishlistWorkflow(workflowRepository, logger);
        public async Task<Unit> Handle(RetryWishlistIntegrationCommand request, CancellationToken cancellationToken)
        {
            await _workflow.RetryAsync(request.OrganizationId, request.UserId, request.WishlistId, cancellationToken);
            return Unit.Value;
        }
    }
}
