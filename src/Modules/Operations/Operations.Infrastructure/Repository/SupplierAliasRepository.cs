using Operations.Domain.Entities;
using Operations.Infrastructure.Contracts.IRepository;
using Operations.Infrastructure.DbContext;

namespace Operations.Infrastructure.Repository
{
    public class SupplierAliasRepository : RepositoryBase<SupplierAlias>, ISupplierAliasRepository
    {
        public SupplierAliasRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }
    }
}
