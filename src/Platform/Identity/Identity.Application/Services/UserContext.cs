using Identity.Infrastructure.Contracts.IServices;

namespace Identity.Application.Services
{
    /// <summary>
    /// User Context
    /// </summary>
    public class UserContext : IUserContext
    {
        private Guid _userId;

        /// <summary>
        /// Gets the Current User Id which is setted from the Service layer for API-KEY api's
        /// </summary>
        /// <returns>Guid User Id</returns>
        public Guid GetCurrentUserId()
        {
            return _userId;
        }
        
        /// <summary>
        /// Sets the Current Used Id from the Service Layer for the API-KEY api's
        /// </summary>
        /// <param name="userId">User Id</param>
        public void SetCurrentUserId(Guid userId)
        {
            _userId = userId;
        }
    }
}