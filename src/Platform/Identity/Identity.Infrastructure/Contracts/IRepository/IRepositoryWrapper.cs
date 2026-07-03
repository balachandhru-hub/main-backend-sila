namespace Contracts.IRepository
{
    /// <summary>
    /// Repository Wrapper class holding every instance of repository.
    /// </summary>
    public interface IRepositoryWrapper
    {
        IEmailVerificationRepository EmailVerification { get; }

      
        bool Save();
        Task<bool> SaveAsync();
    }
}