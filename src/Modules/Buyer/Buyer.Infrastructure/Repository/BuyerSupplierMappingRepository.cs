using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class BuyerSupplierMappingRepository
        : RepositoryBase<BuyerSupplierMapping>,
          IBuyerSupplierMappingRepository
    {
        public BuyerSupplierMappingRepository(
            RepositoryContext repositoryContext)
            : base(repositoryContext)
        {
        }
    }
}