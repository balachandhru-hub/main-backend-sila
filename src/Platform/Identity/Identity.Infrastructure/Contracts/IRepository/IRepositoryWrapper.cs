namespace Contracts.IRepository
{
    /// <summary>
    /// Repository Wrapper class holding every instance of repository.
    /// </summary>
    public interface IRepositoryWrapper
    {
        IEmailVerificationRepository EmailVerification { get; }
        IOrganizationRepository Organization { get; }
        IUserRepository User { get; }
        IPersonRepository Person { get; }

      
        bool Save();
        Task<bool> SaveAsync();
    }
}