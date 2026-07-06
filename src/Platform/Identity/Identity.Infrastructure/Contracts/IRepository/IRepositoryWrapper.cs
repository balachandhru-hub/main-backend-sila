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
        IUserRoleMappingRepository UserRoleMapping { get; }
        IRoleRepository Role { get; }
        IAccessTokenRepository AccessToken { get; }
        IRefreshTokenRepository RefreshToken { get; }
        ILoginRecordRepository LoginRecord { get; }

      
        bool Save();
        Task<bool> SaveAsync();
    }
}