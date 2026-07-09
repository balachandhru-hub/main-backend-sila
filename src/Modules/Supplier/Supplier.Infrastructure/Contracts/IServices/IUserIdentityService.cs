namespace Supplier.Infrastructure.Contracts.IServices;

public interface IUserIdentityService
{
    Guid GetCurrentUser();
}
