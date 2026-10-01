using Operations.Infrastructure.Contracts.IServices;

namespace Operations.Application.Services
{
    /// <summary>
    /// User Context
    /// </summary>
    public class UserContext : IUserContext
    {
        private Guid _userId;

        /// <summary>
        /// Gets the current user id set by a background worker (there is no HTTP user there).
        /// </summary>
        /// <returns>Guid User Id</returns>
        public Guid GetCurrentUserId()
        {
            return _userId;
        }

        /// <summary>
        /// Sets the current user id for work that runs outside an HTTP request.
        /// </summary>
        /// <param name="userId">User Id</param>
        public void SetCurrentUserId(Guid userId)
        {
            _userId = userId;
        }
    }
}
