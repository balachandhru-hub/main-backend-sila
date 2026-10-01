namespace Supplier.Domain.Common
{
    public static class Common
    {
        public static readonly string APPLICATION_SCHEMA = "ConnectionStrings:Schema";
        public static readonly string DEFAULT_FRONT_END_ORIGIN_LOCAL = "Origin:HostOriginLocal";
        public static readonly string MAX_REQUEST_SIZE = "MaxRequestBodySize";
        public static readonly string UAT_ENVIRONMENT = "UAT";
        public static readonly string EMAIL_API_CREDENTIAL_DESCRIPTION = "EmailCredentials";
        public static readonly string MASTER_DATA_URL = "InterCallService:MasterDataUrl";
        public static readonly string METADATA_DOCUMENT_TYPE = "DOCUMENT_TYPE";
        public static readonly string ASSET_TYPE = "ASSET_TYPE";
        public static readonly string ENTITY_TYPE = "ENTITY_TYPE";
        public static readonly string FILE_TYPE = "FILE_TYPE";
        public static readonly string BASE_FOLDER_PATH = "FolderPath:BasePath";
        public static readonly string COOKIE_ACCESS_TOKEN_KEY = "access_token";
        public static readonly string PENDING_STATUS = "PENDING_VERIFICATION";
        public static readonly string METADATA_STATUS_TYPE = "STATUS";
        public static readonly string VERIFIED_STATUS = "VERIFIED";
        public static readonly string REJECTED_STATUS = "REJECTED";
        public static readonly string REVERIFICATION_STATUS = "RE_VERIFICATION";
        public static readonly string IDENTITY_SERVICE_BASE_URL = "InterCallService:IdentityUrl";
        public static readonly string ACCESS_TOKEN = "access_token";
        public static readonly string QUOTATION_STATUS = "DRAFT";
        public static readonly string PERCENTAGE = "PERCENTAGE";
        public static readonly string BUYER_SERVICE_BASE_URL = "InterCallService:BuyerUrl";
        public static readonly string UNVERIFIED_STATUS = "UNVERIFIED";
        public const string SUBMITTED = "SUBMITTED";
        public const string DRAFT = "DRAFT";
        public const string CATALOG = "CATALOG";
        public const string NON_CATALOG = "NONCATALOG";
        public const string SUBMITTED_STATUS = "SUBMITTED";
        public static readonly string EMAIL_VERIFICATION = "OTP_VERIFICATION";
        public static readonly string EMAIL_OTP = "OTP";
        public static readonly string EMAIL_OTP_VALIDITY = "OTP_VALIDITY";
        public static readonly string DOMAIN_COOKIE_NAME = "Domain:DomainName";
        public static readonly string VERIFICATION_TOKEN_COOKIE_NAME = "VerificationToken";
        public static readonly string RFQ_LIVE_STATUS = "LIVE";
        public const string AWARDED_STATUS = "AWARDED";
        public const string TERMS_CONDITION = "TERMS_CONDITION";
        public const string ESIGN = "ESIGN";
        public const string APPROVED = "APPROVE";
        public const string REJECTED = "REJECT";
        public const string ACCEPTED_STATUS = "ACCEPTED";
        public const string PENDING = "PENDING";
        public static Guid SUPPLIER_ADMIN_ROLE_ID = new Guid("735bb267-fec0-489f-8249-d3d65b3857ea");

        public const string SEND_MESSAGE_PERMISSION = "SEND_MESSAGE";
        public const string GET_MESSAGE_THREADS_PERMISSION = "GET_MESSAGE_THREADS";
        public const string GET_MESSAGE_HISTORY_PERMISSION = "GET_MESSAGE_HISTORY";
        public const string MARK_MESSAGE_READ_PERMISSION = "MARK_MESSAGE_READ";
        public const string DOWNLOAD_MESSAGE_ATTACHMENT_PERMISSION = "DOWNLOAD_MESSAGE_ATTACHMENT";

        // ---- RFQ status written by the buyer's Freeze Bid action. The Buyer service forwards the
        // posted value to UpdateSupplierRFQStatusCommandHandler, which stores it as-is ("Freezing").
        public const string RFQ_FREEZING_STATUS = "Freezing";

        // ---- Dashboard analytics (GET api/v1/supplier/dashboard-analytics)
        public const int DASHBOARD_TREND_MONTHS = 12;
        /// <summary>Rows returned per ranked breakdown (departments, suppliers, buyers).</summary>
        public const int DASHBOARD_MAX_BREAKDOWN_ROWS = 8;
        public const int DASHBOARD_UPCOMING_DEADLINES = 6;
        public const int DASHBOARD_CLOSING_SOON_DAYS = 7;
        public const string DASHBOARD_MONTH_FORMAT = "yyyy-MM";
        public const string DASHBOARD_STAGE_UPCOMING = "Upcoming";
        public const string DASHBOARD_STAGE_OPEN = "Open";
        public const string DASHBOARD_STAGE_QUOTED = "Quoted";
        public const string DASHBOARD_STAGE_FROZEN = "Frozen";
        public const string DASHBOARD_STAGE_CLOSED = "Closed";
        public const string DASHBOARD_STAGE_WON = "Won";
        public const string DASHBOARD_STAGE_NOT_AWARDED = "Not awarded";
        public static readonly string[] DASHBOARD_STAGE_ORDER =
        {
            DASHBOARD_STAGE_UPCOMING, DASHBOARD_STAGE_OPEN, DASHBOARD_STAGE_QUOTED, DASHBOARD_STAGE_FROZEN,
            DASHBOARD_STAGE_CLOSED, DASHBOARD_STAGE_WON, DASHBOARD_STAGE_NOT_AWARDED
        };
        public const string DASHBOARD_FUNNEL_INVITED = "Invited";
        public const string DASHBOARD_FUNNEL_QUOTED = "Quoted";
        public const string DASHBOARD_FUNNEL_WON = "Won";
        public const string DASHBOARD_UNKNOWN_BUYER = "Unknown buyer";
        /// <summary>Days-until-close buckets for the closing schedule (inclusive bounds).</summary>
        public static readonly (string Key, string Label, int FromDay, int ToDay)[] DASHBOARD_CLOSING_WINDOWS =
        {
            ("THIS_WEEK", "0–7 days", 0, 7),
            ("NEXT_WEEK", "8–14 days", 8, 14),
            ("TWO_TO_FOUR_WEEKS", "15–30 days", 15, 30),
            ("LATER", "30+ days", 31, int.MaxValue),
        };

        public const string ERP_TYPE_ANE_DCI = "ANE_DCI";
        public const string ERP_TYPE_ARIBA = "ARIBA";
        public const string ERP_TYPE_SAP_S4 = "SAP_S4";
        public const string PAYLOAD_CXML = "CXML";
        public const string AUTH_DCI_PASSWORD = "DCI_PASSWORD";
        public const string AUTH_NONE = "NONE";
        public const string AUTH_BASIC = "BASIC";
        public const string AUTH_API_KEY = "API_KEY";
        public const string AUTH_BEARER = "BEARER";
        public const string AUTH_OAUTH2_CLIENT_CREDENTIALS = "OAUTH2_CLIENT_CREDENTIALS";
        public const string INTEGRATION_SUCCEEDED = "SUCCEEDED";
        public const string INTEGRATION_FAILED = "FAILED";
        public const string INTEGRATION_UNKNOWN = "UNKNOWN";
        public const string ERP_DOCUMENT_PO = "PO";
    }
}
