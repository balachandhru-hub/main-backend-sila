using Supplier.Domain.Entities;
using Supplier.Infrastructure.Contracts.IRepository;
using Supplier.Infrastructure.DbContext;

namespace Supplier.Infrastructure.Repository
{
    public class SupplierCatalogRepository
        : RepositoryBase<SupplierCatalog>,
          ISupplierCatalogRepository
    {
        public SupplierCatalogRepository(
            RepositoryContext repositoryContext)
            : base(repositoryContext)
        {
        }
    }
}   

