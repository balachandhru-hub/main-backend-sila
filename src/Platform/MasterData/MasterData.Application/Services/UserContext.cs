using MasterData.Infrastructure.Contracts.IServices;

namespace MasterData.Application.Services;

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
