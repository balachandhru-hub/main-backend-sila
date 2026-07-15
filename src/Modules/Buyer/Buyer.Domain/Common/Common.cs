
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
                public static readonly string PENDING_STATUS="PENDING_VERIFICATION";
                 public static readonly string METADATA_STATUS_TYPE = "STATUS";
                public static readonly string VERIFIED_STATUS="VERIFIED";
                public static readonly string REJECTED_STATUS="REJECTED";
                public static readonly string REVERIFICATION_STATUS = "RE_VERIFICATION";
                public static readonly string IDENTITY_SERVICE_BASE_URL = "InterCallService:IdentityUrl";
                public static readonly string ACCESS_TOKEN = "access_token";
                public static readonly string METADATA_DOCUMENT_TYPE = "DOCUMENT_TYPE";
               
        }
}