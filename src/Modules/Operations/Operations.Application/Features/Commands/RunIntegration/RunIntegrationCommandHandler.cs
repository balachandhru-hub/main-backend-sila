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

namespace Operations.Application.Features.Commands.RunIntegration
{
    public class RunIntegrationCommandHandler : IRequestHandler<RunIntegrationCommand, IntegrationExecutionResponseDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly IIntegrationHttpExecutor _executor;
        private readonly ISupplierCatalogSyncClient _catalogSync;

        public RunIntegrationCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            IIntegrationHttpExecutor executor,
            ISupplierCatalogSyncClient catalogSync)
        {
            _repository = repository;
            _logger = logger;
            _executor = executor;
            _catalogSync = catalogSync;
        }

        public async Task<IntegrationExecutionResponseDto> Handle(RunIntegrationCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Running integration. ConfigurationId: {request.ConfigurationId}, Trigger: {request.Trigger}, FullSync: {request.FullSync}, OrganizationId: {request.OrganizationId}");

            bool claimed = await _repository.ApiIntegrationConfiguration.TryClaimAsync(request.ConfigurationId, request.OrganizationId, cancellationToken);
            if (!claimed)
            {
                bool exists = await _repository.ApiIntegrationConfiguration
                    .FindByCondition(x => x.Id == request.ConfigurationId && x.OrganizationId == request.OrganizationId && x.IsActive)
                    .AnyAsync(cancellationToken);
                if (!exists)
                {
                    _logger.LogError($"Integration configuration not found. ConfigurationId: {request.ConfigurationId}, OrganizationId: {request.OrganizationId}");
                    throw new NotFoundCustomException("Integration not found.", "The integration configuration does not exist in your organization.");
                }

                _logger.LogError($"Integration is already running or inactive. ConfigurationId: {request.ConfigurationId}");
                throw new ConflictCustomException("Integration is busy.", "This integration is already running or is inactive.");
            }

            ApiIntegrationConfiguration configuration = await IntegrationConfigurationRules.GetTrackedAsync(_repository, _logger, request.ConfigurationId, request.OrganizationId);
            ApiIntegrationExecution execution = NewExecution(configuration, request);
            _repository.ApiIntegrationExecution.Create(execution);
            try
            {
                if (IntegrationProcessCatalog.Find(configuration.ProcessType)?.CanPull != true)
                {
                    throw new IntegrationException("PROCESS_NOT_IMPLEMENTED", "This API type is called by the application; it cannot be pulled.");
                }

                List<ApiFieldMapping> mappings = await _repository.ApiFieldMapping
                    .FindByCondition(x => x.ConfigurationId == configuration.Id)
                    .ToListAsync(cancellationToken);
                if (mappings.Count == 0)
                {
                    throw new IntegrationException("MAPPING_REQUIRED", "Map the fields of this integration before running it.");
                }

                // A full sync ignores the watermark and reads everything again.
                DateTime? watermark = configuration.LastWatermark;
                if (request.FullSync)
                {
                    configuration.LastWatermark = null;
                }

                string url = _executor.BuildUrl(configuration, false);
                configuration.LastWatermark = watermark;
                string payload;
                using (HttpResponseMessage response = await _executor.SendAsync(configuration, url, HttpMethod.Get, null, cancellationToken))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new IntegrationException("REMOTE_HTTP_ERROR", $"The API returned HTTP {(int)response.StatusCode}.", 424);
                    }

                    payload = await response.Content.ReadAsStringAsync(cancellationToken);
                }

                DateTime? maxWatermark = watermark;
                // Stock and catalog records are not stored in this service: the stock is only checked,
                // and the catalog belongs to the Supplier service.
                List<JsonElement> records = _executor.ReadRecords(payload);
                bool handledElsewhere = await RunStockOrCatalogAsync(configuration, mappings, records, execution, cancellationToken);
                foreach (JsonElement record in handledElsewhere ? new List<JsonElement>() : records)
                {
                    execution.RecordsRead++;
                    try
                    {
                        IntegrationImportWorkflow.ValidateMappedRecord(configuration, mappings, record);
                        bool created = configuration.ProcessType == IntegrationProcessType.GET_SUPPLIER
                            ? await IntegrationImportWorkflow.UpsertSupplierAsync(_repository, configuration, mappings, record, cancellationToken)
                            : await IntegrationImportWorkflow.UpsertPurchaseOrderAsync(_repository, configuration, mappings, record, cancellationToken);
                        if (created)
                        {
                            execution.RecordsCreated++;
                        }
                        else
                        {
                            execution.RecordsUpdated++;
                        }

                        maxWatermark = IntegrationRecordReader.Max(maxWatermark, IntegrationRecordReader.ReadDate(record, configuration.WatermarkField));
                    }
                    catch (Exception exception) when (exception is FormatException or InvalidOperationException or IntegrationException)
                    {
                        // One bad record (missing value, unknown or inactive supplier) does not stop the run.
                        execution.RecordsFailed++;
                        execution.ErrorMessageSafe = "One or more records could not be imported.";
                        _logger.LogError($"Integration record could not be imported. ConfigurationId: {configuration.Id}, Record: {execution.RecordsRead}, Error: {exception.Message}");
                    }
                }

                execution.Status = execution.RecordsFailed > 0 ? IntegrationExecutionStatus.PARTIAL : IntegrationExecutionStatus.SUCCESS;
                execution.WatermarkAfter = maxWatermark;
                execution.CompletedAt = DateTime.UtcNow;
                configuration.LastWatermark = maxWatermark;
                configuration.LastSuccessfulRunAt = DateTime.UtcNow;
                configuration.LastErrorSafe = execution.RecordsFailed > 0 ? execution.ErrorMessageSafe : null;
                configuration.NextRunAt = NextRun(configuration);
                configuration.IsRunning = false;
                configuration.RunningSince = null;
                await _repository.SaveAsync();
            }
            catch (Exception exception) when (exception is IntegrationException or DbUpdateException or JsonException)
            {
                // Nothing of a failed run is kept: the tracked changes are dropped and only the failure is recorded.
                IntegrationException failure = exception as IntegrationException
                    ?? new IntegrationException("INTEGRATION_FAILED", "The integration run failed. No records were changed.");
                _logger.LogError($"Integration run failed. ConfigurationId: {request.ConfigurationId}, Code: {failure.Code}, Error: {exception.Message}");
                _repository.ApiIntegrationExecution.DetachAllEntities();
                ApiIntegrationConfiguration failed = await IntegrationConfigurationRules.GetTrackedAsync(_repository, _logger, request.ConfigurationId, request.OrganizationId);
                ApiIntegrationExecution failedExecution = NewExecution(failed, request);
                failedExecution.StartedAt = execution.StartedAt;
                failedExecution.RecordsRead = execution.RecordsRead;
                failedExecution.Status = IntegrationExecutionStatus.FAILED;
                failedExecution.CompletedAt = DateTime.UtcNow;
                failedExecution.ErrorCode = failure.Code;
                failedExecution.ErrorMessageSafe = failure.Message;
                _repository.ApiIntegrationExecution.Create(failedExecution);
                failed.LastErrorSafe = failure.Message;
                failed.NextRunAt = NextRun(failed);
                failed.IsRunning = false;
                failed.RunningSince = null;
                await _repository.SaveAsync();
                throw IntegrationErrors.ToCustomException(failure);
            }

            _logger.LogInfo($"Integration run completed. ConfigurationId: {configuration.Id}, Status: {execution.Status}, Read: {execution.RecordsRead}, Created: {execution.RecordsCreated}, Updated: {execution.RecordsUpdated}, Failed: {execution.RecordsFailed}");
            return ResponseBuilder.Execution(execution);
        }

        /// <returns>True when the records belonged to a stock or catalog API and were dealt with here.</returns>
        private async Task<bool> RunStockOrCatalogAsync(
            ApiIntegrationConfiguration configuration,
            List<ApiFieldMapping> mappings,
            List<JsonElement> records,
            ApiIntegrationExecution execution,
            CancellationToken cancellationToken)
        {
            if (configuration.ProcessType is not (IntegrationProcessType.GET_STOCK or IntegrationProcessType.GET_CATALOG or IntegrationProcessType.GET_CATALOG_STOCK))
            {
                return false;
            }

            List<CatalogSyncItemDto> items = new List<CatalogSyncItemDto>();
            foreach (JsonElement record in records)
            {
                execution.RecordsRead++;
                try
                {
                    if (configuration.ProcessType == IntegrationProcessType.GET_STOCK)
                    {
                        // Stock in hand is read live when it is needed; a pull only proves the API and its mapping.
                        _ = IntegrationStockWorkflow.ReadStock(mappings, record);
                    }
                    else
                    {
                        items.Add(IntegrationStockWorkflow.ReadCatalogItem(configuration.ProcessType, mappings, record));
                    }
                }
                catch (InvalidOperationException exception)
                {
                    execution.RecordsFailed++;
                    execution.ErrorMessageSafe = "One or more records could not be read.";
                    _logger.LogError($"Integration record could not be read. ConfigurationId: {configuration.Id}, Record: {execution.RecordsRead}, Error: {exception.Message}");
                }
            }

            if (configuration.ProcessType == IntegrationProcessType.GET_STOCK || items.Count == 0)
            {
                return true;
            }

            CatalogSyncResultDto result = await _catalogSync.SyncAsync(new CatalogSyncRequestDto
            {
                OrganizationId = configuration.OrganizationId,
                Mode = configuration.ProcessType == IntegrationProcessType.GET_CATALOG ? CatalogSyncRequestDto.MODE_CATALOG : CatalogSyncRequestDto.MODE_STOCK,
                Items = items
            }, cancellationToken);
            execution.RecordsCreated += result.Created;
            execution.RecordsUpdated += result.Updated;
            if (result.Skipped > 0)
            {
                execution.RecordsFailed += result.Skipped;
                execution.ErrorMessageSafe = "Some SKUs are not in the catalog.";
            }

            return true;
        }

        private static ApiIntegrationExecution NewExecution(ApiIntegrationConfiguration configuration, RunIntegrationCommand request)
        {
            return new ApiIntegrationExecution
            {
                Id = Guid.NewGuid(),
                ConfigurationId = configuration.Id,
                Trigger = request.Trigger,
                Status = IntegrationExecutionStatus.RUNNING,
                StartedAt = DateTime.UtcNow,
                WatermarkBefore = request.FullSync ? null : configuration.LastWatermark
            };
        }

        // The schedule field switches the scheduled pull on; a scheduled integration is pulled every 15 minutes.
        private static DateTime? NextRun(ApiIntegrationConfiguration configuration)
        {
            return string.IsNullOrWhiteSpace(configuration.ScheduleCron) || configuration.Status != IntegrationConfigurationStatus.ACTIVE
                ? null
                : DateTime.UtcNow.AddMinutes(15);
        }
    }
}
