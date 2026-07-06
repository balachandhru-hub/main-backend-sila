

namespace Identity.Infrastructure.Contracts.IServices
{
    public interface IApiKeyService
    {
        /// <summary>
        /// Checks weather given api key is valid or not
        /// </summary>
        /// <param name="key">key</param>
        /// <returns>bool value</returns>
        bool IsApiKeyValid(string key);
    }
}