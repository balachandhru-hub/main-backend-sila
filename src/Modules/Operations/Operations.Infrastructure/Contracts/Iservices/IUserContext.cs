namespace Operations.Infrastructure.Contracts.IServices
{
    public interface IUserContext
    {
        /// <summary>
        /// Gets the current user id set by a background worker (there is no HTTP user there).
        /// </summary>
        /// <returns>Guid User Id</returns>
        Guid GetCurrentUserId();

        /// <summary>
        /// Sets the current user id for work that runs outside an HTTP request.
        /// </summary>
        /// <param name="userId">User Id</param>
        void SetCurrentUserId(Guid userId);
    }
}
