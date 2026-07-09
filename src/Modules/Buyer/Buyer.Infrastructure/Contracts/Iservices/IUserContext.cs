namespace Buyer.Infrastructure.Contracts.IServices
{
    public interface IUserContext
    {
        /// <summary>
        /// Gets the Current User Id which is setted from the Service layer for API-KEY api's
        /// </summary>
        /// <returns>Guid User Id</returns>
        Guid GetCurrentUserId();
        /// <summary>
        /// Sets the Current Used Id from the Service Layer for the API-KEY api's
        /// </summary>
        /// <param name="userId">User Id</param>
        void SetCurrentUserId(Guid userId);
    }
}