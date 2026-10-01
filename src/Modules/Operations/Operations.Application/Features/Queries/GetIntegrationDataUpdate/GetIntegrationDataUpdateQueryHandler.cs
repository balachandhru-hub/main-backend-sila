using MediatR;
using Microsoft.EntityFrameworkCore;
using Operations.Application.Features.Shared;
using Operations.Domain.Common;
using Operations.Domain.Dtos;
using Operations.Domain.Entities;
using Operations.Domain.Enums;
using Operations.Infrastructure.Contracts.IRepository;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Operations.Application.Features.Queries.GetIntegrationDataUpdate
{
    public class GetIntegrationDataUpdateQueryHandler : IRequestHandler<GetIntegrationDataUpdateQuery, IntegrationDataUpdateResponseDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetIntegrationDataUpdateQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<IntegrationDataUpdateResponseDto> Handle(GetIntegrationDataUpdateQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching integration data. ConfigurationId: {request.ConfigurationId}, Kind: {request.Request.Kind}, Page: {request.Request.Page}, OrganizationId: {request.OrganizationId}");

            IntegrationDataUpdateRequestDto dto = request.Request;
            ApiIntegrationConfiguration? configuration = await _repository.ApiIntegrationConfiguration
                .FindByCondition(x => x.Id == request.ConfigurationId && x.OrganizationId == request.OrganizationId && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);
            if (configuration == null)
            {
                _logger.LogError($"Integration configuration not found. ConfigurationId: {request.ConfigurationId}, OrganizationId: {request.OrganizationId}");
                throw new NotFoundCustomException("Integration not found.", "The integration configuration does not exist in your organization.");
            }

            int page = Math.Max(1, dto.Page);
            int pageSize = Math.Clamp(dto.PageSize, 10, 100);
            bool allEntities = configuration.EntityCode == Common.ENTITY_CODE_ALL;
            IQueryable<SupplierMaster> suppliers = _repository.SupplierMaster
                .FindByCondition(x => x.OrganizationId == request.OrganizationId && (allEntities || x.EntityCode == configuration.EntityCode));
            int supplierCount = await suppliers.CountAsync(cancellationToken);
            int purchaseOrderCount = await _repository.PurchaseOrder
                .FindByCondition(x => x.OrganizationId == request.OrganizationId && x.SourceConfigurationId == configuration.Id)
                .CountAsync(cancellationToken);

            List<IntegrationPurchaseOrderRowDto> purchaseOrderRows = new List<IntegrationPurchaseOrderRowDto>();
            List<IntegrationSupplierRowDto> supplierRows = new List<IntegrationSupplierRowDto>();
            int totalRows;
            DateTime? lastSynced;
            if (dto.Kind == IntegrationImportKind.SUPPLIERS)
            {
                if (!string.IsNullOrWhiteSpace(dto.Search))
                {
                    string term = dto.Search.Trim();
                    suppliers = suppliers.Where(x => x.SupplierCode.Contains(term) || x.Name.Contains(term) || (x.TaxNumber != null && x.TaxNumber.Contains(term)));
                }

                if (!string.IsNullOrWhiteSpace(dto.Status) && Enum.TryParse(dto.Status, true, out StatusKind supplierStatus))
                {
                    suppliers = suppliers.Where(x => x.Status == supplierStatus);
                }

                suppliers = (dto.SortBy ?? string.Empty).ToLowerInvariant() switch
                {
                    "suppliercode" => dto.Descending ? suppliers.OrderByDescending(x => x.SupplierCode) : suppliers.OrderBy(x => x.SupplierCode),
                    "status" => dto.Descending ? suppliers.OrderByDescending(x => x.Status) : suppliers.OrderBy(x => x.Status),
                    "updatedat" => dto.Descending ? suppliers.OrderByDescending(x => x.DateUpdated) : suppliers.OrderBy(x => x.DateUpdated),
                    _ => dto.Descending ? suppliers.OrderByDescending(x => x.Name) : suppliers.OrderBy(x => x.Name),
                };
                totalRows = await suppliers.CountAsync(cancellationToken);
                supplierRows = await suppliers.Skip((page - 1) * pageSize).Take(pageSize)
                    .Select(x => new IntegrationSupplierRowDto
                    {
                        Id = x.Id,
                        SupplierCode = x.SupplierCode,
                        Name = x.Name,
                        TaxNumber = x.TaxNumber,
                        Status = x.Status,
                        UpdatedAt = x.DateUpdated
                    })
                    .ToListAsync(cancellationToken);
                lastSynced = await _repository.SupplierMaster
                    .FindByCondition(x => x.OrganizationId == request.OrganizationId && (allEntities || x.EntityCode == configuration.EntityCode))
                    .MaxAsync(x => x.LastSyncedAt, cancellationToken);
            }
            else
            {
                totalRows = await _repository.PurchaseOrder.CountIntegrationRowsAsync(request.OrganizationId, configuration.Id, dto, cancellationToken);
                purchaseOrderRows = await _repository.PurchaseOrder.ListIntegrationRowsAsync(request.OrganizationId, configuration.Id, dto, cancellationToken);
                lastSynced = await _repository.PurchaseOrder
                    .FindByCondition(x => x.OrganizationId == request.OrganizationId && x.SourceConfigurationId == configuration.Id)
                    .MaxAsync(x => x.LastSyncedAt, cancellationToken);
            }

            _logger.LogInfo($"Integration data fetched. ConfigurationId: {configuration.Id}, Kind: {dto.Kind}, TotalRows: {totalRows}");
            return new IntegrationDataUpdateResponseDto
            {
                ConfigurationId = configuration.Id,
                PurchaseOrders = purchaseOrderCount,
                Suppliers = supplierCount,
                LastSyncedAt = lastSynced,
                LastWatermark = configuration.LastWatermark,
                IsRunning = configuration.IsRunning,
                LastErrorSafe = configuration.LastErrorSafe,
                PurchaseOrderRows = purchaseOrderRows,
                SupplierRows = supplierRows,
                Kind = dto.Kind.ToString(),
                TotalRows = totalRows,
                Page = page,
                PageSize = pageSize
            };
        }
    }
}
