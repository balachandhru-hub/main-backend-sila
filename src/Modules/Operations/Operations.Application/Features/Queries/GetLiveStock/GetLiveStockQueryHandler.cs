using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Operations.Application.Features.Shared;
using Operations.Application.Services.Integration;
using Operations.Domain.Dtos;
using Operations.Domain.Entities;
using Operations.Domain.Enums;
using Operations.Infrastructure.Contracts.IRepository;
using SharedKernel.LoggerServices;

namespace Operations.Application.Features.Queries.GetLiveStock
{
    /// <summary>
    /// Stock in hand is not stored here: it is read from the organization's stock API each time it
    /// is asked for, so the answer is the ERP's figure of that moment.
    /// </summary>
    public class GetLiveStockQueryHandler : IRequestHandler<GetLiveStockQuery, LiveStockResponseDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly IIntegrationHttpExecutor _executor;

        public GetLiveStockQueryHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            IIntegrationHttpExecutor executor)
        {
            _repository = repository;
            _logger = logger;
            _executor = executor;
        }

        public async Task<LiveStockResponseDto> Handle(GetLiveStockQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Reading live stock. OrganizationId: {request.OrganizationId}");

            LiveStockResponseDto result = new LiveStockResponseDto();
            List<ApiIntegrationConfiguration> configurations = await _repository.ApiIntegrationConfiguration
                .FindByCondition(x => x.OrganizationId == request.OrganizationId
                    && x.ProcessType == IntegrationProcessType.GET_STOCK
                    && x.Status == IntegrationConfigurationStatus.ACTIVE
                    && x.IsActive)
                .ToListAsync(cancellationToken);
            if (configurations.Count == 0)
            {
                _logger.LogInfo($"No active stock API. OrganizationId: {request.OrganizationId}");
                return result;
            }

            result.Configured = true;
            foreach (ApiIntegrationConfiguration configuration in configurations)
            {
                List<ApiFieldMapping> mappings = await _repository.ApiFieldMapping
                    .FindByCondition(x => x.ConfigurationId == configuration.Id)
                    .ToListAsync(cancellationToken);
                try
                {
                    string payload;
                    using (HttpResponseMessage response = await _executor.SendAsync(
                        configuration, _executor.BuildUrl(configuration, false), HttpMethod.Get, null, cancellationToken))
                    {
                        if (!response.IsSuccessStatusCode)
                        {
                            throw new IntegrationException("REMOTE_HTTP_ERROR", $"The API returned HTTP {(int)response.StatusCode}.", 424);
                        }

                        payload = await response.Content.ReadAsStringAsync(cancellationToken);
                    }

                    foreach (JsonElement record in _executor.ReadRecords(payload))
                    {
                        try
                        {
                            result.Items.Add(IntegrationStockWorkflow.ReadStock(mappings, record));
                        }
                        catch (InvalidOperationException)
                        {
                            // A record without a material code or quantity says nothing about any stock.
                        }
                    }
                }
                catch (IntegrationException exception)
                {
                    // The stock of this entity is then simply not known; the caller shows it as such.
                    result.Failed = true;
                    _logger.LogError($"Live stock could not be read. ConfigurationId: {configuration.Id}, Code: {exception.Code}, Error: {exception.Message}");
                }
            }

            _logger.LogInfo($"Live stock read. OrganizationId: {request.OrganizationId}, Items: {result.Items.Count}");
            return result;
        }
    }
}
