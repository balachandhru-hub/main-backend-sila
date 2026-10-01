using MediatR;
using Microsoft.EntityFrameworkCore;
using Operations.Application.Features.Shared;
using Operations.Application.Services.Integration;
using Operations.Domain.Common;
using Operations.Domain.Dtos;
using Operations.Domain.Entities;
using Operations.Domain.Enums;
using Operations.Infrastructure.Contracts.IRepository;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Operations.Application.Features.Commands.UpdateIntegration
{
    public class UpdateIntegrationCommandHandler : IRequestHandler<UpdateIntegrationCommand, IntegrationConfigurationResponseDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly IIntegrationCredentialProtector _credentials;

        public UpdateIntegrationCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            IIntegrationCredentialProtector credentials)
        {
            _repository = repository;
            _logger = logger;
            _credentials = credentials;
        }

        public async Task<IntegrationConfigurationResponseDto> Handle(UpdateIntegrationCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Updating integration. ConfigurationId: {request.ConfigurationId}, OrganizationId: {request.OrganizationId}, UserId: {request.UserId}");

            ApiIntegrationConfiguration configuration = await IntegrationConfigurationRules.GetTrackedAsync(_repository, _logger, request.ConfigurationId, request.OrganizationId);
            await IntegrationConfigurationRules.ApplyAsync(_repository, _logger, _credentials, configuration, request.Request, cancellationToken);
            if (configuration.Status != IntegrationConfigurationStatus.ACTIVE)
            {
                configuration.Status = IntegrationConfigurationStatus.DRAFT;
            }

            AuditTrail.Add(_repository, request.OrganizationId, null, request.UserId, "INTEGRATION_UPDATED", "ApiIntegrationConfiguration", configuration.Id, configuration.Name);
            await _repository.SaveAsync();

            _logger.LogInfo($"Integration updated. ConfigurationId: {configuration.Id}, OrganizationId: {request.OrganizationId}");
            return ResponseBuilder.Integration(configuration, _credentials);
        }
    }
}
