using System.Text.Json;
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

namespace Operations.Application.Features.Commands.DiscoverIntegrationSchema
{
    public class DiscoverIntegrationSchemaCommandHandler : IRequestHandler<DiscoverIntegrationSchemaCommand, IntegrationSchemaResponseDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly IIntegrationHttpExecutor _executor;

        public DiscoverIntegrationSchemaCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            IIntegrationHttpExecutor executor)
        {
            _repository = repository;
            _logger = logger;
            _executor = executor;
        }

        public async Task<IntegrationSchemaResponseDto> Handle(DiscoverIntegrationSchemaCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Discovering integration schema. ConfigurationId: {request.ConfigurationId}, OrganizationId: {request.OrganizationId}, UserId: {request.UserId}");

            ApiIntegrationConfiguration configuration = await IntegrationConfigurationRules.GetTrackedAsync(_repository, _logger, request.ConfigurationId, request.OrganizationId);
            if (configuration.Protocol != IntegrationProtocol.ODATA_V4)
            {
                _logger.LogError($"Schema discovery is not supported for this protocol. ConfigurationId: {configuration.Id}, Protocol: {configuration.Protocol}");
                throw new BadRequestCustomException("Schema discovery is not supported.", "Schema discovery is available for OData V4 configurations.");
            }

            string metadataUrl = _executor.MetadataUrl(configuration);
            List<IntegrationSchemaEntityDto> entities;
            try
            {
                using HttpResponseMessage response = await _executor.SendAsync(configuration, metadataUrl, HttpMethod.Get, null, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    throw new IntegrationException("SCHEMA_REQUEST_FAILED", $"The OData metadata request returned HTTP {(int)response.StatusCode}.", 424);
                }

                entities = _executor.ParseMetadata(await response.Content.ReadAsStringAsync(cancellationToken));
            }
            catch (IntegrationException exception)
            {
                _logger.LogError($"Schema discovery failed. ConfigurationId: {configuration.Id}, Code: {exception.Code}");
                throw IntegrationErrors.ToCustomException(exception);
            }

            IntegrationSchemaSnapshot snapshot = new IntegrationSchemaSnapshot
            {
                Id = Guid.NewGuid(),
                ConfigurationId = configuration.Id,
                MetadataUrl = metadataUrl,
                SchemaJson = JsonSerializer.Serialize(entities, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
                DiscoveredAt = DateTime.UtcNow
            };
            _repository.IntegrationSchemaSnapshot.Create(snapshot);
            await _repository.SaveAsync();

            _logger.LogInfo($"Integration schema discovered. ConfigurationId: {configuration.Id}, Entities: {entities.Count}");
            return new IntegrationSchemaResponseDto
            {
                ConfigurationId = configuration.Id,
                MetadataUrl = metadataUrl,
                DiscoveredAt = snapshot.DiscoveredAt,
                Entities = entities
            };
        }
    }
}
