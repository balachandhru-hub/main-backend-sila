using System.Security.Cryptography;
using System.Text;
using Buyer.Domain.Common;
using Microsoft.Extensions.Configuration;

namespace HashingSystem
{
    public class AesEncryption : IAesEncryption
    {
        private readonly byte[] _key;

        public AesEncryption(IConfiguration configuration)
        {
            var key = configuration[Common.BLOCKCHAIN_KEY];

            if (string.IsNullOrWhiteSpace(key))
                throw new InvalidOperationException(
                    "Encryption:AesKey is not configured.");

            _key = Convert.FromBase64String(key);

            if (_key.Length != 32)
                throw new InvalidOperationException(
                    "AES-256 key must be exactly 32 bytes.");
        }

        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            using var aes = Aes.Create();

            aes.Key = _key;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();

            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

            byte[] cipherBytes = encryptor.TransformFinalBlock(
                plainBytes,
                0,
                plainBytes.Length);

            // Store IV + encrypted data together
            byte[] result = new byte[
                aes.IV.Length + cipherBytes.Length];

            Buffer.BlockCopy(
                aes.IV,
                0,
                result,
                0,
                aes.IV.Length);

            Buffer.BlockCopy(
                cipherBytes,
                0,
                result,
                aes.IV.Length,
                cipherBytes.Length);

            return Convert.ToBase64String(result);
        }

        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return string.Empty;

            byte[] encryptedBytes =
                Convert.FromBase64String(cipherText);

            using var aes = Aes.Create();

            aes.Key = _key;

            // First 16 bytes are the IV
            byte[] iv = new byte[16];

            byte[] cipherBytes =
                new byte[encryptedBytes.Length - 16];

            Buffer.BlockCopy(
                encryptedBytes,
                0,
                iv,
                0,
                16);

            Buffer.BlockCopy(
                encryptedBytes,
                16,
                cipherBytes,
                0,
                cipherBytes.Length);

            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();

            byte[] plainBytes = decryptor.TransformFinalBlock(
                cipherBytes,
                0,
                cipherBytes.Length);

            return Encoding.UTF8.GetString(plainBytes);
        }
    }
}