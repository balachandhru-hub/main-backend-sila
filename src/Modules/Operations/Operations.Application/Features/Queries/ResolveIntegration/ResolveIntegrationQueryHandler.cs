using MediatR;
using Microsoft.EntityFrameworkCore;
using Operations.Domain.Dtos;
using Operations.Domain.Entities;
using Operations.Domain.Enums;
using Operations.Infrastructure.Contracts.IRepository;
using SharedKernel.LoggerServices;

namespace Operations.Application.Features.Queries.ResolveIntegration
{
    public class ResolveIntegrationQueryHandler : IRequestHandler<ResolveIntegrationQuery, ResolvedIntegrationDto>
    {
        private const string ENTITY_ALL = "ALL";

        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public ResolveIntegrationQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<ResolvedIntegrationDto> Handle(ResolveIntegrationQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Resolving integration. OrganizationId: {request.Request.OrganizationId}, ProcessType: {request.Request.ProcessType}, EntityCode: {request.Request.EntityCode}");

            List<ApiIntegrationConfiguration> active = await _repository.ApiIntegrationConfiguration
                .FindByCondition(x => x.OrganizationId == request.Request.OrganizationId
                    && x.ProcessType == request.Request.ProcessType
                    && x.Status == IntegrationConfigurationStatus.ACTIVE
                    && x.IsActive)
                .ToListAsync(cancellationToken);

            // The API of the named entity when it has one of its own, otherwise the organization-wide one.
            string? entityCode = request.Request.EntityCode?.Trim();
            ApiIntegrationConfiguration? configuration =
                active.FirstOrDefault(x => entityCode != null && x.EntityCode.Equals(entityCode, StringComparison.OrdinalIgnoreCase))
                ?? active.FirstOrDefault(x => x.EntityCode.Equals(ENTITY_ALL, StringComparison.OrdinalIgnoreCase))
                ?? active.OrderBy(x => x.DateCreated).FirstOrDefault();
            if (configuration == null)
            {
                _logger.LogInfo($"No active integration. OrganizationId: {request.Request.OrganizationId}, ProcessType: {request.Request.ProcessType}");
                return new ResolvedIntegrationDto { Configured = false };
            }

            _logger.LogInfo($"Integration resolved. ConfigurationId: {configuration.Id}, OrganizationId: {configuration.OrganizationId}");
            return new ResolvedIntegrationDto
            {
                Configured = true,
                ConfigurationId = configuration.Id,
                Name = configuration.Name,
                SystemName = configuration.SystemName,
                EntityCode = configuration.EntityCode,
                BaseUrl = configuration.BaseUrl,
                ResourcePath = configuration.ResourcePath,
                HttpMethod = configuration.HttpMethod,
                PayloadFormat = configuration.PayloadFormat,
                RequestBody = configuration.RequestBody
            };
        }
    }
}
