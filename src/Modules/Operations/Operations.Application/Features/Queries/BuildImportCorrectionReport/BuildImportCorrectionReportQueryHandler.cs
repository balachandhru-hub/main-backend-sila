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

namespace Operations.Application.Features.Queries.BuildImportCorrectionReport
{
    public class BuildImportCorrectionReportQueryHandler : IRequestHandler<BuildImportCorrectionReportQuery, FileDownloadDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly IIntegrationSpreadsheetEngine _spreadsheet;

        public BuildImportCorrectionReportQueryHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            IIntegrationSpreadsheetEngine spreadsheet)
        {
            _repository = repository;
            _logger = logger;
            _spreadsheet = spreadsheet;
        }

        public async Task<FileDownloadDto> Handle(BuildImportCorrectionReportQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Building import correction report. ConfigurationId: {request.ConfigurationId}, Kind: {request.Request.Kind}, OrganizationId: {request.OrganizationId}");

            bool exists = await _repository.ApiIntegrationConfiguration
                .FindByCondition(x => x.Id == request.ConfigurationId && x.OrganizationId == request.OrganizationId && x.IsActive)
                .AnyAsync(cancellationToken);
            if (!exists)
            {
                _logger.LogError($"Integration configuration not found. ConfigurationId: {request.ConfigurationId}, OrganizationId: {request.OrganizationId}");
                throw new NotFoundCustomException("Integration not found.", "The integration configuration does not exist in your organization.");
            }

            if (request.Request.Rows.Count > Common.MAX_IMPORT_ROWS)
            {
                throw new BadRequestCustomException("Import file is too large.", "A spreadsheet can contain at most 5,000 data rows.");
            }

            return new FileDownloadDto
            {
                FileName = $"{request.Request.Kind.ToString().ToLowerInvariant()}-correction-report.csv",
                ContentType = Common.CSV_CONTENT_TYPE,
                FileBytes = _spreadsheet.BuildCorrectionReport(request.Request.Kind, request.Request.Rows)
            };
        }
    }
}
