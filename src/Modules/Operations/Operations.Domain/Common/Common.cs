namespace Operations.Domain.Common
{
    /// <summary>
    /// Configuration keys and constants of the Operations service.
    /// </summary>
    public static class Common
    {
        public static readonly string APPLICATION_SCHEMA = "ConnectionStrings:Schema";
        public static readonly string DEFAULT_FRONT_END_ORIGIN_LOCAL = "Origin:HostOriginLocal";
        public static readonly string MAX_REQUEST_SIZE = "MaxRequestBodySize";
        public static readonly string UAT_ENVIRONMENT = "UAT";
        public static readonly string BASE_FOLDER_PATH = "FolderPath:BasePath";
        public static readonly string IDENTITY_SERVICE_BASE_URL = "InterCallService:IdentityUrl";
        public static readonly string ACCESS_TOKEN = "access_token";
        public static readonly string TOKEN_KEY = "Tokens:Key";
        public const string IDEMPOTENCY_HEADER = "Idempotency-Key";

        // ---- Local document storage: {FolderPath:BasePath}/operations/documents/{organizationId}/{file}
        public const string DOCUMENT_SUBFOLDER = "operations/documents";
        public const string DATA_PROTECTION_SUBFOLDER = "operations/dataprotection-keys";
        public const string STORAGE_PROVIDER_LOCAL = "LOCAL_DISK";
        public const long MAX_DOCUMENT_SIZE = 20 * 1024 * 1024;
        public const long MAX_IMPORT_SIZE = 10 * 1024 * 1024;
        public const int MAX_IMPORT_ROWS = 5000;

        // ---- OCR (binary names can be overridden; SILAME shells out to pdftoppm and tesseract)
        public static readonly string OCR_PDF_RENDERER = "Ocr:PdfToPpmPath";
        public static readonly string OCR_TESSERACT = "Ocr:TesseractPath";
        public static readonly string OCR_LANGUAGE = "Ocr:Language";
        public const string OCR_PROVIDER_BUILT_IN = "BUILT_IN_OCR";
        public const string OCR_PROVIDER_EMBEDDED_TEXT = "EMBEDDED_PDF_TEXT";
        public const string OCR_PROVIDER_ADVANCED = "BUILT_IN_ADVANCED";
        public const string OCR_PROVIDER_MOBILE = "MOBILE_OCR";
        public const string OCR_UNAVAILABLE = "OCR_UNAVAILABLE";
        public const string OCR_FAILED = "OCR_FAILED";
        public const string MANUAL_ENTRY_REQUIRED = "MANUAL_ENTRY_REQUIRED";
        public const string OCR_UNAVAILABLE_MESSAGE = "OCR tools are not installed on this server. Enter the invoice details manually.";
        public const string PENDING_INVOICE_PREFIX = "PENDING-";

        // ---- Microsoft Graph / SharePoint (SILAME key names)
        public const string MICROSOFT_SECTION = "Microsoft";
        public const string MICROSOFT_CONNECTION_NAME = "Microsoft SharePoint";
        public const string MICROSOFT_DEFAULT_SCOPES = "openid profile offline_access User.Read Sites.ReadWrite.All";
        public const string MICROSOFT_DEFAULT_RETURN_URL = "/operations-document-storage";

        // ---- Idempotent operations
        public const string OPERATION_SAVE_INVOICE = "SAVE_INVOICE";
        public const string OPERATION_POST_GRN = "POST_GRN";

        // ---- Goods receipt
        public const string GRN_POSTING_PROVIDER = "LOCAL_DEVELOPMENT";
        public const string GRN_REFERENCE_TYPE = "GOODS_RECEIPT";
        public const string GRN_BUSINESS_RECEIVING = "RECEIVING";
        public const string GRN_BUSINESS_POSTED = "POSTED";
        public const string GRN_BUSINESS_ERP_FAILED = "ERP_POST_FAILED";
        public const string GRN_BUSINESS_RECONCILIATION = "ERP_RECONCILIATION_REQUIRED";
        public const string ERP_STATUS_PENDING = "PENDING";
        public const string ERP_STATUS_RETRYING = "RETRYING";
        public const string ERP_STATUS_POSTED = "POSTED";
        public const string ERP_STATUS_FAILED = "FAILED";
        public const string ERP_STATUS_UNKNOWN = "UNKNOWN";
        public const string ERP_STATUS_NOT_CONFIGURED = "NOT_CONFIGURED";

        // ---- ERP integration
        public const string SOURCE_SYSTEM_INTEGRATION = "API_INTEGRATION";
        public const string SOURCE_SYSTEM_MANUAL = "SILA";
        public const string ENTITY_CODE_ALL = "ALL";
        public const string ENTITY_CODE_DEFAULT = "DEFAULT";
        public const string INTEGRATION_CREDENTIAL_PURPOSE = "SilaMe.Api.IntegrationCredentials.v1";
        public const string HTTP_CLIENT_INTEGRATIONS = "api-integrations";
        public const string HTTP_CLIENT_EXTRACTION = "external-extraction";
        public const string HTTP_CLIENT_GRAPH = "microsoft-graph";
        public const string SPREADSHEET_CONTENT_TYPE = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        public const string CSV_CONTENT_TYPE = "text/csv; charset=utf-8";

        // ---- Resolution source of a document destination
        public const string RESOLUTION_USER = "USER";
        public const string RESOLUTION_STORE = "STORE";
        public const string RESOLUTION_PROPERTY = "PROPERTY";
        public const string RESOLUTION_ORGANIZATION = "ORGANIZATION";
    }
}
