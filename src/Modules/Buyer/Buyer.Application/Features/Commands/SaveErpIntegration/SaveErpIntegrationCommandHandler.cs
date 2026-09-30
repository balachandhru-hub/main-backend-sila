using Buyer.Application.Services;
using Buyer.Domain.Dtos;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.SaveErpIntegration
{
    public class SaveErpIntegrationCommandHandler : IRequestHandler<SaveErpIntegrationCommand, Guid>
    {
        private readonly WishlistWorkflow _workflow;
        public SaveErpIntegrationCommandHandler(IRepositoryWrapper workflowRepository, ILoggerManager logger)
            => _workflow = new WishlistWorkflow(workflowRepository, logger);
        public Task<Guid> Handle(SaveErpIntegrationCommand request, CancellationToken cancellationToken) =>
            _workflow.SaveErpConfigurationAsync(request.OrganizationId, request.Request, cancellationToken);
    }
}
