using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Operations.Domain.Entities;
using Operations.Domain.Enums;
using Operations.Infrastructure.Contracts.IRepository;

namespace Operations.Application.Features.Shared
{
    /// <summary>
    /// Matching rules of an invoice (supplier, purchase order, lines) and its denormalized
    /// extraction rows. Centralized here because upload, processing, re-read and the manual match
    /// commands must all apply exactly the same rules.
    /// </summary>
    internal static class InvoiceWorkflow
    {
        public static string Normalize(string? value)
        {
            return Regex.Replace((value ?? string.Empty).Trim().ToUpperInvariant(), @"[^A-Z0-9]+", " ").Trim();
        }

        public static async Task MatchSupplierAsync(IRepositoryWrapper repository, Invoice invoice, CancellationToken cancellationToken)
        {
            SupplierMaster? supplier = await repository.SupplierMaster.FindMatchAsync(
                invoice.OrganizationId,
                invoice.SupplierTaxNumberRaw,
                string.IsNullOrWhiteSpace(invoice.SupplierNameRaw) ? null : Normalize(invoice.SupplierNameRaw),
                cancellationToken);
            invoice.SupplierId = supplier?.Id;
        }

        public static async Task MatchPurchaseOrderAsync(IRepositoryWrapper repository, Invoice invoice, CancellationToken cancellationToken)
        {
            // An invoice the user saved as "no purchase order" is never matched automatically.
            if (invoice.NoPurchaseOrder)
            {
                invoice.PurchaseOrderId = null;
                return;
            }

            if (invoice.SupplierId == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(invoice.PoNumberRaw) && invoice.PurchaseOrderId != null)
            {
                return;
            }

            invoice.PurchaseOrderId = await repository.PurchaseOrder.FindOpenIdAsync(
                invoice.OrganizationId,
                invoice.SupplierId.Value,
                invoice.PoNumberRaw,
                invoice.OperatingUnitId,
                cancellationToken);
        }

        public static async Task MatchLinesAsync(IRepositoryWrapper repository, Invoice invoice, List<InvoiceLine> lines, CancellationToken cancellationToken)
        {
            if (invoice.PurchaseOrderId == null)
            {
                return;
            }

            List<PurchaseOrderItem> items = await repository.PurchaseOrderItem
                .FindByCondition(x => x.PurchaseOrderId == invoice.PurchaseOrderId)
                .ToListAsync(cancellationToken);
            foreach (InvoiceLine line in lines)
            {
                string normalized = Normalize(line.DescriptionRaw);
                PurchaseOrderItem? match = items.FirstOrDefault(item => item.MaterialCode == line.MaterialCodeRaw)
                    ?? items.FirstOrDefault(item => Normalize(item.Description) == normalized);
                line.PurchaseOrderItemId = match?.Id;
                line.MatchStatus = match == null ? InvoiceLineMatchStatus.UNMATCHED : InvoiceLineMatchStatus.MATCHED;
            }
        }

        public static InvoiceStatus ResolveStatus(Invoice invoice, List<InvoiceLine> lines)
        {
            return invoice.PurchaseOrderId != null && lines.All(line => line.MatchStatus == InvoiceLineMatchStatus.MATCHED)
                ? InvoiceStatus.READY_FOR_GRN
                : InvoiceStatus.REVIEW_REQUIRED;
        }

        /// <summary>Fields the organization's OCR policy requires before a reviewed invoice can be saved.</summary>
        public static async Task<List<string>> MissingReviewFieldsAsync(
            IRepositoryWrapper repository,
            Guid organizationId,
            string? supplierName,
            string? invoiceNumber,
            string? purchaseOrderNumber,
            decimal? grossAmount,
            DateOnly? invoiceDate,
            string? currency,
            string? supplierTaxNumber,
            bool noPurchaseOrder,
            CancellationToken cancellationToken)
        {
            InvoiceOcrConfiguration? policy = await repository.InvoiceOcrConfiguration
                .FindByCondition(x => x.OrganizationId == organizationId)
                .FirstOrDefaultAsync(cancellationToken);
            List<string> missing = new List<string>();
            if ((policy?.RequireSupplierName ?? true) && string.IsNullOrWhiteSpace(supplierName))
            {
                missing.Add("supplierName");
            }

            if ((policy?.RequireInvoiceNumber ?? true) && string.IsNullOrWhiteSpace(invoiceNumber))
            {
                missing.Add("invoiceNumber");
            }

            if (!noPurchaseOrder && (policy?.RequirePurchaseOrderNumber ?? true) && string.IsNullOrWhiteSpace(purchaseOrderNumber))
            {
                missing.Add("purchaseOrderNumber");
            }

            if ((policy?.RequireInvoiceAmount ?? true) && grossAmount == null)
            {
                missing.Add("grossAmount");
            }

            if ((policy?.RequireInvoiceDate ?? false) && invoiceDate == null)
            {
                missing.Add("invoiceDate");
            }

            if ((policy?.RequireCurrency ?? false) && string.IsNullOrWhiteSpace(currency))
            {
                missing.Add("currency");
            }

            if ((policy?.RequireSupplierTrn ?? false) && string.IsNullOrWhiteSpace(supplierTaxNumber))
            {
                missing.Add("supplierTaxNumber");
            }

            return missing;
        }

        /// <summary>
        /// Finds another invoice of the organization with the same number from the same supplier
        /// (tax number first, supplier name when the other invoice has no tax number).
        /// </summary>
        public static async Task<Guid?> FindProbableDuplicateAsync(
            IRepositoryWrapper repository,
            Guid organizationId,
            Guid? excludedInvoiceId,
            string invoiceNumber,
            string? supplierTaxNumber,
            string? supplierName,
            CancellationToken cancellationToken)
        {
            string normalizedNumber = Normalize(invoiceNumber);
            string normalizedSupplier = Normalize(supplierTaxNumber ?? supplierName);

            // Numbers are compared normalized ("INV-1001" = "inv/1001"), which SQL cannot do:
            // only the four columns needed for the comparison are read.
            var candidates = await repository.Invoice
                .FindByCondition(x => x.OrganizationId == organizationId && (excludedInvoiceId == null || x.Id != excludedInvoiceId))
                .Select(x => new { x.Id, x.InvoiceNumber, x.SupplierTaxNumberRaw, x.SupplierNameRaw })
                .ToListAsync(cancellationToken);
            return candidates
                .Where(item => Normalize(item.InvoiceNumber) == normalizedNumber
                    && ((item.SupplierTaxNumberRaw != null && Normalize(item.SupplierTaxNumberRaw) == normalizedSupplier)
                        || (item.SupplierTaxNumberRaw == null && item.SupplierNameRaw != null && Normalize(item.SupplierNameRaw) == normalizedSupplier)))
                .Select(item => (Guid?)item.Id)
                .FirstOrDefault();
        }

        public static HashSet<string> ParseManualFields(string? json)
        {
            HashSet<string> fields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(json))
            {
                return fields;
            }

            try
            {
                foreach (string field in JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>())
                {
                    fields.Add(field switch
                    {
                        "supplierName" => "SupplierName",
                        "supplierTaxNumber" => "SupplierTaxNumber",
                        "supplierTrn" => "SupplierTaxNumber",
                        "supplierInvoiceNumber" => "InvoiceNumber",
                        "invoiceDate" => "InvoiceDate",
                        "purchaseOrderNumber" => "PoNumber",
                        "invoiceGross" => "GrossAmount",
                        "currency" => "Currency",
                        _ => field,
                    });
                }
            }
            catch (JsonException)
            {
                // The client sent something that is not a JSON array: no field counts as manually edited.
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            return fields;
        }

        /// <summary>
        /// Replaces the current, denormalized extraction rows of the invoice document: one row per
        /// invoice line, or a single header row. History stays in DocumentExtraction.
        /// </summary>
        public static async Task ReplaceExtDataAsync(
            IRepositoryWrapper repository,
            Invoice invoice,
            Document document,
            List<InvoiceLine> lines,
            decimal? confidence,
            ExtractionMethod method,
            string provider,
            CancellationToken cancellationToken)
        {
            List<InvoiceExtData> existing = await repository.InvoiceExtData.GetTrackedByDocumentAsync(invoice.DocumentId, cancellationToken);
            InvoiceExtData? previous = existing.FirstOrDefault();
            if (document.SourceChannel == DocumentSourceChannel.MOBILE_SCANNER)
            {
                invoice.SupplierNameRaw ??= previous?.SupplierName;
                invoice.SupplierTaxNumberRaw ??= previous?.SupplierTrn;
                invoice.PoNumberRaw ??= previous?.PurchaseOrderNumber;
                invoice.GrossAmount ??= previous?.InvoiceGross;
                invoice.Currency ??= previous?.Currency;
            }

            repository.InvoiceExtData.DeleteRange(existing);
            List<InvoiceLine?> rows = lines.Count == 0
                ? new List<InvoiceLine?> { null }
                : lines.OrderBy(item => item.LineNumber).Cast<InvoiceLine?>().ToList();
            foreach (InvoiceLine? line in rows)
            {
                repository.InvoiceExtData.Create(new InvoiceExtData
                {
                    Id = Guid.NewGuid(),
                    DocumentId = invoice.DocumentId,
                    OrganizationId = invoice.OrganizationId,
                    OperatingUnitId = invoice.OperatingUnitId,
                    SupplierName = invoice.SupplierNameRaw,
                    SupplierTrn = invoice.SupplierTaxNumberRaw,
                    SupplierInvoiceNumber = IsPendingNumber(invoice.InvoiceNumber) ? null : invoice.InvoiceNumber,
                    InvoiceDate = invoice.InvoiceDate,
                    PurchaseOrderNumber = invoice.PoNumberRaw,
                    InvoiceGross = invoice.GrossAmount,
                    InvoiceNet = invoice.NetAmount,
                    Currency = invoice.Currency,
                    ItemSkuId = line?.MaterialCodeRaw,
                    ItemAmount = line?.LineAmount,
                    ItemNet = line?.LineAmount,
                    ItemDescription = string.IsNullOrWhiteSpace(line?.DescriptionRaw) ? null : line!.DescriptionRaw,
                    LineItemNumber = line?.LineNumber.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    PurchaseOrderItemId = line?.PurchaseOrderItemId,
                    SourceProvider = provider,
                    ExtractionMethod = method.ToString(),
                    ExtractionConfidence = confidence
                });
            }
        }

        public static bool IsPendingNumber(string invoiceNumber)
        {
            return invoiceNumber.StartsWith(Operations.Domain.Common.Common.PENDING_INVOICE_PREFIX, StringComparison.Ordinal);
        }
    }
}
