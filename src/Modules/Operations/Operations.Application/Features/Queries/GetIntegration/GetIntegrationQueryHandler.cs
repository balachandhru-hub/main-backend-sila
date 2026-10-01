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

namespace Operations.Application.Features.Queries.GetIntegration
{
    public class GetIntegrationQueryHandler : IRequestHandler<GetIntegrationQuery, IntegrationConfigurationResponseDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly IIntegrationCredentialProtector _credentials;

        public GetIntegrationQueryHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            IIntegrationCredentialProtector credentials)
        {
            _repository = repository;
            _logger = logger;
            _credentials = credentials;
        }

        public async Task<IntegrationConfigurationResponseDto> Handle(GetIntegrationQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching integration. ConfigurationId: {request.ConfigurationId}, OrganizationId: {request.OrganizationId}");

            ApiIntegrationConfiguration? configuration = await _repository.ApiIntegrationConfiguration
                .FindByCondition(x => x.Id == request.ConfigurationId && x.OrganizationId == request.OrganizationId && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);
            if (configuration == null)
            {
                _logger.LogError($"Integration configuration not found. ConfigurationId: {request.ConfigurationId}, OrganizationId: {request.OrganizationId}");
                throw new NotFoundCustomException("Integration not found.", "The integration configuration does not exist in your organization.");
            }

            _logger.LogInfo($"Integration fetched. ConfigurationId: {configuration.Id}");
            return ResponseBuilder.Integration(configuration, _credentials);
        }
    }
}
