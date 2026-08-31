
namespace Buyer.Domain.Common
{
        /// <summary>
        ///
        /// </summary>
        public static class Common
        {
                public static readonly string APPLICATION_SCHEMA = "ConnectionStrings:Schema";
                public static readonly string DEFAULT_FRONT_END_ORIGIN_LOCAL = "Origin:HostOriginLocal";
                public static readonly string MAX_REQUEST_SIZE = "MaxRequestBodySize";
                public static readonly string UAT_ENVIRONMENT = "UAT";
                public static readonly string TOKEN_EXPIRY = "Tokens:TokenExpirationTimeInSeconds";
                public static int TOKEN_EXPIRY_TIME_DEFAULT = 3600;
                public static readonly string BASE_FOLDER_PATH = "FolderPath:BasePath";
                public static readonly string MASTER_DATA_URL = "InterCallService:MasterDataUrl";
                public static readonly string ASSET_TYPE = "ASSET_TYPE";
                public static readonly string ENTITY_TYPE = "ENTITY_TYPE";
                public static readonly string FILE_TYPE = "FILE_TYPE";
                public static readonly string PENDING_STATUS = "PENDING_VERIFICATION";
                public static readonly string METADATA_STATUS_TYPE = "STATUS";
                public static readonly string VERIFIED_STATUS = "VERIFIED";
                public static readonly string REJECTED_STATUS = "REJECTED";
                public static readonly string REVERIFICATION_STATUS = "RE_VERIFICATION";
                public static readonly string IDENTITY_SERVICE_BASE_URL = "InterCallService:IdentityUrl";
                public static readonly string SUPPLIER_SERVICE_BASE_URL = "InterCallService:SupplierUrl";
                public static readonly string ACCESS_TOKEN = "access_token";
                public static readonly string METADATA_DOCUMENT_TYPE = "DOCUMENT_TYPE";
                public static readonly string RFQ_OPEN_STATUS = "Open";
                public static readonly string TERMS_CONDITION = "TERMS_CONDITION";
                public static readonly string TECHNICAL_SPECIFICATION = "TECHNICAL_SPECIFICATION";
                public static int DISPLAY_ORDER = 1;
                public const string PENDING = "PENDING";
                public static readonly string RFQ_ITEM_ATTACHMENT = "RFQ_ITEM_ATTACHMENT";
                public const string DEFAULT = "DEFAULT";
                public const string SUBMITTED = "SUBMITTED";
                public const string DRAFT = "DRAFT";
                public static Guid SUPPLIER_ROLE_ID = new Guid("735bb267-fec0-489f-8249-d3d65b3857ea");
                public static Guid BUYER_ROLE_ID = new Guid("c95f5a1b-4aec-4647-9328-895a58193ec4");
                public const string DEFAULT_TEMPLATE = "DEFAULT_TEMPLATE";
                public const string BUYER = "BUYER";
                public const string RADIO_BUTTON = "Radio";
                public const string CHECKBOX = "Checkbox";
                public const string ACCEPT = "Accept";
                public const string DECLINE = "Decline";
                public static Guid DEFAULT_VERIFICATION_TEMPLATE_ID = new Guid("DD5A50B8-F087-4F0A-A004-CE43E92CE2B9");
                public const string SUPPLIER_QUOTATION="UPPLIER_QUOTATION";
                public const string QUOTATION_SUBMITTED="QUOTATION_SUBMITTED";
                public const string HYPERLEDGER_FABRIC="HYPERLEDGER_FABRIC";
                public const string BLOCKCHAIN_KEY="Encryption:AesKey";

        }
}