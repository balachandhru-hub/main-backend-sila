namespace MasterData.Infrastructure.Contracts.IServices;

public interface IUserIdentityService
{
    Guid GetCurrentUser();
}
