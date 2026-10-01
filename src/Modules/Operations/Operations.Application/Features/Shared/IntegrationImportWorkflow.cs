using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Operations.Application.Services.Integration;
using Operations.Domain.Common;
using Operations.Domain.Entities;
using Operations.Domain.Enums;
using Operations.Infrastructure.Contracts.IRepository;

namespace Operations.Application.Features.Shared
{
    /// <summary>
    /// Writes one mapped record into the supplier master or the purchase orders. The ERP pull and
    /// the spreadsheet import both go through here, so both follow the same rules: a purchase
    /// order must name an existing, active supplier and no value is ever guessed.
    /// Rule violations are reported as <see cref="IntegrationException"/> or
    /// <see cref="InvalidOperationException"/>; the handler decides what that means for the run.
    /// </summary>
    internal static class IntegrationImportWorkflow
    {
        public static void ValidateMappedRecord(ApiIntegrationConfiguration configuration, IReadOnlyList<ApiFieldMapping> mappings, JsonElement record)
        {
            if (configuration.ProcessType == IntegrationProcessType.GET_SUPPLIER)
            {
                if (string.IsNullOrWhiteSpace(IntegrationRecordReader.Read(mappings, "Supplier.SupplierCode", record))
                    || string.IsNullOrWhiteSpace(IntegrationRecordReader.Read(mappings, "Supplier.Name", record)))
                {
                    throw new InvalidOperationException("Supplier code and name are required.");
                }

                return;
            }

            string? currency = IntegrationRecordReader.Read(mappings, "PurchaseOrder.Currency", record);
            if (string.IsNullOrWhiteSpace(IntegrationRecordReader.Read(mappings, "PurchaseOrder.PoNumber", record))
                || string.IsNullOrWhiteSpace(IntegrationRecordReader.Read(mappings, "PurchaseOrder.SupplierCode", record))
                || string.IsNullOrWhiteSpace(IntegrationRecordReader.Read(mappings, "PurchaseOrder.SupplierName", record))
                || string.IsNullOrWhiteSpace(currency))
            {
                throw new InvalidOperationException("PO number, supplier code, supplier name, and currency are required.");
            }

            if (!Regex.IsMatch(currency.Trim().ToUpperInvariant(), "^[A-Z]{3}$"))
            {
                throw new InvalidOperationException("Currency must be a three-letter ISO code.");
            }
        }

        /// <returns>True when the supplier was created, false when it was updated.</returns>
        public static async Task<bool> UpsertSupplierAsync(
            IRepositoryWrapper repository,
            ApiIntegrationConfiguration configuration,
            IReadOnlyList<ApiFieldMapping> mappings,
            JsonElement record,
            CancellationToken cancellationToken)
        {
            string? Get(string target) => IntegrationRecordReader.Read(mappings, target, record);
            string code = Get("Supplier.SupplierCode") ?? throw new InvalidOperationException("Supplier code is missing.");
            string name = Get("Supplier.Name") ?? throw new InvalidOperationException("Supplier name is missing.");
            string? externalId = Get("Supplier.ExternalId");

            SupplierMaster? supplier = await repository.SupplierMaster.FindFirstByConditionAsync(x =>
                x.OrganizationId == configuration.OrganizationId
                && x.EntityCode == configuration.EntityCode
                && ((externalId != null && x.SourceSystem == Common.SOURCE_SYSTEM_INTEGRATION && x.ExternalId == externalId) || x.SupplierCode == code));
            bool created = supplier == null;
            if (supplier == null)
            {
                supplier = new SupplierMaster
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = configuration.OrganizationId,
                    EntityCode = configuration.EntityCode,
                    Status = StatusKind.ACTIVE,
                    SourceSystem = Common.SOURCE_SYSTEM_INTEGRATION
                };
                repository.SupplierMaster.Create(supplier);
            }

            string normalizedName = InvoiceWorkflow.Normalize(name);
            supplier.SupplierCode = code;
            supplier.Name = name;
            supplier.NormalizedName = normalizedName;
            supplier.SourceConfigurationId = configuration.Id;
            supplier.LegalName = Get("Supplier.LegalName") ?? supplier.LegalName;
            supplier.TaxNumber = Get("Supplier.TaxNumber") ?? supplier.TaxNumber;
            supplier.Email = Get("Supplier.Email") ?? supplier.Email;
            supplier.Phone = Get("Supplier.Phone") ?? supplier.Phone;
            supplier.Country = Get("Supplier.Country") ?? supplier.Country;
            supplier.Currency = Get("Supplier.Currency")?.Trim().ToUpperInvariant() ?? supplier.Currency;
            supplier.ExternalId = externalId ?? supplier.ExternalId;
            supplier.SourceLastChangedAt = IntegrationRecordReader.ParseDateTime(Get("Supplier.SourceLastChangedAt")) ?? supplier.SourceLastChangedAt;
            supplier.LastSyncedAt = DateTime.UtcNow;

            bool aliasExists = await repository.SupplierAlias
                .FindByCondition(x => x.OrganizationId == configuration.OrganizationId && x.EntityCode == configuration.EntityCode && x.NormalizedAlias == normalizedName)
                .AnyAsync(cancellationToken);
            if (!aliasExists)
            {
                repository.SupplierAlias.Create(new SupplierAlias
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = configuration.OrganizationId,
                    SupplierId = supplier.Id,
                    EntityCode = configuration.EntityCode,
                    Alias = name,
                    NormalizedAlias = normalizedName,
                    SourceSystem = Common.SOURCE_SYSTEM_INTEGRATION,
                    IsConfirmed = true
                });
            }

            return created;
        }

        /// <returns>True when the purchase order was created, false when it was updated.</returns>
        public static async Task<bool> UpsertPurchaseOrderAsync(
            IRepositoryWrapper repository,
            ApiIntegrationConfiguration configuration,
            IReadOnlyList<ApiFieldMapping> mappings,
            JsonElement record,
            CancellationToken cancellationToken)
        {
            string? Get(string target) => IntegrationRecordReader.Read(mappings, target, record);
            string poNumber = Get("PurchaseOrder.PoNumber") ?? throw new InvalidOperationException("Purchase order number is missing.");
            string? externalId = Get("PurchaseOrder.ExternalId");
            string supplierCode = Get("PurchaseOrder.SupplierCode") ?? throw new InvalidOperationException("Supplier code is missing.");
            string supplierName = Get("PurchaseOrder.SupplierName") ?? throw new InvalidOperationException("Supplier name is missing.");
            string currency = Get("PurchaseOrder.Currency")?.Trim().ToUpperInvariant() ?? throw new InvalidOperationException("Currency is missing.");
            if (!Regex.IsMatch(currency, "^[A-Z]{3}$"))
            {
                throw new InvalidOperationException("Currency must be a three-letter ISO code.");
            }

            SupplierMaster? supplier = await repository.SupplierMaster
                .FindByCondition(x => x.OrganizationId == configuration.OrganizationId && x.EntityCode == configuration.EntityCode && x.SupplierCode == supplierCode)
                .FirstOrDefaultAsync(cancellationToken);
            if (supplier == null)
            {
                throw new IntegrationException("SUPPLIER_NOT_FOUND", $"Supplier {supplierCode} does not exist in entity {configuration.EntityCode}.");
            }

            if (supplier.IsBlocked || supplier.IsDeleted || supplier.Status != StatusKind.ACTIVE)
            {
                throw new IntegrationException("SUPPLIER_INACTIVE", $"Supplier {supplierCode} is not active.");
            }

            // Items are the array(s) nested in the record (for example an OData $expand of the PO items).
            // They are all validated first, so a bad item never leaves a half-imported purchase order behind.
            List<JsonElement> itemRecords = record.ValueKind == JsonValueKind.Object
                ? record.EnumerateObject().Select(property => property.Value).Where(value => value.ValueKind == JsonValueKind.Array).SelectMany(value => value.EnumerateArray()).ToList()
                : new List<JsonElement>();
            List<ApiFieldMapping> itemMappings = mappings.Where(item => item.TargetField.StartsWith("PurchaseOrderItem.", StringComparison.OrdinalIgnoreCase)).ToList();
            if (itemMappings.Count == 0)
            {
                // No item field is mapped: only the purchase order header is imported.
                itemRecords.Clear();
            }

            for (int index = 0; index < itemRecords.Count; index++)
            {
                JsonElement itemRecord = itemRecords[index];
                string position = IntegrationRecordReader.Read(itemMappings, "PurchaseOrderItem.LineNumber", itemRecord) ?? (index + 1).ToString();
                if (string.IsNullOrWhiteSpace(IntegrationRecordReader.Read(itemMappings, "PurchaseOrderItem.MaterialCode", itemRecord))
                    && string.IsNullOrWhiteSpace(IntegrationRecordReader.Read(itemMappings, "PurchaseOrderItem.Description", itemRecord)))
                {
                    throw new InvalidOperationException($"PO item {position} requires a material code or description.");
                }

                decimal? quantity = IntegrationRecordReader.ParseDecimal(IntegrationRecordReader.Read(itemMappings, "PurchaseOrderItem.OrderedQuantity", itemRecord));
                if (string.IsNullOrWhiteSpace(IntegrationRecordReader.Read(itemMappings, "PurchaseOrderItem.Uom", itemRecord)) || quantity == null || quantity <= 0)
                {
                    throw new InvalidOperationException($"PO item {position} requires a positive quantity and UOM.");
                }
            }

            PurchaseOrder? order = await repository.PurchaseOrder.FindFirstByConditionAsync(x =>
                x.OrganizationId == configuration.OrganizationId
                && ((externalId != null && x.SourceConfigurationId == configuration.Id && x.ExternalId == externalId)
                    || (x.EntityCode == configuration.EntityCode && x.PoNumber == poNumber)));
            bool created = order == null;
            if (order == null)
            {
                order = new PurchaseOrder
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = configuration.OrganizationId,
                    PoNumber = poNumber,
                    SourceSystem = Common.SOURCE_SYSTEM_INTEGRATION,
                    Status = PurchaseOrderStatus.OPEN
                };
                repository.PurchaseOrder.Create(order);
            }

            order.OperatingUnitId = configuration.OrganizationUnitId ?? order.OperatingUnitId;
            order.EntityCode = configuration.EntityCode;
            order.SourceConfigurationId = configuration.Id;
            order.ExternalId = externalId ?? order.ExternalId;
            order.SupplierId = supplier.Id;
            order.Currency = currency;
            order.SupplierName = supplierName;
            order.CompanyCode = Get("PurchaseOrder.CompanyCode") ?? order.CompanyCode;
            order.PurchaseOrderType = Get("PurchaseOrder.PurchaseOrderType") ?? order.PurchaseOrderType;
            order.TotalNetAmount = IntegrationRecordReader.ParseDecimal(Get("PurchaseOrder.TotalNetAmount")) ?? order.TotalNetAmount;
            order.TotalTaxAmount = IntegrationRecordReader.ParseDecimal(Get("PurchaseOrder.TotalTaxAmount")) ?? order.TotalTaxAmount;
            order.TotalAmount = IntegrationRecordReader.ParseDecimal(Get("PurchaseOrder.TotalAmount")) ?? order.TotalAmount;
            order.PoDate = IntegrationRecordReader.ParseDate(Get("PurchaseOrder.PoDate")) ?? order.PoDate;
            order.DeliveryDate = IntegrationRecordReader.ParseDate(Get("PurchaseOrder.DeliveryDate")) ?? order.DeliveryDate;
            order.SourceLastChangedAt = IntegrationRecordReader.ParseDateTime(Get("PurchaseOrder.SourceLastChangedAt")) ?? order.SourceLastChangedAt;
            order.LastSyncedAt = DateTime.UtcNow;
            order.SourceHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(record.GetRawText())));

            if (itemRecords.Count == 0)
            {
                return created;
            }

            List<PurchaseOrderItem> items = created
                ? new List<PurchaseOrderItem>()
                : await repository.PurchaseOrderItem.GetTrackedByOrderAsync(order.Id, cancellationToken);
            foreach (JsonElement itemRecord in itemRecords)
            {
                string? Item(string target) => IntegrationRecordReader.Read(itemMappings, target, itemRecord);
                int lineNumber = IntegrationRecordReader.ParseInt(Item("PurchaseOrderItem.LineNumber")) ?? items.Count + 1;
                string? materialCode = Item("PurchaseOrderItem.MaterialCode");
                string? description = Item("PurchaseOrderItem.Description");
                string? uom = Item("PurchaseOrderItem.Uom");
                decimal? orderedQuantity = IntegrationRecordReader.ParseDecimal(Item("PurchaseOrderItem.OrderedQuantity"));
                PurchaseOrderItem? line = items.FirstOrDefault(candidate => candidate.LineNumber == lineNumber);
                if (line == null)
                {
                    line = new PurchaseOrderItem
                    {
                        Id = Guid.NewGuid(),
                        PurchaseOrderId = order.Id,
                        LineNumber = lineNumber,
                        Status = PurchaseOrderItemStatus.OPEN
                    };
                    repository.PurchaseOrderItem.Create(line);
                    items.Add(line);
                }

                line.ExternalId = Item("PurchaseOrderItem.ExternalId");
                line.MaterialCode = materialCode ?? line.MaterialCode;
                line.Description = description ?? line.Description;
                line.OrderedQuantity = orderedQuantity ?? line.OrderedQuantity;
                line.OpenQuantity = Math.Max(0, line.OrderedQuantity - line.ReceivedQuantity);
                line.Uom = uom ?? line.Uom;
                line.UnitPrice = IntegrationRecordReader.ParseDecimal(Item("PurchaseOrderItem.UnitPrice")) ?? line.UnitPrice;
                line.SourceLastChangedAt = IntegrationRecordReader.ParseDateTime(Item("PurchaseOrderItem.SourceLastChangedAt"));
                line.LastSyncedAt = DateTime.UtcNow;
                line.SourceHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(itemRecord.GetRawText())));
            }

            order.TotalOrderedQuantity = items.Sum(item => item.OrderedQuantity);
            order.TotalReceivedQuantity = items.Sum(item => item.ReceivedQuantity);
            return created;
        }
    }
}
