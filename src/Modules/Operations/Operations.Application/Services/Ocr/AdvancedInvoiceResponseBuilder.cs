using System.Globalization;
using System.Text.RegularExpressions;
using Operations.Domain.Common;
using Operations.Domain.Dtos;
using Operations.Domain.Entities;
using Operations.Domain.Enums;

namespace Operations.Application.Services.Ocr
{
    /// <summary>
    /// Builds the field-by-field "advanced" extraction result from the text of an invoice.
    /// Pure text processing: the supplier / purchase order master checks are done by the handler.
    /// </summary>
    public static class AdvancedInvoiceResponseBuilder
    {
        public static AdvancedInvoiceExtractionResponseDto Build(
            Document document,
            string? rawText,
            ExtractionTrigger trigger,
            InvoiceOcrConfiguration configuration,
            DateTime startedAt,
            bool ocrUnavailable)
        {
            string text = Regex.Replace(rawText ?? string.Empty, @"[ \t]+\r?\n", "\n").Trim();
            ExtractedInvoice? extracted = string.IsNullOrWhiteSpace(text) ? null : InvoiceTextParser.Parse(text);
            if (extracted == null)
            {
                return new AdvancedInvoiceExtractionResponseDto
                {
                    DocumentId = document.Id,
                    Provider = Common.OCR_PROVIDER_ADVANCED,
                    Status = "FAILED",
                    Trigger = trigger.ToString(),
                    Header = EmptyHeader(),
                    RequiresReview = true,
                    StartedAt = startedAt,
                    CompletedAt = DateTime.UtcNow,
                    PageCount = document.PageCount ?? 1,
                    ErrorMessage = ocrUnavailable ? Common.OCR_UNAVAILABLE_MESSAGE : "The invoice document did not contain readable text.",
                    OcrRequestId = document.OcrRequestId,
                    EffectiveConfiguration = Snapshot(configuration)
                };
            }

            string? invoiceTypeValue = LabelValue(text, "Invoice Type", "Document Type");
            if (string.IsNullOrWhiteSpace(invoiceTypeValue))
            {
                invoiceTypeValue = extracted.Lines.Count == 0 ? InvoiceType.SERVICE.ToString() : InvoiceType.MATERIAL.ToString();
            }

            AdvancedInvoiceHeaderResponseDto header = new AdvancedInvoiceHeaderResponseDto
            {
                SupplierName = Field(extracted.SupplierName, "Supplier", text),
                SupplierLegalName = Field(LabelValue(text, "Supplier Legal Name", "Legal Name"), "Supplier Legal Name", text),
                SupplierTrn = Field(extracted.SupplierTaxNumber, "TRN", text),
                SupplierAddress = Field(LabelValue(text, "Supplier Address", "Vendor Address", "Address"), "Supplier Address", text),
                SupplierEmail = Field(LabelValue(text, "Supplier Email", "Email"), "Supplier Email", text),
                SupplierPhone = Field(LabelValue(text, "Supplier Phone", "Phone", "Telephone"), "Supplier Phone", text),
                InvoiceNumber = Field(extracted.InvoiceNumber, "Invoice Number", text),
                InvoiceDate = Field(extracted.InvoiceDate, "Invoice Date", text),
                InvoiceType = Field(invoiceTypeValue, "Invoice Type", text),
                PurchaseOrderNumber = Field(extracted.PoNumber, "PO Number", text),
                Currency = Field(extracted.Currency, "Currency", text),
                NetAmount = Field(extracted.NetAmount, "Net Amount", text),
                DiscountAmount = Field(ParseDecimal(LabelValue(text, "Discount Amount", "Discount")), "Discount Amount", text),
                FreightAmount = Field(ParseDecimal(LabelValue(text, "Freight", "Delivery Charge", "Freight Charge")), "Freight", text),
                OtherCharges = Field(ParseDecimal(LabelValue(text, "Other Charges", "Additional Charges")), "Other Charges", text),
                TaxableAmount = Field(ParseDecimal(LabelValue(text, "Taxable Amount", "Taxable")), "Taxable Amount", text),
                TaxAmount = Field(extracted.TaxAmount, "Tax Amount", text),
                GrossAmount = Field(extracted.GrossAmount, "Grand Total", text),
                AmountDue = Field(ParseDecimal(LabelValue(text, "Amount Due", "Net Payable", "Total Payable")), "Amount Due", text),
                PaymentTerms = Field(LabelValue(text, "Payment Terms", "Terms"), "Payment Terms", text),
                DueDate = Field(ParseDate(LabelValue(text, "Due Date", "Payment Due Date")), "Due Date", text)
            };

            List<AdvancedInvoiceLineResponseDto> lines = configuration.DetailedLineExtractionEnabled
                ? extracted.Lines.Select(line => new AdvancedInvoiceLineResponseDto
                {
                    LineNumber = line.LineNumber,
                    SupplierItemCode = Field(line.SupplierMaterialCode, "Item Code", text),
                    Description = Field<string>(line.Description, "Description", text),
                    PoItemNumber = Field<string>(null, "PO Item", text),
                    Quantity = Field(line.Quantity, "Quantity", text),
                    Uom = Field(line.Uom, "UOM", text),
                    UnitPrice = Field(line.UnitPrice, "Unit Price", text),
                    DiscountAmount = Field<decimal?>(null, "Discount", text),
                    NetAmount = Field(line.LineAmount, "Line Amount", text),
                    TaxRate = Field(line.TaxRate, "Tax Rate", text),
                    TaxAmount = Field(line.TaxAmount, "Tax Amount", text),
                    GrossAmount = Field(line.LineAmount, "Line Total", text),
                    BatchNumber = Field<string>(null, "Batch Number", text),
                    ExpiryDate = Field<DateOnly?>(null, "Expiry Date", text)
                }).ToList()
                : new List<AdvancedInvoiceLineResponseDto>();

            AdvancedInvoiceValidationResponseDto validation = Reconcile(header.NetAmount.Value, header.TaxAmount.Value, header.GrossAmount.Value, lines, configuration.AmountTolerance);

            bool requiredMissing =
                (configuration.RequireSupplierName && string.IsNullOrWhiteSpace(header.SupplierName.Value))
                || (configuration.RequireInvoiceNumber && string.IsNullOrWhiteSpace(header.InvoiceNumber.Value))
                || (configuration.RequirePurchaseOrderNumber && string.IsNullOrWhiteSpace(header.PurchaseOrderNumber.Value))
                || (configuration.RequireInvoiceAmount && header.GrossAmount.Value == null)
                || (configuration.RequireInvoiceDate && header.InvoiceDate.Value == null)
                || (configuration.RequireCurrency && string.IsNullOrWhiteSpace(header.Currency.Value))
                || (configuration.RequireSupplierTrn && string.IsNullOrWhiteSpace(header.SupplierTrn.Value));
            bool requiresReview = requiredMissing || (configuration.FinancialReconciliationEnabled && !validation.AmountsReconciled);

            return new AdvancedInvoiceExtractionResponseDto
            {
                DocumentId = document.Id,
                Provider = Common.OCR_PROVIDER_ADVANCED,
                Status = requiresReview ? "REVIEW_REQUIRED" : "SUCCESS",
                Trigger = trigger.ToString(),
                Confidence = HeaderConfidence(header, lines),
                Header = header,
                Lines = lines,
                Validation = validation,
                RequiresReview = requiresReview,
                FallbackUsed = false,
                StartedAt = startedAt,
                CompletedAt = DateTime.UtcNow,
                PageCount = document.PageCount ?? Math.Max(1, text.Split("\f", StringSplitOptions.RemoveEmptyEntries).Length),
                OcrRequestId = document.OcrRequestId,
                RawText = text,
                EffectiveConfiguration = Snapshot(configuration)
            };
        }

        public static AdvancedOcrConfigurationSnapshotDto Snapshot(InvoiceOcrConfiguration configuration)
        {
            return new AdvancedOcrConfigurationSnapshotDto
            {
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
                AlwaysBackendOnReread = configuration.AlwaysBackendOnReread,
                DetailedLineExtractionEnabled = configuration.DetailedLineExtractionEnabled,
                SupplierMasterValidationEnabled = configuration.SupplierMasterValidationEnabled,
                PurchaseOrderValidationEnabled = configuration.PurchaseOrderValidationEnabled,
                FinancialReconciliationEnabled = configuration.FinancialReconciliationEnabled,
                Version = configuration.Version
            };
        }

        private static AdvancedInvoiceValidationResponseDto Reconcile(
            decimal? net,
            decimal? tax,
            decimal? gross,
            List<AdvancedInvoiceLineResponseDto> lines,
            decimal tolerance)
        {
            bool allAmounts = net.HasValue && tax.HasValue && gross.HasValue;
            decimal lineTotal = lines.Where(line => line.GrossAmount.Value.HasValue).Sum(line => line.GrossAmount.Value!.Value);
            return new AdvancedInvoiceValidationResponseDto
            {
                AmountsReconciled = allAmounts && Math.Abs(net!.Value + tax!.Value - gross!.Value) <= tolerance,
                LineTotalMatchesNet = net.HasValue && lines.Count > 0 && Math.Abs(lineTotal - net.Value) <= tolerance,
                TaxReconciled = lines.Count > 0 && tax.HasValue && lines.All(line => line.TaxAmount.Value.HasValue)
                    && Math.Abs(lines.Sum(line => line.TaxAmount.Value!.Value) - tax.Value) <= tolerance,
                Difference = allAmounts ? gross!.Value - (net!.Value + tax!.Value) : null
            };
        }

        private static AdvancedInvoiceHeaderResponseDto EmptyHeader()
        {
            return new AdvancedInvoiceHeaderResponseDto
            {
                SupplierName = NotFound<string>(),
                SupplierLegalName = NotFound<string>(),
                SupplierTrn = NotFound<string>(),
                SupplierAddress = NotFound<string>(),
                SupplierEmail = NotFound<string>(),
                SupplierPhone = NotFound<string>(),
                InvoiceNumber = NotFound<string>(),
                InvoiceDate = NotFound<DateOnly?>(),
                InvoiceType = NotFound<string>(),
                PurchaseOrderNumber = NotFound<string>(),
                Currency = NotFound<string>(),
                NetAmount = NotFound<decimal?>(),
                DiscountAmount = NotFound<decimal?>(),
                FreightAmount = NotFound<decimal?>(),
                OtherCharges = NotFound<decimal?>(),
                TaxableAmount = NotFound<decimal?>(),
                TaxAmount = NotFound<decimal?>(),
                GrossAmount = NotFound<decimal?>(),
                AmountDue = NotFound<decimal?>(),
                PaymentTerms = NotFound<string>(),
                DueDate = NotFound<DateOnly?>()
            };
        }

        private static AdvancedExtractedFieldDto<T> NotFound<T>()
        {
            return new AdvancedExtractedFieldDto<T> { ConfidenceBand = "NOT_FOUND" };
        }

        private static AdvancedExtractedFieldDto<T> Field<T>(T? value, string label, string text)
        {
            bool found = value != null && (value is not string stringValue || !string.IsNullOrWhiteSpace(stringValue));
            return new AdvancedExtractedFieldDto<T>
            {
                Value = value,
                Confidence = found ? 0.86m : null,
                ConfidenceBand = found ? "MEDIUM" : "NOT_FOUND",
                PageNumber = found ? PageFor(text, label) : null,
                SourceLabel = found ? label : null,
                ExtractionMethod = found ? "LABEL_AND_LAYOUT" : null
            };
        }

        private static int PageFor(string text, string label)
        {
            int index = text.IndexOf(label, StringComparison.OrdinalIgnoreCase);
            return index < 0 ? 1 : text[..index].Count(character => character == '\f') + 1;
        }

        private static string? LabelValue(string text, params string[] labels)
        {
            string pattern = string.Join("|", labels.OrderByDescending(item => item.Length).Select(Regex.Escape));
            Match match = Regex.Match(text, $@"(?im)^\s*(?:{pattern})\s*(?:[:#=\-]\s*|\s+)(?<value>[^\r\n|]+)");
            return match.Success ? match.Groups["value"].Value.Trim() : null;
        }

        private static decimal? ParseDecimal(string? value)
        {
            return decimal.TryParse(value?.Replace(",", string.Empty), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsed) ? parsed : null;
        }

        private static DateOnly? ParseDate(string? value)
        {
            return DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly parsed) ? parsed : null;
        }

        private static decimal? HeaderConfidence(AdvancedInvoiceHeaderResponseDto header, List<AdvancedInvoiceLineResponseDto> lines)
        {
            List<decimal> values = new decimal?[]
            {
                header.SupplierName.Confidence, header.InvoiceNumber.Confidence, header.InvoiceDate.Confidence,
                header.PurchaseOrderNumber.Confidence, header.Currency.Confidence, header.NetAmount.Confidence,
                header.TaxAmount.Confidence, header.GrossAmount.Confidence,
            }.Where(value => value.HasValue).Select(value => value!.Value).ToList();
            values.AddRange(lines.SelectMany(line => new[] { line.Description.Confidence, line.Quantity.Confidence, line.GrossAmount.Confidence }
                .Where(value => value.HasValue).Select(value => value!.Value)));
            return values.Count == 0 ? null : Math.Round(values.Average(), 4);
        }
    }
}
