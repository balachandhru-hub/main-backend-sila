using Buyer.Application.Services;
using Buyer.Domain.Dtos;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.CreateOutlet
{
    public class CreateOutletCommandHandler : IRequestHandler<CreateOutletCommand, Guid>
    {
        private readonly WishlistWorkflow _workflow;
        public CreateOutletCommandHandler(IRepositoryWrapper workflowRepository, ILoggerManager logger)
            => _workflow = new WishlistWorkflow(workflowRepository, logger);
        public Task<Guid> Handle(CreateOutletCommand request, CancellationToken cancellationToken) =>
            _workflow.CreateOutletAsync(request.OrganizationId, request.Request, cancellationToken);
    }
}
