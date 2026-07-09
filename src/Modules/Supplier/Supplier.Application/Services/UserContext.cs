using Supplier.Infrastructure.Contracts.IServices;

namespace Supplier.Application.Services
{
public class UserContext : IUserContext
{
    private Guid _userId;

    public Guid GetCurrentUserId()
    {
        return _userId;
    }

    public void SetCurrentUserId(Guid userId)
    {
        _userId = userId;
    }
}
}
