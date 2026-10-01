using MediatR;
using Microsoft.EntityFrameworkCore;
using Operations.Application.Features.Shared;
using Operations.Domain.Common;
using Operations.Domain.Dtos;
using Operations.Domain.Entities;
using Operations.Domain.Enums;
using Operations.Infrastructure.Contracts.IRepository;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Operations.Application.Features.Commands.DeactivateIntegration
{
    public class DeactivateIntegrationCommandHandler : IRequestHandler<DeactivateIntegrationCommand, bool>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public DeactivateIntegrationCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<bool> Handle(DeactivateIntegrationCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Deactivating integration. ConfigurationId: {request.ConfigurationId}, OrganizationId: {request.OrganizationId}, UserId: {request.UserId}");

            ApiIntegrationConfiguration configuration = await IntegrationConfigurationRules.GetTrackedAsync(_repository, _logger, request.ConfigurationId, request.OrganizationId);
            configuration.Status = IntegrationConfigurationStatus.INACTIVE;
            configuration.NextRunAt = null;
            AuditTrail.Add(_repository, request.OrganizationId, null, request.UserId, "INTEGRATION_DEACTIVATED", "ApiIntegrationConfiguration", configuration.Id, configuration.Name);
            await _repository.SaveAsync();

            _logger.LogInfo($"Integration deactivated. ConfigurationId: {configuration.Id}");
            return false;
        }
    }
}
