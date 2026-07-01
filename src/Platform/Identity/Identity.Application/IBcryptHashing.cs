namespace HashingSystem
{
    public interface IBcryptHashing
    {

        /// <summary>
        /// Hash a key using the OpenBSD BCrypt scheme with the configured salt.
        /// </summary>
        /// <param name="key">The key to hash.</param>
        /// <returns>
        ///  The hashed key.
        /// </returns>
        string HashStringWithSalt(string key);
        /// <summary>
        /// Used for the Comparing the Values
        /// </summary>
        /// <param name="plainText"></param>
        /// <param name="hashedValue"></param>
        /// <returns></returns>
        bool VerifyHash(string plainText, string hashedValue);

    }
}
