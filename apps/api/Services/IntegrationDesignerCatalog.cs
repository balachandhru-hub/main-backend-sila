using SilaMe.Api.DTOs;
using SilaMe.Api.Models;

namespace SilaMe.Api.Services;

public static class IntegrationDesignerCatalog
{
    public static readonly IReadOnlyDictionary<IntegrationSystemKind, string> SystemLabels = new Dictionary<IntegrationSystemKind, string>
    {
        [IntegrationSystemKind.SAP_S4HANA] = "SAP S/4HANA",
        [IntegrationSystemKind.SAP_ARIBA] = "SAP Ariba",
        [IntegrationSystemKind.ORACLE] = "Oracle",
        [IntegrationSystemKind.ODOO] = "Odoo",
        [IntegrationSystemKind.CUSTOM] = "Custom API",
        [IntegrationSystemKind.OTHER] = "Other",
    };

    public static readonly IReadOnlyDictionary<IntegrationSystemKind, IntegrationProtocol[]> InterfacesBySystem = new Dictionary<IntegrationSystemKind, IntegrationProtocol[]>
    {
        [IntegrationSystemKind.SAP_S4HANA] = [IntegrationProtocol.ODATA_V2, IntegrationProtocol.ODATA_V4, IntegrationProtocol.REST],
        [IntegrationSystemKind.SAP_ARIBA] = [IntegrationProtocol.REST, IntegrationProtocol.SOAP],
        [IntegrationSystemKind.ORACLE] = [IntegrationProtocol.REST, IntegrationProtocol.SOAP],
        [IntegrationSystemKind.ODOO] = [IntegrationProtocol.REST],
        [IntegrationSystemKind.CUSTOM] = [IntegrationProtocol.REST, IntegrationProtocol.SOAP, IntegrationProtocol.ODATA_V2, IntegrationProtocol.ODATA_V4],
        [IntegrationSystemKind.OTHER] = [IntegrationProtocol.REST, IntegrationProtocol.SOAP, IntegrationProtocol.ODATA_V2, IntegrationProtocol.ODATA_V4],
    };

    public static IReadOnlyList<IntegrationAuthenticationType> AuthenticationFor(IntegrationSystemKind system, IntegrationProtocol protocol)
    {
        if (system == IntegrationSystemKind.SAP_S4HANA && protocol is IntegrationProtocol.ODATA_V2 or IntegrationProtocol.ODATA_V4)
            return [IntegrationAuthenticationType.BASIC, IntegrationAuthenticationType.OAUTH2_CLIENT_CREDENTIALS];
        if (system == IntegrationSystemKind.SAP_ARIBA && protocol == IntegrationProtocol.SOAP)
            return [IntegrationAuthenticationType.BASIC];
        if (system == IntegrationSystemKind.SAP_ARIBA && protocol == IntegrationProtocol.REST)
            return [IntegrationAuthenticationType.CUSTOM_TOKEN_ENDPOINT, IntegrationAuthenticationType.API_KEY, IntegrationAuthenticationType.BASIC, IntegrationAuthenticationType.OAUTH2_CLIENT_CREDENTIALS];
        if (system == IntegrationSystemKind.ORACLE && protocol == IntegrationProtocol.SOAP)
            return [IntegrationAuthenticationType.BASIC];
        if (system == IntegrationSystemKind.ORACLE)
            return [IntegrationAuthenticationType.BASIC, IntegrationAuthenticationType.BEARER_TOKEN, IntegrationAuthenticationType.OAUTH2_CLIENT_CREDENTIALS, IntegrationAuthenticationType.API_KEY, IntegrationAuthenticationType.CUSTOM_TOKEN_ENDPOINT];
        if (system == IntegrationSystemKind.ODOO)
            return [IntegrationAuthenticationType.NONE, IntegrationAuthenticationType.BASIC, IntegrationAuthenticationType.API_KEY, IntegrationAuthenticationType.OAUTH2_CLIENT_CREDENTIALS, IntegrationAuthenticationType.CUSTOM_TOKEN_ENDPOINT];
        return
        [
            IntegrationAuthenticationType.NONE, IntegrationAuthenticationType.BASIC, IntegrationAuthenticationType.API_KEY,
            IntegrationAuthenticationType.BEARER_TOKEN, IntegrationAuthenticationType.OAUTH2_CLIENT_CREDENTIALS,
            IntegrationAuthenticationType.CUSTOM_TOKEN_ENDPOINT, IntegrationAuthenticationType.CUSTOM_HEADER
        ];
    }

    public static bool IsPush(IntegrationProcessType process) =>
        process.ToString().StartsWith("POST_", StringComparison.Ordinal) ||
        process.ToString().StartsWith("UPDATE_", StringComparison.Ordinal) ||
        process.ToString().StartsWith("CANCEL_", StringComparison.Ordinal);

    public static IntegrationMappingDirection Direction(IntegrationProcessType process) =>
        IsPush(process) ? IntegrationMappingDirection.SILA_TO_EXTERNAL : IntegrationMappingDirection.EXTERNAL_TO_SILA;

    public static IntegrationDesignerCatalogResponse Catalog() => new(
        SystemLabels.Select(item => new IntegrationCatalogOption(item.Key.ToString(), item.Value)).ToList(),
        InterfacesBySystem.ToDictionary(item => item.Key.ToString(), item => item.Value.Select(value => value.ToString()).ToList()),
        Enum.GetValues<IntegrationProcessType>().Select(item => new IntegrationCatalogOption(item.ToString(), item.ToString().Replace('_', ' '))).ToList(),
        SourceStructures(),
        Recommendations().ToDictionary(item => item.Key.ToString(), item => item.Value),
        Enum.GetValues<IntegrationSystemKind>().SelectMany(system => InterfacesBySystem[system].Select(protocol => new
        {
            Key = $"{system}:{protocol}",
            Auth = AuthenticationFor(system, protocol).Select(value => value.ToString()).ToList()
        })).ToDictionary(item => item.Key, item => item.Auth));

    public static IReadOnlyList<IntegrationSourceStructure> SourceStructures() =>
    [
        Structure("PURCHASE_ORDER_HEADER", "Purchase Order Header", false, [
            Field("PoNumber", "Purchase Order"), Field("SupplierName", "Supplier"), Field("CompanyCode", "Company Code"),
            Field("Currency", "Currency"), Field("PoDate", "PO Date"), Field("DeliveryDate", "Delivery Date"),
            Field("PurchaseOrderType", "PO Type"), Field("PurchasingOrganization", "Purchasing Organization"),
            Field("PaymentTerms", "Payment Terms"), Field("TotalNetAmount", "Total Net Amount"), Field("TotalAmount", "Total Amount"),
            Field("Status", "Status"), Field("ExternalId", "External Id")
        ]),
        Structure("PURCHASE_ORDER_ITEM", "Purchase Order Items", true, [
            Field("LineNumber", "PO Item"), Field("ItemNumber", "Item Number"), Field("MaterialCode", "Material"),
            Field("Description", "Description"), Field("Plant", "Plant"), Field("StorageLocation", "Storage Location"),
            Field("OrderedQuantity", "Ordered Quantity"), Field("ReceivedQuantity", "Received Quantity"),
            Field("OpenQuantity", "Open Quantity"), Field("Uom", "UOM"), Field("UnitPrice", "Net Price"),
            Field("Currency", "Currency"), Field("Status", "Status")
        ]),
        Structure("INVOICE_HEADER", "Invoice Header", false, [
            Field("InvoiceNumber", "Invoice Number"), Field("InvoiceDate", "Invoice Date"), Field("SupplierNameRaw", "Supplier"),
            Field("PoNumberRaw", "Purchase Order"), Field("Currency", "Currency"), Field("NetAmount", "Net Amount"),
            Field("TaxAmount", "Tax Amount"), Field("GrossAmount", "Gross Amount"), Field("DueDate", "Due Date"),
            Field("PaymentTerms", "Payment Terms"), Field("Status", "Status")
        ]),
        Structure("INVOICE_ITEM", "Invoice Items", true, [
            Field("LineNumber", "Line"), Field("MaterialCodeRaw", "Material"), Field("DescriptionRaw", "Description"),
            Field("Quantity", "Quantity"), Field("Uom", "UOM"), Field("UnitPrice", "Unit Price"),
            Field("LineAmount", "Line Amount"), Field("PoItemNumber", "PO Item")
        ]),
        Structure("INVOICE_EXTRACTION", "Invoice Extraction", false, [
            Field("SupplierName", "Extracted Supplier"), Field("SupplierInvoiceNumber", "Extracted Invoice Number"),
            Field("PurchaseOrderNumber", "Extracted Purchase Order"), Field("InvoiceGross", "Extracted Gross"),
            Field("Currency", "Extracted Currency")
        ]),
        Structure("GRN_HEADER", "GRN Header", false, [
            Field("GrnNumber", "GRN Number"), Field("ReceiptDate", "Posting Date"), Field("CreatedAt", "Document Date"),
            Field("ExternalReference", "Reference"), Field("Status", "Status"),
            Field("PurchaseOrderNumber", "Purchase Order", true), Field("SupplierCode", "Supplier", true)
        ]),
        Structure("GRN_ITEM", "GRN Items", true, [
            Field("PurchaseOrderItemNumber", "PO Item", true), Field("MaterialCode", "Material"),
            Field("AcceptedQuantity", "Accepted Quantity"), Field("DamagedQuantity", "Damaged Quantity"),
            Field("RejectedQuantity", "Rejected Quantity"), Field("ReceivedQuantity", "Received Quantity"),
            Field("Uom", "UOM"), Field("Plant", "Plant", true), Field("StorageLocation", "Storage Location", true),
            Field("BatchNumber", "Batch")
        ]),
        Structure("SUPPLIER", "Supplier Master", false, [
            Field("SupplierCode", "Supplier Code"), Field("Name", "Name"), Field("TaxNumber", "Tax Number"),
            Field("Email", "Email"), Field("CompanyCode", "Company Code"), Field("Currency", "Currency"), Field("Country", "Country")
        ]),
        Structure("MATERIAL", "Material Master", false, [
            Field("MaterialCode", "Material"), Field("Description", "Description"), Field("BaseUom", "Base UOM"), Field("Category", "Category")
        ]),
        Structure("INVENTORY", "Inventory", false, [
            Field("MaterialCode", "Material"), Field("Quantity", "Quantity"), Field("Uom", "UOM")
        ]),
        Structure("INVENTORY_MOVEMENT", "Inventory Movement", false, [
            Field("MaterialCode", "Material"), Field("Quantity", "Quantity"), Field("Uom", "UOM"),
            Field("Plant", "Plant"), Field("StorageLocation", "Storage Location"), Field("PostingDate", "Posting Date"),
            Field("Reference", "Reference")
        ]),
        Structure("POS_TRANSACTION", "POS Transaction", false, [
            Field("SourceTransactionId", "POS Transaction Id"), Field("BusinessDate", "Business Date"),
            Field("OutletCode", "Outlet"), Field("ItemCode", "POS Item"), Field("Quantity", "Quantity")
        ]),
        Structure("COMPANY_CODE", "Company Code", false, [Field("CompanyCode", "Company Code")]),
        Structure("PLANT", "Plant", false, [Field("Plant", "Plant")]),
        Structure("STORAGE_LOCATION", "Storage Location", false, [Field("StorageLocation", "Storage Location")]),
        Structure("PROPERTY", "Property", false, [Field("Name", "Name"), Field("Code", "Code")]),
        Structure("LOCATION", "Location", false, [Field("Name", "Name"), Field("Code", "Code")]),
        Structure("DOCUMENT", "Document", false, [
            Field("OriginalFilename", "File Name"), Field("ContentType", "Content Type"), Field("DocumentType", "Document Type")
        ]),
        Structure("USER_CONTEXT", "User Context", false, [
            Field("UserId", "User Id"), Field("Email", "Email")
        ]),
    ];

    public static IReadOnlyDictionary<IntegrationProcessType, IntegrationSourceRecommendation> Recommendations() => new Dictionary<IntegrationProcessType, IntegrationSourceRecommendation>
    {
        [IntegrationProcessType.POST_GRN] = new(["GRN_HEADER", "GRN_ITEM"], ["PURCHASE_ORDER_HEADER", "PURCHASE_ORDER_ITEM"], ["SUPPLIER", "MATERIAL", "COMPANY_CODE", "PLANT", "STORAGE_LOCATION", "USER_CONTEXT"]),
        [IntegrationProcessType.POST_INVOICE] = new(["INVOICE_HEADER", "INVOICE_ITEM"], ["INVOICE_EXTRACTION", "PURCHASE_ORDER_HEADER", "PURCHASE_ORDER_ITEM", "GRN_HEADER", "GRN_ITEM"], ["SUPPLIER", "COMPANY_CODE", "DOCUMENT"]),
        [IntegrationProcessType.POST_PO] = new(["PURCHASE_ORDER_HEADER", "PURCHASE_ORDER_ITEM"], [], ["SUPPLIER", "MATERIAL", "COMPANY_CODE", "PLANT", "STORAGE_LOCATION"]),
        [IntegrationProcessType.GET_PO] = new(["PURCHASE_ORDER_HEADER", "PURCHASE_ORDER_ITEM"], ["SUPPLIER"], ["MATERIAL", "COMPANY_CODE", "PLANT", "STORAGE_LOCATION"]),
        [IntegrationProcessType.GET_SUPPLIER] = new(["SUPPLIER"], [], ["COMPANY_CODE"]),
        [IntegrationProcessType.GET_MATERIAL] = new(["MATERIAL"], [], ["PLANT", "STORAGE_LOCATION"]),
        [IntegrationProcessType.GET_GRN] = new(["GRN_HEADER", "GRN_ITEM"], ["PURCHASE_ORDER_HEADER", "PURCHASE_ORDER_ITEM"], ["SUPPLIER", "MATERIAL"]),
        [IntegrationProcessType.GET_INVOICE] = new(["INVOICE_HEADER", "INVOICE_ITEM"], ["INVOICE_EXTRACTION", "PURCHASE_ORDER_HEADER"], ["SUPPLIER", "DOCUMENT"]),
        [IntegrationProcessType.GET_STOCK] = new(["INVENTORY"], ["MATERIAL"], ["PLANT", "STORAGE_LOCATION"]),
        [IntegrationProcessType.GET_COMPANY_CODE] = new(["COMPANY_CODE"], [], []),
        [IntegrationProcessType.GET_PLANT] = new(["PLANT"], [], ["COMPANY_CODE"]),
        [IntegrationProcessType.GET_STORAGE_LOCATION] = new(["STORAGE_LOCATION"], [], ["PLANT"]),
        [IntegrationProcessType.UPDATE_STOCK] = new(["INVENTORY", "INVENTORY_MOVEMENT"], ["MATERIAL"], ["PLANT", "STORAGE_LOCATION"]),
        [IntegrationProcessType.POST_INVENTORY_ADJUSTMENT] = new(["INVENTORY_MOVEMENT"], ["MATERIAL"], ["PLANT", "STORAGE_LOCATION", "COMPANY_CODE"]),
        [IntegrationProcessType.GET_POS_SALE] = new(["POS_TRANSACTION"], [], ["PROPERTY", "LOCATION"]),
        [IntegrationProcessType.CANCEL_GRN] = new(["GRN_HEADER"], ["GRN_ITEM"], ["PURCHASE_ORDER_HEADER"]),
        [IntegrationProcessType.UPDATE_PO] = new(["PURCHASE_ORDER_HEADER", "PURCHASE_ORDER_ITEM"], [], ["SUPPLIER", "MATERIAL"]),
    };

    public static IntegrationDesignerDocument DefaultsFor(IntegrationSystemKind system, IntegrationProcessType process, IntegrationProtocol protocol)
    {
        var csrf = system == IntegrationSystemKind.SAP_S4HANA && protocol is IntegrationProtocol.ODATA_V2 or IntegrationProtocol.ODATA_V4 && IsPush(process);
        var recommendation = Recommendations().GetValueOrDefault(process);
        var selected = (recommendation?.Primary ?? []).Concat(recommendation?.Recommended ?? []).Distinct().ToList();
        return new IntegrationDesignerDocument
        {
            ContentType = protocol == IntegrationProtocol.SOAP ? "text/xml" : "application/json",
            Accept = protocol == IntegrationProtocol.SOAP ? "text/xml" : "application/json",
            CookieHandling = "AUTOMATIC",
            CsrfRequired = csrf,
            CsrfFetchMethod = "GET",
            CsrfHeaderName = "X-CSRF-Token",
            CsrfHeaderValue = "Fetch",
            CsrfResponseHeader = "X-CSRF-Token",
            RequestBodyMode = protocol == IntegrationProtocol.SOAP ? IntegrationRequestBodyMode.XML_TEMPLATE : IntegrationRequestBodyMode.FIELD_MAPPING,
            ExecutionMode = IsPush(process) ? IntegrationExecutionMode.EVENT_DRIVEN : IntegrationExecutionMode.MANUAL,
            IdempotencyEnabled = IsPush(process),
            SelectedSourceStructures = selected,
            SuccessHttpCodes = [200, 201, 202, 204],
            SoapVersion = "1.1",
            TokenHttpMethod = "POST",
            TokenAuthenticationType = IntegrationAuthenticationType.NONE.ToString(),
        };
    }

    public static IntegrationDesignerDraft FiveS4PostGrnSample() => new()
    {
        Name = "FIVE S4 - POST GRN",
        Description = "SAP S/4HANA Cloud goods-receipt posting (Phase 1 configuration only).",
        SystemKind = IntegrationSystemKind.SAP_S4HANA,
        ProcessType = IntegrationProcessType.POST_GRN,
        Protocol = IntegrationProtocol.ODATA_V2,
        BaseUrl = "https://my419951-api.s4hana.cloud.sap",
        ServicePath = "/sap/opu/odata/sap/API_MATERIAL_DOCUMENT_SRV",
        EntitySet = "A_MaterialDocumentHeader",
        HttpMethod = "POST",
        AuthenticationType = IntegrationAuthenticationType.BASIC,
        TimeoutSeconds = 30,
        RetryCount = 2,
        Priority = 100,
        EntityCode = "ALL",
        Designer = DefaultsFor(IntegrationSystemKind.SAP_S4HANA, IntegrationProcessType.POST_GRN, IntegrationProtocol.ODATA_V2) with
        {
            CsrfRequired = true,
            CsrfFetchPath = "/sap/opu/odata/sap/API_MATERIAL_DOCUMENT_SRV/",
            ExternalDocumentNumberPath = "MaterialDocument",
            ExternalDocumentYearPath = "MaterialDocumentYear",
        },
        Mappings =
        [
            Map("GRN_HEADER", "ReceiptDate", "PostingDate"),
            Map("GRN_HEADER", "CreatedAt", "DocumentDate"),
            Map("PURCHASE_ORDER_HEADER", "PoNumber", "PurchaseOrder"),
            Map("GRN_ITEM", "PurchaseOrderItemNumber", "MaterialDocumentItems[].PurchaseOrderItem", true),
            Map("GRN_ITEM", "MaterialCode", "MaterialDocumentItems[].Material", true),
            Map("GRN_ITEM", "AcceptedQuantity", "MaterialDocumentItems[].Quantity", true),
            Map("GRN_ITEM", "Uom", "MaterialDocumentItems[].UOM", true),
            Map("GRN_ITEM", "Plant", "MaterialDocumentItems[].Plant", true),
        ],
        ResponseMappings =
        [
            new IntegrationResponseMappingInput { SourcePath = "MaterialDocument", TargetField = "ExternalDocumentNumber" },
            new IntegrationResponseMappingInput { SourcePath = "MaterialDocumentYear", TargetField = "ExternalDocumentYear" },
        ]
    };

    private static DesignerMappingInput Map(string structure, string source, string target, bool collection = false) => new()
    {
        SourceKind = IntegrationMappingSourceKind.FIELD, SourceStructure = structure, SourceField = source, TargetField = target, IsCollection = collection
    };

    private static IntegrationSourceStructure Structure(string code, string label, bool collection, IReadOnlyList<IntegrationSourceField> fields) =>
        new(code, label, collection, fields);

    private static IntegrationSourceField Field(string name, string label, bool derived = false) => new(name, label, derived);
}
