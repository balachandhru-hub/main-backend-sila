using System.Security.Cryptography;
using System.Text;

namespace HashingSystem
{
    public class Sha256Hashing : ISha256Hashing
    {
        public string GenerateHash(string data)
        {
            if (string.IsNullOrEmpty(data))
                return string.Empty;

            byte[] bytes = Encoding.UTF8.GetBytes(data);

            byte[] hash = SHA256.HashData(bytes);

            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}