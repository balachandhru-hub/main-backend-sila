using System.Text;
using SilaMe.Api.Models;
using SilaMe.Api.Services;
using Xunit;

namespace SilaMe.Api.Tests;

public sealed class InvoiceExtractionTests
{
    [Fact]
    public void Normalizer_reads_uae_invoice_headers_and_line_table()
    {
        var service = new InvoiceExtractionService();
        var result = service.Extract("""
            Supplier: Gulf Kitchen Foods
            TRN: 100123456700003
            Tax Invoice Number: TAX-7788
            Invoice Date: 2026-09-14
            Purchase Order Number: 4500001001
            Currency: AED
            Net Total: 950.00
            VAT Amount: 47.50
            Grand Total: 997.50
            LINE 10 | CHK001 | Chicken Breast | 48 KG | 500.00
            LINE 20 | RICE001 | Basmati Rice | 25 KG | 300.00
            """);

        Assert.Equal("Gulf Kitchen Foods", result.SupplierName);
        Assert.Equal("100123456700003", result.SupplierTaxNumber);
        Assert.Equal("TAX-7788", result.InvoiceNumber);
        Assert.Equal(new DateOnly(2026, 9, 14), result.InvoiceDate);
        Assert.Equal("4500001001", result.PoNumber);
        Assert.Equal("AED", result.Currency);
        Assert.Equal(997.50m, result.GrossAmount);
        Assert.Equal(2, result.Lines.Count);
        Assert.Equal("CHK001", result.Lines[0].SupplierMaterialCode);
        Assert.Equal(48m, result.Lines[0].Quantity);
    }

    [Fact]
    public void Reads_supplier_date_and_po_from_noisy_tesseract_text()
    {
        var service = new InvoiceExtractionService();
        var result = service.Extract("""
            TAX INVOICE
            Gulf Kitchen Foods LLC
            Invoice Number. INV-7788
            Invoice Date. 16/09/2026,
            Supplier Gulf Kitchen Foods LLC
            P0 Number PO-45001
            Currency. AED
            Grand Total 997.50
            """);

        Assert.Equal("Gulf Kitchen Foods LLC", result.SupplierName);
        Assert.Equal("INV-7788", result.InvoiceNumber);
        Assert.Equal(new DateOnly(2026, 9, 16), result.InvoiceDate);
        Assert.Equal("PO-45001", result.PoNumber);
        Assert.Equal(997.50m, result.GrossAmount);
    }

    [Fact]
    public void Reads_supplier_trn_invoice_number_and_gross_from_noisy_tesseract_text()
    {
        var service = new InvoiceExtractionService();
        var result = service.Extract("""
            TAX INVOICE
            SUPPLIER NAME
            Gulf Kitchen Foods LLC
            SUPPLIER TRN
            100123456700003
            INVOICE NUMBER
            INV.7788
            Invoice Date. 16/09/2026,
            Gross Amount
            AED 1,247.50
            VAT Amount 47.50
            Subtotal 1,200.00
            """);

        Assert.Equal("Gulf Kitchen Foods LLC", result.SupplierName);
        Assert.Equal("100123456700003", result.SupplierTaxNumber);
        Assert.Equal("INV-7788", result.InvoiceNumber);
        Assert.Equal(1247.50m, result.GrossAmount);
    }

    [Fact]
    public void Reads_vendor_on_next_line_and_named_invoice_date()
    {
        var service = new InvoiceExtractionService();
        var result = service.Extract("""
            Vendor Name
            Desert Harvest General Trading
            Dated: 14 Sep 2026
            Your PO: 4500001001
            Invoice Number: SCANNED-2001
            """);

        Assert.Equal("Desert Harvest General Trading", result.SupplierName);
        Assert.Equal(new DateOnly(2026, 9, 14), result.InvoiceDate);
        Assert.Equal("4500001001", result.PoNumber);
    }

    [Fact]
    public void Supplier_reader_never_maps_company_code_or_currency_to_supplier()
    {
        var service = new InvoiceExtractionService();
        var result = service.Extract("""
            TAX INVOICE
            Supplier
            Company Code: 1050 Currency: Not provided
            Test SBN
            Supplier ID: 1003430
            PO Number: 4500003415
            Invoice Date: 17-09-2026
            """);

        Assert.Equal("Test SBN", result.SupplierName);
        Assert.DoesNotContain("Company Code", result.SupplierName);
        Assert.DoesNotContain("Currency", result.SupplierName);
        Assert.Equal("4500003415", result.PoNumber);
        Assert.Equal("1003430", InvoiceOcrFieldReader.ReadSupplierId("""
            Supplier ID: 1003430
            """));
        Assert.Equal("1003430", InvoiceOcrFieldReader.ReadSupplierId("""
            ID: 1003430
            """));
        Assert.Equal("1003430", InvoiceOcrFieldReader.NormalizeSupplierCandidate("ID: 1003430"));
        Assert.Equal("1003430", InvoiceOcrFieldReader.NormalizeSupplierCandidate("Supplier ID: 1003430"));
        Assert.Equal("1003430", InvoiceOcrFieldReader.NormalizeSupplierCandidate("Vendor ID: 1003430"));
        Assert.DoesNotContain("ID:", InvoiceOcrFieldReader.NormalizeSupplierCandidate("ID: 1003430")!);
        Assert.True(InvoiceOcrFieldReader.IsSupplierIdLabel("ID: 1003430"));
        Assert.Equal("ABRACADABRA LIFE LIMITED", InvoiceOcrFieldReader.ReadSupplierName("""
            TAX INVOICE
            TESTINVOICE123
            Supplier
            Invoice
            Date 21/09/2026
            ABRACADABRA LIFE LIMITED
            Supplier ID: 1003754
            """));
        Assert.True(InvoiceOcrFieldReader.IsSupplierNameNoise("Invoice"));
        Assert.True(InvoiceOcrFieldReader.IsSupplierNameNoise("TAX INVOICE"));
    }

    [Fact]
    public void Supplier_reader_maps_bare_id_label_to_supplier_code()
    {
        var service = new InvoiceExtractionService();
        var result = service.Extract("""
            TAX INVOICE
            Supplier
            ID: 1003430
            Invoice Number: INV-100
            Grand Total: 10.00
            """);

        Assert.Equal("1003430", result.SupplierName);
        Assert.Equal("1003430", InvoiceOcrFieldReader.ReadSupplierId("""
            ID: 1003430
            """));
    }

    [Fact]
    public void Gross_reads_total_aed_line()
    {
        Assert.Equal(2000.00m, InvoiceOcrFieldReader.ReadGrossAmount("Total AED 2,000.00"));
        Assert.Equal(2000.00m, InvoiceOcrFieldReader.ReadGrossAmount("Subtotal AED 1,000.00\nTax AED 0.00\nTotal AED 2,000.00"));
    }

    [Fact]
    public void Reads_banner_invoice_number_supplier_total_and_material_table()
    {
        var text = """
            TAX INVOICE
            TESTINVOICE123
            Supplier
            ABRACADABRA LIFE LIMITED
            Supplier ID: 1003754
            Invoice Date 21/09/2026
            PO Number 4500002849
            PO Date 15/09/2026
            Currency AED
            Bill To
            Company Code 1050
            Dubai Main Road, Dubai, UAE
            Description PO Item Quantity Unit Price (AED) Amount (AED)
            test003 (Material: M) 10 1.00 2,000.00 2,000.00
            Subtotal AED 2,000.00
            Tax AED 0.00
            Total AED 2,000.00
            Payment Terms: Net 30 Days
            Due Date: 21/10/2026
            Reference: Purchase Order 4500002849 | PO Item 10
            """;
        var service = new InvoiceExtractionService();
        var result = service.Extract(text);

        Assert.Equal("TESTINVOICE123", result.InvoiceNumber);
        Assert.Equal("ABRACADABRA LIFE LIMITED", result.SupplierName);
        Assert.Equal("1003754", InvoiceOcrFieldReader.ReadSupplierId("""
            Supplier
            ABRACADABRA LIFE LIMITED
            Supplier ID: 1003754
            """));
        Assert.Equal(new DateOnly(2026, 9, 21), result.InvoiceDate);
        Assert.Equal("4500002849", result.PoNumber);
        Assert.Equal("AED", result.Currency);
        Assert.Equal(2000.00m, result.GrossAmount);
        Assert.Equal(2000.00m, result.NetAmount);
        Assert.Single(result.Lines);
        Assert.Equal(10, result.Lines[0].LineNumber);
        Assert.Equal("M", result.Lines[0].SupplierMaterialCode);
        Assert.Contains("test003", result.Lines[0].Description, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1.00m, result.Lines[0].Quantity);
        Assert.Equal(2000.00m, result.Lines[0].UnitPrice);
        Assert.Equal(2000.00m, result.Lines[0].LineAmount);
    }

    [Fact]
    public async Task Built_in_reader_processes_local_text_without_external_credentials()
    {
        var provider = new BuiltInOcrProvider();
        var document = new Document
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            UploadedByUserId = Guid.NewGuid(),
            DocumentType = DocumentType.INVOICE,
            OriginalFilename = "invoice-camera.txt",
            ContentType = "text/plain",
            StorageProvider = "TEST",
            StorageReference = "test",
            SourceChannel = DocumentSourceChannel.MOBILE_UPLOAD,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        await using var content = new MemoryStream(Encoding.UTF8.GetBytes("Invoice Supplier TRN 100123456700003"));

        var result = await provider.ExtractAsync(document, content, CancellationToken.None);

        Assert.NotNull(result.Text);
        Assert.Equal(0.82m, result.Confidence);
    }
}