using Microsoft.EntityFrameworkCore;
using Operations.Application.Services.Integration;
using Operations.Domain.Dtos;
using Operations.Domain.Entities;
using Operations.Domain.Enums;
using Operations.Infrastructure.Contracts.IRepository;

namespace Operations.Application.Features.Shared
{
    /// <summary>
    /// Builds the response DTOs of the receiving screens. Centralized here because a list query,
    /// a detail query and the command that returns the changed record must produce the same shape.
    /// Entities only hold plain ids of related records, so the names are looked up here in bulk.
    /// </summary>
    internal static class ResponseBuilder
    {
        public static async Task<List<InvoiceResponseDto>> InvoicesAsync(IRepositoryWrapper repository, List<Invoice> invoices, CancellationToken cancellationToken)
        {
            List<Guid> invoiceIds = invoices.Select(x => x.Id).ToList();
            List<Guid> supplierIds = invoices.Where(x => x.SupplierId != null).Select(x => x.SupplierId!.Value).Distinct().ToList();
            List<Guid> orderIds = invoices.Where(x => x.PurchaseOrderId != null).Select(x => x.PurchaseOrderId!.Value).Distinct().ToList();
            List<Guid> unitIds = invoices.Where(x => x.OperatingUnitId != null).Select(x => x.OperatingUnitId!.Value).Distinct().ToList();

            List<InvoiceLine> lines = await repository.InvoiceLine
                .FindByCondition(x => invoiceIds.Contains(x.InvoiceId))
                .ToListAsync(cancellationToken);
            Dictionary<Guid, string> supplierCodes = await repository.SupplierMaster
                .FindByCondition(x => supplierIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.SupplierCode, cancellationToken);
            Dictionary<Guid, string> orderNumbers = await repository.PurchaseOrder
                .FindByCondition(x => orderIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.PoNumber, cancellationToken);
            Dictionary<Guid, string> unitNames = await UnitNamesAsync(repository, unitIds, cancellationToken);
            List<GoodsReceipt> receipts = await repository.GoodsReceipt
                .FindByCondition(x => x.InvoiceId != null && invoiceIds.Contains(x.InvoiceId.Value) && x.Status == GoodsReceiptStatus.POSTED)
                .ToListAsync(cancellationToken);

            return invoices.Select(invoice => new InvoiceResponseDto
            {
                Id = invoice.Id,
                DocumentId = invoice.DocumentId,
                InvoiceNumber = invoice.InvoiceNumber,
                InvoiceDate = invoice.InvoiceDate,
                SupplierName = invoice.SupplierNameRaw,
                SupplierTaxNumber = invoice.SupplierTaxNumberRaw,
                PurchaseOrderNumber = invoice.PurchaseOrderId != null && orderNumbers.TryGetValue(invoice.PurchaseOrderId.Value, out string? poNumber)
                    ? poNumber
                    : invoice.PoNumberRaw,
                PurchaseOrderId = invoice.PurchaseOrderId,
                NoPurchaseOrder = invoice.NoPurchaseOrder,
                Currency = invoice.Currency,
                NetAmount = invoice.NetAmount,
                TaxAmount = invoice.TaxAmount,
                GrossAmount = invoice.GrossAmount,
                InvoiceType = invoice.InvoiceType,
                Status = invoice.Status,
                OverallConfidence = invoice.OverallConfidence,
                OrganizationId = invoice.OrganizationId,
                OperatingUnitId = invoice.OperatingUnitId,
                OperatingUnitName = invoice.OperatingUnitId != null && unitNames.TryGetValue(invoice.OperatingUnitId.Value, out string? unitName) ? unitName : null,
                SupplierId = invoice.SupplierId,
                SupplierCode = invoice.SupplierId != null && supplierCodes.TryGetValue(invoice.SupplierId.Value, out string? supplierCode) ? supplierCode : null,
                GoodsReceiptId = receipts.FirstOrDefault(receipt => receipt.InvoiceId == invoice.Id)?.Id,
                Lines = lines.Where(line => line.InvoiceId == invoice.Id).OrderBy(line => line.LineNumber).Select(line => new InvoiceLineResponseDto
                {
                    Id = line.Id,
                    LineNumber = line.LineNumber,
                    SupplierMaterialCode = line.SupplierMaterialCode,
                    MaterialId = line.MaterialId,
                    Description = line.DescriptionRaw,
                    Quantity = line.Quantity,
                    Uom = line.Uom,
                    UnitPrice = line.UnitPrice,
                    TaxRate = line.TaxRate,
                    TaxAmount = line.TaxAmount,
                    LineAmount = line.LineAmount,
                    Confidence = line.Confidence,
                    MatchStatus = line.MatchStatus,
                    PurchaseOrderItemId = line.PurchaseOrderItemId
                }).ToList(),
                CreatedAt = invoice.DateCreated,
                UpdatedAt = invoice.DateUpdated
            }).ToList();
        }

        public static async Task<InvoiceResponseDto> InvoiceAsync(IRepositoryWrapper repository, Invoice invoice, CancellationToken cancellationToken)
        {
            List<InvoiceResponseDto> result = await InvoicesAsync(repository, new List<Invoice> { invoice }, cancellationToken);
            return result[0];
        }

        public static async Task<List<PurchaseOrderResponseDto>> PurchaseOrdersAsync(IRepositoryWrapper repository, List<PurchaseOrder> orders, CancellationToken cancellationToken)
        {
            List<Guid> orderIds = orders.Select(x => x.Id).ToList();
            List<Guid> supplierIds = orders.Select(x => x.SupplierId).Distinct().ToList();
            List<Guid> unitIds = orders.Where(x => x.OperatingUnitId != null).Select(x => x.OperatingUnitId!.Value).Distinct().ToList();

            List<PurchaseOrderItem> items = await repository.PurchaseOrderItem
                .FindByCondition(x => orderIds.Contains(x.PurchaseOrderId))
                .ToListAsync(cancellationToken);
            Dictionary<Guid, string> supplierNames = await repository.SupplierMaster
                .FindByCondition(x => supplierIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);
            Dictionary<Guid, string> unitNames = await UnitNamesAsync(repository, unitIds, cancellationToken);

            return orders.Select(order => new PurchaseOrderResponseDto
            {
                Id = order.Id,
                PoNumber = order.PoNumber,
                PoDate = order.PoDate,
                DeliveryDate = order.DeliveryDate,
                Currency = order.Currency,
                Status = order.Status,
                OrganizationId = order.OrganizationId,
                OperatingUnitId = order.OperatingUnitId,
                OperatingUnitName = order.OperatingUnitId != null && unitNames.TryGetValue(order.OperatingUnitId.Value, out string? unitName) ? unitName : null,
                SupplierId = order.SupplierId,
                SupplierName = supplierNames.TryGetValue(order.SupplierId, out string? supplierName) ? supplierName : order.SupplierName ?? string.Empty,
                Items = items.Where(item => item.PurchaseOrderId == order.Id).OrderBy(item => item.LineNumber).Select(item => new PurchaseOrderItemResponseDto
                {
                    Id = item.Id,
                    LineNumber = item.LineNumber,
                    MaterialId = item.MaterialId,
                    MaterialCode = item.MaterialCode,
                    Description = item.Description,
                    OrderedQuantity = item.OrderedQuantity,
                    ReceivedQuantity = item.ReceivedQuantity,
                    OpenQuantity = item.OpenQuantity,
                    Uom = item.Uom,
                    UnitPrice = item.UnitPrice,
                    Status = item.Status,
                    ItemNumber = item.ItemNumber,
                    PriceQuantity = item.PriceQuantity,
                    ItemAmount = item.ItemAmount,
                    TaxCode = item.TaxCode,
                    TaxAmount = item.TaxAmount,
                    GrossItemAmount = item.GrossItemAmount,
                    Currency = item.Currency,
                    MaterialGroup = item.MaterialGroup,
                    Plant = item.Plant,
                    StorageLocation = item.StorageLocation,
                    ItemCategory = item.ItemCategory,
                    AccountAssignmentCategory = item.AccountAssignmentCategory,
                    GoodsReceiptExpected = item.GoodsReceiptExpected,
                    InvoiceExpected = item.InvoiceExpected,
                    DeliveryCompleted = item.DeliveryCompleted,
                    DeletionIndicator = item.DeletionIndicator
                }).ToList(),
                EntityCode = order.EntityCode,
                PurchaseOrderType = order.PurchaseOrderType,
                CompanyCode = order.CompanyCode,
                ErpSupplierId = order.ErpSupplierId,
                PurchasingOrganization = order.PurchasingOrganization,
                PurchasingGroup = order.PurchasingGroup,
                PaymentTerms = order.PaymentTerms,
                PoCategory = order.PoCategory,
                TotalNetAmount = order.TotalNetAmount,
                TotalTaxAmount = order.TotalTaxAmount,
                TotalAmount = order.TotalAmount,
                TotalOrderedQuantity = order.TotalOrderedQuantity,
                TotalReceivedQuantity = order.TotalReceivedQuantity,
                SourceSystem = order.SourceSystem,
                SourceLastChangedAt = order.SourceLastChangedAt,
                LastSyncedAt = order.LastSyncedAt
            }).ToList();
        }

        public static async Task<List<GoodsReceiptResponseDto>> GoodsReceiptsAsync(IRepositoryWrapper repository, List<GoodsReceipt> receipts, CancellationToken cancellationToken)
        {
            List<Guid> receiptIds = receipts.Select(x => x.Id).ToList();
            List<Guid> orderIds = receipts.Select(x => x.PurchaseOrderId).Distinct().ToList();
            List<Guid> supplierIds = receipts.Select(x => x.SupplierId).Distinct().ToList();
            List<Guid> unitIds = receipts.Select(x => x.OperatingUnitId).Distinct().ToList();
            List<Guid> invoiceIds = receipts.Where(x => x.InvoiceId != null).Select(x => x.InvoiceId!.Value).Distinct().ToList();

            List<GoodsReceiptLine> lines = await repository.GoodsReceiptLine
                .FindByCondition(x => receiptIds.Contains(x.GoodsReceiptId))
                .ToListAsync(cancellationToken);
            Dictionary<Guid, string> orderNumbers = await repository.PurchaseOrder
                .FindByCondition(x => orderIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.PoNumber, cancellationToken);
            Dictionary<Guid, int> itemLineNumbers = await repository.PurchaseOrderItem
                .FindByCondition(x => orderIds.Contains(x.PurchaseOrderId))
                .ToDictionaryAsync(x => x.Id, x => x.LineNumber, cancellationToken);
            Dictionary<Guid, string> supplierNames = await repository.SupplierMaster
                .FindByCondition(x => supplierIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);
            Dictionary<Guid, string> invoiceNumbers = await repository.Invoice
                .FindByCondition(x => invoiceIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.InvoiceNumber, cancellationToken);
            Dictionary<Guid, string> unitNames = await UnitNamesAsync(repository, unitIds, cancellationToken);

            return receipts.Select(receipt => new GoodsReceiptResponseDto
            {
                Id = receipt.Id,
                GrnNumber = receipt.GrnNumber,
                Status = receipt.Status,
                PurchaseOrderId = receipt.PurchaseOrderId,
                PurchaseOrderNumber = orderNumbers.TryGetValue(receipt.PurchaseOrderId, out string? poNumber) ? poNumber : string.Empty,
                InvoiceId = receipt.InvoiceId,
                InvoiceNumber = receipt.InvoiceId != null && invoiceNumbers.TryGetValue(receipt.InvoiceId.Value, out string? invoiceNumber) ? invoiceNumber : null,
                SupplierId = receipt.SupplierId,
                SupplierName = supplierNames.TryGetValue(receipt.SupplierId, out string? supplierName) ? supplierName : string.Empty,
                OrganizationId = receipt.OrganizationId,
                OperatingUnitId = receipt.OperatingUnitId,
                OperatingUnitName = unitNames.TryGetValue(receipt.OperatingUnitId, out string? unitName) ? unitName : string.Empty,
                ReceiptDate = receipt.ReceiptDate,
                CreatedAt = receipt.DateCreated,
                PostedAt = receipt.PostedAt,
                BusinessStatus = receipt.BusinessStatus,
                ErpPostingStatus = receipt.ErpPostingStatus,
                ErpMaterialDocument = receipt.ErpMaterialDocument,
                ErpDocumentYear = receipt.ErpDocumentYear,
                ErpResponseJson = receipt.ErpResponseJson,
                FailureCode = receipt.FailureCode,
                FailureMessage = receipt.FailureMessage,
                ErpAttemptCount = receipt.ErpAttemptCount,
                Lines = lines.Where(line => line.GoodsReceiptId == receipt.Id)
                    .Select(line => new GoodsReceiptLineResponseDto
                    {
                        Id = line.Id,
                        PurchaseOrderItemId = line.PurchaseOrderItemId,
                        PurchaseOrderLineNumber = itemLineNumbers.TryGetValue(line.PurchaseOrderItemId, out int lineNumber) ? lineNumber : 0,
                        MaterialCode = line.MaterialCode,
                        Description = line.Description,
                        OpenQuantityBefore = line.OpenQuantityBefore,
                        InvoiceQuantity = line.InvoiceQuantity,
                        ReceivedQuantity = line.ReceivedQuantity,
                        AcceptedQuantity = line.AcceptedQuantity,
                        DamagedQuantity = line.DamagedQuantity,
                        RejectedQuantity = line.RejectedQuantity,
                        Uom = line.Uom,
                        BatchNumber = line.BatchNumber,
                        ExpiryDate = line.ExpiryDate
                    })
                    .OrderBy(line => line.PurchaseOrderLineNumber)
                    .ToList()
            }).ToList();
        }

        public static async Task<GoodsReceiptResponseDto> GoodsReceiptAsync(IRepositoryWrapper repository, GoodsReceipt receipt, CancellationToken cancellationToken)
        {
            List<GoodsReceiptResponseDto> result = await GoodsReceiptsAsync(repository, new List<GoodsReceipt> { receipt }, cancellationToken);
            return result[0];
        }

        public static async Task<List<SupplierResponseDto>> SuppliersAsync(IRepositoryWrapper repository, List<SupplierMaster> suppliers, CancellationToken cancellationToken)
        {
            List<Guid> supplierIds = suppliers.Select(x => x.Id).ToList();
            List<SupplierAlias> aliases = await repository.SupplierAlias
                .FindByCondition(x => supplierIds.Contains(x.SupplierId))
                .ToListAsync(cancellationToken);
            return suppliers.Select(supplier => new SupplierResponseDto
            {
                Id = supplier.Id,
                SupplierCode = supplier.SupplierCode,
                Name = supplier.Name,
                LegalName = supplier.LegalName,
                TaxNumber = supplier.TaxNumber,
                Email = supplier.Email,
                Phone = supplier.Phone,
                EntityCode = supplier.EntityCode,
                Country = supplier.Country,
                Currency = supplier.Currency,
                IsBlocked = supplier.IsBlocked,
                IsDeleted = supplier.IsDeleted,
                Status = supplier.Status,
                Aliases = aliases.Where(alias => alias.SupplierId == supplier.Id).Select(alias => alias.Alias).OrderBy(alias => alias).ToList(),
                LastSyncedAt = supplier.LastSyncedAt,
                UpdatedAt = supplier.DateUpdated,
                SearchName = supplier.SearchName,
                BusinessPartnerId = supplier.BusinessPartnerId,
                Trn = supplier.Trn,
                City = supplier.City,
                PostalCode = supplier.PostalCode,
                Street = supplier.Street,
                IsActive = supplier.IsActive,
                SourceSystem = supplier.SourceSystem,
                SourceLastChangedAt = supplier.SourceLastChangedAt
            }).ToList();
        }

        public static DocumentResponseDto DocumentResponse(Document document, Guid invoiceId, string saveStatus, string? message)
        {
            return new DocumentResponseDto
            {
                Id = document.Id,
                Filename = document.OriginalFilename,
                ContentType = document.ContentType,
                FileSizeBytes = document.FileSizeBytes,
                PageCount = document.PageCount,
                SourceChannel = document.SourceChannel,
                Status = document.Status,
                CreatedAt = document.DateCreated,
                InvoiceId = invoiceId,
                SaveStatus = saveStatus,
                NextStep = "PO_MATCH",
                Message = message
            };
        }

        public static IntegrationConfigurationResponseDto Integration(ApiIntegrationConfiguration item, IIntegrationCredentialProtector credentials)
        {
            return new IntegrationConfigurationResponseDto
            {
                Id = item.Id,
                OrganizationId = item.OrganizationId,
                OrganizationUnitId = item.OrganizationUnitId,
                EntityCode = item.EntityCode,
                Name = item.Name,
                ProcessType = item.ProcessType,
                Protocol = item.Protocol,
                BaseUrl = item.BaseUrl,
                ResourcePath = item.ResourcePath,
                AuthenticationType = item.AuthenticationType,
                Username = item.Username,
                CredentialStatus = credentials.State(item),
                TimeoutSeconds = item.TimeoutSeconds,
                RetryCount = item.RetryCount,
                PageSize = item.PageSize,
                WatermarkField = item.WatermarkField,
                LastWatermark = item.LastWatermark,
                LastAttemptAt = item.LastAttemptAt,
                LastSuccessfulRunAt = item.LastSuccessfulRunAt,
                NextRunAt = item.NextRunAt,
                IsRunning = item.IsRunning,
                LastErrorSafe = item.LastErrorSafe,
                ScheduleCron = item.ScheduleCron,
                Status = item.Status,
                TestedAt = item.TestedAt,
                CreatedAt = item.DateCreated,
                UpdatedAt = item.DateUpdated,
                SystemName = item.SystemName,
                HttpMethod = item.HttpMethod,
                PayloadFormat = item.PayloadFormat,
                RequestBody = item.RequestBody,
                Headers = IntegrationConfigurationRules.ReadHeaders(item.HeadersJson),
                ApiKeyHeader = item.ApiKeyHeader
            };
        }

        public static IntegrationMappingResponseDto Mapping(ApiFieldMapping item)
        {
            return new IntegrationMappingResponseDto
            {
                Id = item.Id,
                ConfigurationId = item.ConfigurationId,
                SourceField = item.SourceField,
                TargetField = item.TargetField,
                Transformation = item.Transformation,
                NullPolicy = item.NullPolicy,
                DefaultValue = item.DefaultValue,
                IsValidated = item.IsValidated,
                UpdatedAt = item.DateUpdated
            };
        }

        public static IntegrationExecutionResponseDto Execution(ApiIntegrationExecution item)
        {
            return new IntegrationExecutionResponseDto
            {
                Id = item.Id,
                ConfigurationId = item.ConfigurationId,
                Trigger = item.Trigger,
                Status = item.Status,
                StartedAt = item.StartedAt,
                CompletedAt = item.CompletedAt,
                RecordsRead = item.RecordsRead,
                RecordsCreated = item.RecordsCreated,
                RecordsUpdated = item.RecordsUpdated,
                RecordsFailed = item.RecordsFailed,
                WatermarkBefore = item.WatermarkBefore,
                WatermarkAfter = item.WatermarkAfter,
                ErrorCode = item.ErrorCode,
                ErrorMessageSafe = item.ErrorMessageSafe
            };
        }

        public static OrganizationUnitResponseDto Unit(OrganizationUnit unit)
        {
            return new OrganizationUnitResponseDto
            {
                Id = unit.Id,
                OrganizationId = unit.OrganizationId,
                ParentUnitId = unit.ParentUnitId,
                Code = unit.Code,
                Name = unit.Name,
                Kind = unit.Kind,
                Status = unit.Status
            };
        }

        public static StorageConnectionResponseDto StorageConnection(DocumentStorageConnection connection)
        {
            return new StorageConnectionResponseDto
            {
                Id = connection.Id,
                OrganizationId = connection.OrganizationId,
                Provider = connection.Provider,
                Name = connection.Name,
                ConnectionStatus = connection.ConnectionStatus,
                TenantIdentifier = connection.TenantIdentifier,
                SiteIdentifier = connection.SiteIdentifier,
                DriveIdentifier = connection.DriveIdentifier,
                FolderIdentifier = connection.FolderIdentifier,
                DisplayUrl = connection.DisplayUrl,
                DisplayName = connection.DisplayName,
                ValidatedAt = connection.ValidatedAt
            };
        }

        public static InvoiceOcrConfigurationResponseDto OcrConfiguration(InvoiceOcrConfiguration configuration)
        {
            return new InvoiceOcrConfigurationResponseDto
            {
                Id = configuration.Id,
                OrganizationId = configuration.OrganizationId,
                MobileBasicOcrEnabled = configuration.MobileBasicOcrEnabled,
                AutomaticBackendFallbackEnabled = configuration.AutomaticBackendFallbackEnabled,
                MinimumMobileConfidence = configuration.MinimumMobileConfidence,
                RequireSupplierName = configuration.RequireSupplierName,
                RequireInvoiceNumber = configuration.RequireInvoiceNumber,
                RequirePurchaseOrderNumber = configuration.RequirePurchaseOrderNumber,
                RequireInvoiceAmount = configuration.RequireInvoiceAmount,
                RequireInvoiceDate = configuration.RequireInvoiceDate,
                RequireCurrency = configuration.RequireCurrency,
                RequireSupplierTrn = configuration.RequireSupplierTrn,
                BackendProvider = configuration.BackendProvider,
                AlwaysBackendOnReread = configuration.AlwaysBackendOnReread,
                DetailedLineExtractionEnabled = configuration.DetailedLineExtractionEnabled,
                SupplierMasterValidationEnabled = configuration.SupplierMasterValidationEnabled,
                PurchaseOrderValidationEnabled = configuration.PurchaseOrderValidationEnabled,
                FinancialReconciliationEnabled = configuration.FinancialReconciliationEnabled,
                AmountTolerance = configuration.AmountTolerance,
                BackendTimeoutSeconds = configuration.BackendTimeoutSeconds,
                BackendRetryCount = configuration.BackendRetryCount,
                ReuseCachedOcr = configuration.ReuseCachedOcr,
                Version = configuration.Version,
                UpdatedAt = configuration.DateUpdated
            };
        }

        public static ExtractionAgentConfigResponseDto ExtractionAgent(ExtractionAgentConfig configuration)
        {
            return new ExtractionAgentConfigResponseDto
            {
                Id = configuration.Id,
                OrganizationId = configuration.OrganizationId,
                Name = configuration.Name,
                DocumentType = configuration.DocumentType,
                ProviderType = configuration.ProviderType,
                EndpointUrl = configuration.EndpointUrl,
                AuthenticationType = configuration.AuthenticationType,
                CredentialMask = configuration.CredentialLast4 == null ? null : $"••••••••{configuration.CredentialLast4}",
                Priority = configuration.Priority,
                IsActive = configuration.Enabled,
                CreatedAt = configuration.DateCreated,
                UpdatedAt = configuration.DateUpdated
            };
        }

        private static Task<Dictionary<Guid, string>> UnitNamesAsync(IRepositoryWrapper repository, List<Guid> unitIds, CancellationToken cancellationToken)
        {
            return repository.OrganizationUnit
                .FindByCondition(x => unitIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);
        }
    }
}
