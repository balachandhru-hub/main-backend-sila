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

namespace Operations.Application.Features.Queries.GetImportTemplate
{
    public class GetImportTemplateQueryHandler : IRequestHandler<GetImportTemplateQuery, FileDownloadDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly IIntegrationSpreadsheetEngine _spreadsheet;

        public GetImportTemplateQueryHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            IIntegrationSpreadsheetEngine spreadsheet)
        {
            _repository = repository;
            _logger = logger;
            _spreadsheet = spreadsheet;
        }

        public async Task<FileDownloadDto> Handle(GetImportTemplateQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Building import template. Kind: {request.Kind}, OrganizationId: {request.OrganizationId}");

            byte[] bytes = _spreadsheet.BuildSpreadsheet(_spreadsheet.Columns(request.Kind), new List<Dictionary<string, string?>>());
            return await Task.FromResult(new FileDownloadDto
            {
                FileName = $"{request.Kind.ToString().ToLowerInvariant()}-template.xlsx",
                ContentType = Common.SPREADSHEET_CONTENT_TYPE,
                FileBytes = bytes
            });
        }
    }
}
