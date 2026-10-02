using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using SharedKernel.ExceptionHandler;

namespace SharedKernel.Security
{
    /// <summary>
    /// Key for calls one service makes to another on its own behalf (no user token). It is derived
    /// from the token signing key every service already shares; it is a one-way hash, so presenting
    /// it does not reveal the signing key.
    /// </summary>
    public static class InternalServiceKey
    {
        public const string HEADER = "X-Internal-Key";

        public static string Value(IConfiguration configuration)
        {
            string signingKey = configuration["Tokens:Key"]
                ?? throw new InvalidOperationException("'Tokens:Key' is not configured.");
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{signingKey}:internal-service-call"));
            return Convert.ToHexString(hash);
        }

        /// <summary>
        /// Refuses a request that does not carry the internal key. Used by the endpoints that only
        /// other services may call.
        /// </summary>
        public static void Require(HttpRequest request, IConfiguration configuration)
        {
            string? presented = request.Headers[HEADER].FirstOrDefault();
            byte[] presentedBytes = Encoding.UTF8.GetBytes(presented ?? string.Empty);
            byte[] expectedBytes = Encoding.UTF8.GetBytes(Value(configuration));
            if (!CryptographicOperations.FixedTimeEquals(presentedBytes, expectedBytes))
            {
                throw new UnAuthorizedCustomException("Unauthorized", "Internal key is required.");
            }
        }
    }
}
