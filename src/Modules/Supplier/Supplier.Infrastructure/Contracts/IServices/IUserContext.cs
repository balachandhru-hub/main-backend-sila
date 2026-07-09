namespace Supplier.Infrastructure.Contracts.IServices;

public interface IUserContext
{
    Guid GetCurrentUserId();
    void SetCurrentUserId(Guid userId);
}
