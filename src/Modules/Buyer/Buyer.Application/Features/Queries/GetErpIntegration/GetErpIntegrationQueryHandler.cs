using Buyer.Application.Services;
using Buyer.Domain.Dtos;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.GetErpIntegration
{
    public class GetErpIntegrationQueryHandler : IRequestHandler<GetErpIntegrationQuery, ErpIntegrationResponseDto?>
    {
        private readonly WishlistWorkflow _workflow;
        public GetErpIntegrationQueryHandler(IRepositoryWrapper workflowRepository, ILoggerManager logger)
            => _workflow = new WishlistWorkflow(workflowRepository, logger);
        public Task<ErpIntegrationResponseDto?> Handle(GetErpIntegrationQuery request, CancellationToken cancellationToken) =>
            _workflow.GetErpConfigurationAsync(request.OrganizationId, cancellationToken);
    }
}
