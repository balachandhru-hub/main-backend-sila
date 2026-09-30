using Contracts.IRepository;
using Identity.Domain.Entities;
using Identity.Infrastructure.DbContext;

namespace Repository
{
    public class LoginRecordRepository : RepositoryBase<LoginRecord>, ILoginRecordRepository
    {
        private readonly RepositoryContext _context;

        public LoginRecordRepository(RepositoryContext context) : base(context)
        {
            _context = context;
        }
    }
}
