using Buyer.Application.Services;
using Buyer.Domain.Dtos;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.CreateWishlist
{
    public class CreateWishlistCommandHandler : IRequestHandler<CreateWishlistCommand, Guid>
    {
        private readonly WishlistWorkflow _workflow;
        public CreateWishlistCommandHandler(IRepositoryWrapper workflowRepository, ILoggerManager logger)
            => _workflow = new WishlistWorkflow(workflowRepository, logger);
        public Task<Guid> Handle(CreateWishlistCommand request, CancellationToken cancellationToken) =>
            _workflow.CreateAsync(request.OrganizationId, request.UserId, request.Request, cancellationToken);
    }
}
