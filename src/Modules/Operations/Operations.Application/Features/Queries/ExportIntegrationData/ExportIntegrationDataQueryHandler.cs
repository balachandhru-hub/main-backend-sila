using System.Globalization;
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

namespace Operations.Application.Features.Queries.ExportIntegrationData
{
    public class ExportIntegrationDataQueryHandler : IRequestHandler<ExportIntegrationDataQuery, FileDownloadDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly IIntegrationSpreadsheetEngine _spreadsheet;

        public ExportIntegrationDataQueryHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            IIntegrationSpreadsheetEngine spreadsheet)
        {
            _repository = repository;
            _logger = logger;
            _spreadsheet = spreadsheet;
        }

        public async Task<FileDownloadDto> Handle(ExportIntegrationDataQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Exporting integration data. ConfigurationId: {request.ConfigurationId}, Kind: {request.Request.Kind}, OrganizationId: {request.OrganizationId}");

            IntegrationDataUpdateRequestDto dto = request.Request;
            ApiIntegrationConfiguration? configuration = await _repository.ApiIntegrationConfiguration
                .FindByCondition(x => x.Id == request.ConfigurationId && x.OrganizationId == request.OrganizationId && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);
            if (configuration == null)
            {
                _logger.LogError($"Integration configuration not found. ConfigurationId: {request.ConfigurationId}, OrganizationId: {request.OrganizationId}");
                throw new NotFoundCustomException("Integration not found.", "The integration configuration does not exist in your organization.");
            }

            bool allEntities = configuration.EntityCode == Common.ENTITY_CODE_ALL;
            string? term = string.IsNullOrWhiteSpace(dto.Search) ? null : dto.Search.Trim();
            List<Dictionary<string, string?>> rows;
            if (dto.Kind == IntegrationImportKind.SUPPLIERS)
            {
                StatusKind? status = Enum.TryParse(dto.Status, true, out StatusKind parsedStatus) ? parsedStatus : null;
                List<SupplierMaster> suppliers = await _repository.SupplierMaster
                    .FindByCondition(x => x.OrganizationId == request.OrganizationId && (allEntities || x.EntityCode == configuration.EntityCode)
                        && (term == null || x.SupplierCode.Contains(term) || x.Name.Contains(term) || (x.TaxNumber != null && x.TaxNumber.Contains(term)))
                        && (status == null || x.Status == status))
                    .OrderBy(x => x.Name)
                    .Take(10000)
                    .ToListAsync(cancellationToken);
                rows = suppliers.Select(item => new Dictionary<string, string?>
                {
                    ["SUPPLIER_CODE"] = item.SupplierCode,
                    ["NAME"] = item.Name,
                    ["LEGAL_NAME"] = item.LegalName,
                    ["TAX_NUMBER"] = item.TaxNumber,
                    ["EMAIL"] = item.Email,
                    ["PHONE"] = item.Phone,
                    ["COUNTRY"] = item.Country,
                    ["CURRENCY"] = item.Currency,
                    ["EXTERNAL_ID"] = item.ExternalId,
                }).ToList();
            }
            else
            {
                PurchaseOrderStatus? status = Enum.TryParse(dto.Status, true, out PurchaseOrderStatus parsedStatus) ? parsedStatus : null;
                List<PurchaseOrder> orders = await _repository.PurchaseOrder
                    .FindByCondition(x => x.OrganizationId == request.OrganizationId && (allEntities || x.EntityCode == configuration.EntityCode)
                        && (term == null || x.PoNumber.Contains(term) || (x.SupplierName != null && x.SupplierName.Contains(term)) || x.Currency.Contains(term))
                        && (status == null || x.Status == status))
                    .OrderByDescending(x => x.LastSyncedAt)
                    .Take(10000)
                    .ToListAsync(cancellationToken);
                List<Guid> supplierIds = orders.Select(x => x.SupplierId).Distinct().ToList();
                Dictionary<Guid, SupplierMaster> suppliers = await _repository.SupplierMaster
                    .FindByCondition(x => supplierIds.Contains(x.Id))
                    .ToDictionaryAsync(x => x.Id, cancellationToken);
                rows = orders.Select(item =>
                {
                    suppliers.TryGetValue(item.SupplierId, out SupplierMaster? supplier);
                    return new Dictionary<string, string?>
                    {
                        ["EXTERNAL_ID"] = item.ExternalId,
                        ["PO_NUMBER"] = item.PoNumber,
                        ["SUPPLIER_CODE"] = supplier?.SupplierCode,
                        ["SUPPLIER_NAME"] = item.SupplierName ?? supplier?.Name,
                        ["CURRENCY"] = item.Currency,
                        ["COMPANY_CODE"] = item.CompanyCode,
                        ["PURCHASE_ORDER_TYPE"] = item.PurchaseOrderType,
                        ["TOTAL_NET_AMOUNT"] = item.TotalNetAmount?.ToString(CultureInfo.InvariantCulture),
                        ["TOTAL_TAX_AMOUNT"] = item.TotalTaxAmount?.ToString(CultureInfo.InvariantCulture),
                        ["TOTAL_AMOUNT"] = item.TotalAmount?.ToString(CultureInfo.InvariantCulture),
                        ["PO_DATE"] = item.PoDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                        ["DELIVERY_DATE"] = item.DeliveryDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                        ["SOURCE_LAST_CHANGED_AT"] = item.SourceLastChangedAt?.ToString("O", CultureInfo.InvariantCulture),
                    };
                }).ToList();
            }

            _logger.LogInfo($"Integration data exported. ConfigurationId: {configuration.Id}, Kind: {dto.Kind}, Rows: {rows.Count}");
            return new FileDownloadDto
            {
                FileName = $"{dto.Kind.ToString().ToLowerInvariant()}-export.xlsx",
                ContentType = Common.SPREADSHEET_CONTENT_TYPE,
                FileBytes = _spreadsheet.BuildSpreadsheet(_spreadsheet.Columns(dto.Kind), rows)
            };
        }
    }
}
