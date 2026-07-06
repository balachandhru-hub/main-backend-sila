
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
                public static int TOKEN_EXPIRY_TIME_DEFAULT = 1200;
                public static readonly string LOGIN_ATTRIBUTE_LOGIN = "LOGIN";
                public const string TOKEN_ISSUER = "Tokens:Issuer";
                public static readonly string COOKIE_ACCESS_TOKEN_KEY = "access_token";
                public static readonly string COOKIE_REFRESH_TOKEN_KEY = "refresh_token";
                public static readonly string TOKEN_KEY = "Tokens:key";
                public static readonly string EMAIL_VERIFICATION="OTP_VERIFICATION";
                public static readonly string EMAIL_OTP="OTP";
                public static readonly string EMAIL_OTP_VALIDITY="OTP_VALIDITY";
                public static readonly string MASTER_DATA_URL = "InterCallService:MasterDataUrl";
                public static readonly string MAX_ACTIVE_SESSIONS = "TokenSecurity:MaxActiveSessions";
        }
}