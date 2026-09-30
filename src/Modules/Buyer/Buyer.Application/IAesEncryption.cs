namespace HashingSystem
{
    public interface IAesEncryption
    {
        string Encrypt(string plainText);
        string Decrypt(string cipherText);
    }
}