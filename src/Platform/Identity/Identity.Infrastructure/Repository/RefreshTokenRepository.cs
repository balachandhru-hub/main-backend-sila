using Contracts.IRepository;
using Identity.Domain.Entities;
using Identity.Infrastructure.DbContext;

namespace Repository
{
    public class RefreshTokenRepository : RepositoryBase<RefreshToken>, IRefreshTokenRepository
    {
        private readonly RepositoryContext _context;

        public RefreshTokenRepository(RepositoryContext context) : base(context)
        {
            _context = context;
        }
    }
}
