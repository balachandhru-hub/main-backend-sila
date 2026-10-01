using MediatR;
using Microsoft.AspNetCore.Http;
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

namespace Operations.Application.Features.Queries.PreviewIntegrationImport
{
    public class PreviewIntegrationImportQueryHandler : IRequestHandler<PreviewIntegrationImportQuery, IntegrationImportPreviewResponseDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly IIntegrationSpreadsheetEngine _spreadsheet;

        public PreviewIntegrationImportQueryHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            IIntegrationSpreadsheetEngine spreadsheet)
        {
            _repository = repository;
            _logger = logger;
            _spreadsheet = spreadsheet;
        }

        public async Task<IntegrationImportPreviewResponseDto> Handle(PreviewIntegrationImportQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Previewing integration import. ConfigurationId: {request.ConfigurationId}, Kind: {request.Kind}, OrganizationId: {request.OrganizationId}");

            bool exists = await _repository.ApiIntegrationConfiguration
                .FindByCondition(x => x.Id == request.ConfigurationId && x.OrganizationId == request.OrganizationId && x.IsActive)
                .AnyAsync(cancellationToken);
            if (!exists)
            {
                _logger.LogError($"Integration configuration not found. ConfigurationId: {request.ConfigurationId}, OrganizationId: {request.OrganizationId}");
                throw new NotFoundCustomException("Integration not found.", "The integration configuration does not exist in your organization.");
            }

            IFormFile? file = request.File;
            if (file == null || file.Length == 0)
            {
                _logger.LogError($"Import file is missing. ConfigurationId: {request.ConfigurationId}");
                throw new BadRequestCustomException("Import file is required.", "Choose an .xlsx or .csv file to preview.");
            }

            if (file.Length > Common.MAX_IMPORT_SIZE)
            {
                _logger.LogError($"Import file is too large. ConfigurationId: {request.ConfigurationId}, Bytes: {file.Length}");
                throw new BadRequestCustomException("Import file is too large.", "The spreadsheet must be smaller than 10 MB.");
            }

            byte[] content;
            await using (MemoryStream memory = new MemoryStream())
            {
                await file.CopyToAsync(memory, cancellationToken);
                content = memory.ToArray();
            }

            List<Dictionary<string, string?>> rows;
            try
            {
                rows = _spreadsheet.Read(content, file.FileName);
            }
            catch (IntegrationException exception)
            {
                _logger.LogError($"Import file could not be read. ConfigurationId: {request.ConfigurationId}, Code: {exception.Code}");
                throw IntegrationErrors.ToCustomException(exception);
            }

            if (rows.Count == 0)
            {
                throw new BadRequestCustomException("Import file is empty.", "The spreadsheet does not contain any data rows.");
            }

            if (rows.Count > Common.MAX_IMPORT_ROWS)
            {
                throw new BadRequestCustomException("Import file is too large.", "A spreadsheet can contain at most 5,000 data rows.");
            }

            List<IntegrationImportRowResponseDto> normalized = _spreadsheet.Normalize(request.Kind, rows);
            _logger.LogInfo($"Integration import previewed. ConfigurationId: {request.ConfigurationId}, Rows: {normalized.Count}, Invalid: {normalized.Count(item => !item.IsValid)}");
            return new IntegrationImportPreviewResponseDto
            {
                ConfigurationId = request.ConfigurationId,
                Kind = request.Kind.ToString(),
                FileName = Path.GetFileName(file.FileName),
                TotalRows = normalized.Count,
                ValidRows = normalized.Count(item => item.IsValid),
                InvalidRows = normalized.Count(item => !item.IsValid),
                Columns = _spreadsheet.Columns(request.Kind),
                Rows = normalized
            };
        }
    }
}
