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

namespace Operations.Application.Features.Commands.CreateIntegration
{
    public class CreateIntegrationCommandHandler : IRequestHandler<CreateIntegrationCommand, IntegrationConfigurationResponseDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly IIntegrationCredentialProtector _credentials;

        public CreateIntegrationCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            IIntegrationCredentialProtector credentials)
        {
            _repository = repository;
            _logger = logger;
            _credentials = credentials;
        }

        public async Task<IntegrationConfigurationResponseDto> Handle(CreateIntegrationCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Creating integration. Name: {request.Request.Name}, ProcessType: {request.Request.ProcessType}, OrganizationId: {request.OrganizationId}, UserId: {request.UserId}");

            ApiIntegrationConfiguration configuration = new ApiIntegrationConfiguration
            {
                Id = Guid.NewGuid(),
                OrganizationId = request.OrganizationId,
                Status = IntegrationConfigurationStatus.DRAFT
            };
            await IntegrationConfigurationRules.ApplyAsync(_repository, _logger, _credentials, configuration, request.Request, request.OrganizationType, cancellationToken);
            _repository.ApiIntegrationConfiguration.Create(configuration);
            AuditTrail.Add(_repository, request.OrganizationId, null, request.UserId, "INTEGRATION_CREATED", "ApiIntegrationConfiguration", configuration.Id, configuration.Name);
            await _repository.SaveAsync();

            _logger.LogInfo($"Integration created. ConfigurationId: {configuration.Id}, OrganizationId: {request.OrganizationId}");
            return ResponseBuilder.Integration(configuration, _credentials);
        }
    }
}
