
namespace Identity.Domain.Common
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
                public static readonly string LOGIN_ATTRIBUTE_LOGIN = "LOGIN";
                public const string TOKEN_ISSUER = "Tokens:Issuer";
                public static readonly string COOKIE_ACCESS_TOKEN_KEY = "access_token";
                public static readonly string COOKIE_REFRESH_TOKEN_KEY = "refresh_token";
                public static readonly string TOKEN_KEY = "Tokens:key";
                public static readonly string EMAIL_VERIFICATION = "OTP_VERIFICATION";
                public static readonly string EMAIL_OTP = "OTP";
                public static readonly string EMAIL_OTP_VALIDITY = "OTP_VALIDITY";
                public static readonly string MASTER_DATA_URL = "InterCallService:MasterDataUrl";
                public static readonly string MAX_ACTIVE_SESSIONS = "TokenSecurity:MaxActiveSessions";
                public static readonly string DOMAIN_COOKIE_NAME = "Domain:DomainName";
                public static readonly string VERIFICATION_TOKEN_COOKIE_NAME = "VerificationToken";
                public static readonly string REFRESH_TOKEN_EXPIRATION_TIME = "Tokens:RefershTokenExpirationTimeInSeconds";
                public static readonly string SUPPLIER_BASE_URL = "InterCallService:SupplierUrl";
                public static readonly string BUYER_BASE_URL = "InterCallService:BuyerUrl";
                public static readonly string ACCESS_TOKEN = "access_token";
                public static readonly string PLATFORM_ADMINISTRATOR = "PLATFORM_ADMINISTRATOR";
                public static readonly string SUPPLIER_NETWORK_ADMIN_KEY = "SUPPLIER_NETWORK_ADMIN";
                public static readonly string BUYER_NETWORK_ADMIN_KEY = "BUYER_NETWORK_ADMIN";
                public static readonly string PLATFORM_USER_KEY = "PLATFORM_ADMINISTRATOR";


                public const string BUYER_NETWORK_ADMIN = "BUYER_NETWORK_ADMIN";
                public const string SUPPLIER_NETWORK_ADMIN = "SUPPLIER_NETWORK_ADMIN";

                public const string BUYER_ADMINISTRATOR = "BUYER_ADMINISTRATOR";
                public const string SUPPLIER_ADMINISTRATOR = "SUPPLIER_ADMINISTRATOR";

                public const string BUYER_USER = "BUYER_USER";
                public const string SUPPLIER_USER = "SUPPLIER_USER";

        }
}