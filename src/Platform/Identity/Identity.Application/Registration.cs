using Microsoft.Extensions.DependencyInjection;
namespace HashingSystem
{

    public static class HashingRegistration
    {
        public enum HashTypes
        {
            Bcrypt = 1
        }

        /// <summary>
        /// Registers the required hashing technique
        /// </summary>
        /// <param name="keys">Salt and Workfactor for hashing</param>
        /// <param name="hashType">Type of hashing. Default is Bcrypt</param>
        public static void RegisterHashing(this IServiceCollection service, KeySpecs keys, HashTypes hashType = HashTypes.Bcrypt)
        {
            switch (hashType)
            {
                case HashTypes.Bcrypt:
                    service.AddScoped<IBcryptHashing>(_ => new BcryptHashing(keys));
                    break;
            }

        }
    }
}
