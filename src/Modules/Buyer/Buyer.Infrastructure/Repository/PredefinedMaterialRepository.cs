using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    /// <summary>
    /// Repository for PredefinedMaterial.
    /// </summary>
    public class PredefinedMaterialRepository
        : RepositoryBase<PredefinedMaterial>,
          IPredefinedMaterialRepository
    {
        public PredefinedMaterialRepository(
            RepositoryContext repositoryContext)
            : base(repositoryContext)
        {
        }
    }
}