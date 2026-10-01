namespace Operations.Infrastructure.Contracts.IServices
{
    /// <summary>
    /// Interface <c>IUserIdentityService</c> contains the definition of user authentication methods.
    /// </summary>
    public interface IUserIdentityService
    {
        /// <summary>
        /// Gets the current user's ID from the HTTP context.
        /// </summary>
        /// <returns>The current user's ID as a GUID, or Guid.Empty if the user is not authenticated or the ID cannot be parsed.</returns>
        Guid GetCurrentUser();
    }
}
