using System;

namespace HashingSystem
{
    public class BcryptHashing : IBcryptHashing
    {
        private readonly KeySpecs _tokens;
        public BcryptHashing(KeySpecs tokens)
        {
            _tokens = tokens;
        }


        /// <summary>
        /// Hash a key using the OpenBSD BCrypt scheme with the configured salt.
        /// </summary>
        /// <param name="key">The key to hash.</param>
        /// <returns>
        ///  The hashed key.
        /// </returns>
        public string HashStringWithSalt(string key)
        {
            if (String.IsNullOrEmpty(key)) return null;
            return BCrypt.Net.BCrypt.HashPassword(key, _tokens.Salt);
        }
        /// <summary>
        /// Verify a plain text password against a hashed password.
        /// </summary>
        /// <param name="plainText">The plain text password to verify.</param>
        /// <param name="hashedValue">The hashed password to compare against.</param>
        /// <returns>
        ///  True if the plain text password matches the hashed password, otherwise false.
        /// </returns>
        public bool VerifyHash(string plainText, string hashedValue)
        {
            if (String.IsNullOrEmpty(plainText) || String.IsNullOrEmpty(hashedValue)) return false;
            return BCrypt.Net.BCrypt.Verify(plainText, hashedValue);
        }
    }
}
