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

namespace Operations.Application.Features.Commands.CommitIntegrationImport
{
    public class CommitIntegrationImportCommandHandler : IRequestHandler<CommitIntegrationImportCommand, IntegrationImportCommitResponseDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly IIntegrationSpreadsheetEngine _spreadsheet;

        public CommitIntegrationImportCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            IIntegrationSpreadsheetEngine spreadsheet)
        {
            _repository = repository;
            _logger = logger;
            _spreadsheet = spreadsheet;
        }

        public async Task<IntegrationImportCommitResponseDto> Handle(CommitIntegrationImportCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Committing integration import. ConfigurationId: {request.ConfigurationId}, Kind: {request.Request.Kind}, Rows: {request.Request.Rows.Count}, OrganizationId: {request.OrganizationId}, UserId: {request.UserId}");

            IntegrationImportCommitInputDto dto = request.Request;
            ApiIntegrationConfiguration configuration = await IntegrationConfigurationRules.GetTrackedAsync(_repository, _logger, request.ConfigurationId, request.OrganizationId);
            if (dto.Rows.Count == 0)
            {
                throw new BadRequestCustomException("Import is empty.", "There are no rows to commit.");
            }

            if (dto.Rows.Count > Common.MAX_IMPORT_ROWS)
            {
                throw new BadRequestCustomException("Import file is too large.", "A spreadsheet can contain at most 5,000 data rows.");
            }

            // The rows come back from the browser, so they are normalized and validated again.
            List<IntegrationImportRowResponseDto> normalized = _spreadsheet.Normalize(dto.Kind, dto.Rows);
            if (normalized.Any(item => !item.IsValid))
            {
                _logger.LogError($"Import contains invalid rows. ConfigurationId: {configuration.Id}, Invalid: {normalized.Count(item => !item.IsValid)}");
                throw new BadRequestCustomException("Import validation failed.", "The spreadsheet contains invalid rows. No records were changed.");
            }

            List<ApiFieldMapping> mappings = _spreadsheet.ImportMappings(dto.Kind);
            ApiIntegrationExecution execution = new ApiIntegrationExecution
            {
                Id = Guid.NewGuid(),
                ConfigurationId = configuration.Id,
                Trigger = IntegrationExecutionTrigger.EXCEL_IMPORT,
                Status = IntegrationExecutionStatus.RUNNING,
                StartedAt = DateTime.UtcNow
            };
            _repository.ApiIntegrationExecution.Create(execution);
            try
            {
                foreach (IntegrationImportRowResponseDto row in normalized)
                {
                    execution.RecordsRead++;
                    JsonElement record = _spreadsheet.ToCanonicalRecord(dto.Kind, row.Values);
                    bool created;
                    try
                    {
                        created = dto.Kind == IntegrationImportKind.SUPPLIERS
                            ? await IntegrationImportWorkflow.UpsertSupplierAsync(_repository, configuration, mappings, record, cancellationToken)
                            : await IntegrationImportWorkflow.UpsertPurchaseOrderAsync(_repository, configuration, mappings, record, cancellationToken);
                    }
                    catch (Exception exception) when (exception is InvalidOperationException or FormatException)
                    {
                        throw new IntegrationException("IMPORT_ROW_INVALID", $"Row {row.RowNumber}: {exception.Message}");
                    }
                    catch (IntegrationException exception)
                    {
                        throw new IntegrationException(exception.Code, $"Row {row.RowNumber}: {exception.Message}");
                    }

                    if (created)
                    {
                        execution.RecordsCreated++;
                    }
                    else
                    {
                        execution.RecordsUpdated++;
                    }
                }

                execution.Status = IntegrationExecutionStatus.SUCCESS;
                execution.CompletedAt = DateTime.UtcNow;

                // One SaveChanges = one database transaction: every row is written or none is.
                await _repository.SaveAsync();
            }
            catch (IntegrationException exception)
            {
                _repository.ApiIntegrationExecution.DetachAllEntities();
                _logger.LogError($"Import could not be committed. ConfigurationId: {request.ConfigurationId}, Code: {exception.Code}, Error: {exception.Message}");
                throw new BadRequestCustomException("Import could not be committed.", $"{exception.Message} No records were changed.");
            }
            catch (DbUpdateException exception)
            {
                _repository.ApiIntegrationExecution.DetachAllEntities();
                _logger.LogError($"Import could not be committed. ConfigurationId: {request.ConfigurationId}, Error: {exception.Message}");
                throw new BadRequestCustomException("Import could not be committed.", "The spreadsheet could not be saved. No records were changed.");
            }

            _logger.LogInfo($"Integration import committed. ConfigurationId: {configuration.Id}, Created: {execution.RecordsCreated}, Updated: {execution.RecordsUpdated}");
            return new IntegrationImportCommitResponseDto
            {
                Execution = ResponseBuilder.Execution(execution),
                RecordsCommitted = normalized.Count
            };
        }
    }
}
