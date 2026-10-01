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

namespace Operations.Application.Features.Queries.GetIntegrations
{
    public class GetIntegrationsQueryHandler : IRequestHandler<GetIntegrationsQuery, List<IntegrationConfigurationResponseDto>>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly IIntegrationCredentialProtector _credentials;

        public GetIntegrationsQueryHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            IIntegrationCredentialProtector credentials)
        {
            _repository = repository;
            _logger = logger;
            _credentials = credentials;
        }

        public async Task<List<IntegrationConfigurationResponseDto>> Handle(GetIntegrationsQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching integrations. OrganizationId: {request.OrganizationId}");

            List<ApiIntegrationConfiguration> configurations = await _repository.ApiIntegrationConfiguration
                .FindByCondition(x => x.OrganizationId == request.OrganizationId && x.IsActive)
                .OrderBy(x => x.Name)
                .ToListAsync(cancellationToken);

            _logger.LogInfo($"Integrations fetched. Count: {configurations.Count}, OrganizationId: {request.OrganizationId}");
            return configurations.Select(item => ResponseBuilder.Integration(item, _credentials)).ToList();
        }
    }
}
