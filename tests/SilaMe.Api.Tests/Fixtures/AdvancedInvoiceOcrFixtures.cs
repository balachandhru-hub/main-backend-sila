using System.Text;

namespace SilaMe.Api.Tests.Fixtures;

public static class AdvancedInvoiceOcrFixtures
{
    public static readonly byte[] DigitalInvoice = Pdf("""
        Invoice Number: DIGITAL-1001
        Invoice Date: 2026-09-14
        Supplier: Gulf Kitchen Foods LLC
        TRN: 100123456700003
        PO Number: PO-DIGITAL-1001
        Currency: AED
        Net Amount: 950.00
        Tax Amount: 47.50
        Grand Total: 997.50
        LINE 10 | CHK001 | Chicken Breast | 48 KG | 500.00
        LINE 20 | RICE001 | Basmati Rice | 25 KG | 450.00
        """);

    public static readonly byte[] ScannedInvoice = Encoding.UTF8.GetBytes("""
        Invoice Number: SCANNED-2001
        Invoice Date: 2026-09-15
        Supplier: Desert Harvest General Trading
        TRN: 100987654300002
        PO Number: PO-SCANNED-2001
        Currency: AED
        Net Amount: 400.00
        Tax Amount: 20.00
        Grand Total: 420.00
        LINE 10 | FLOUR001 | Flour | 20 KG | 400.00
        """);

    public static readonly byte[] MultiPageLineItemInvoice = Pdf("""
        Invoice Number: MULTI-3001
        Invoice Date: 2026-09-16
        Supplier: Northern Supplies Company
        TRN: 100111222300004
        PO Number: PO-MULTI-3001
        Currency: USD
        Net Amount: 1250.00
        Tax Amount: 62.50
        Grand Total: 1312.50
        LINE 10 | ITEM-001 | Flour | 10 KG | 500.00
        LINE 20 | ITEM-002 | Rice | 25 KG | 450.00
        LINE 30 | ITEM-003 | Oil | 30 L | 300.00
        """);

    public static readonly byte[] ReconciliationFailureInvoice = Pdf("""
        Invoice Number: RECON-4001
        Invoice Date: 2026-09-17
        Supplier: Reconciliation Test Supplier
        PO Number: PO-RECON-4001
        Currency: AED
        Net Amount: 100.00
        Tax Amount: 5.00
        Grand Total: 120.00
        LINE 10 | ITEM-004 | Test item | 1 EA | 100.00
        """);

    public static readonly byte[] MissingRequiredFieldInvoice = Pdf("""
        Invoice Number: MISSING-5001
        Supplier: Missing Field Supplier
        PO Number: PO-MISSING-5001
        Net Amount: 200.00
        Tax Amount: 10.00
        Grand Total: 210.00
        """);

    private static byte[] Pdf(string text) =>
        Encoding.UTF8.GetBytes($"%PDF-1.7\n{text}\n%%EOF");
}