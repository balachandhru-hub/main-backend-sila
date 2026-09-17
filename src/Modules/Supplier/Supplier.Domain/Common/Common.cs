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
        public static Guid SUPPLIER_ADMIN_ROLE_ID = new Guid("735bb267-fec0-489f-8249-d3d65b3857ea");

        public const string SEND_MESSAGE_PERMISSION = "SEND_MESSAGE";
        public const string GET_MESSAGE_THREADS_PERMISSION = "GET_MESSAGE_THREADS";
        public const string GET_MESSAGE_HISTORY_PERMISSION = "GET_MESSAGE_HISTORY";
        public const string MARK_MESSAGE_READ_PERMISSION = "MARK_MESSAGE_READ";
        public const string DOWNLOAD_MESSAGE_ATTACHMENT_PERMISSION = "DOWNLOAD_MESSAGE_ATTACHMENT";

    }

}