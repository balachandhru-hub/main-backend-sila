using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;
namespace Buyer.Infrastructure.Repository
{
    public class SupplierVerificationRequestRepository
        : RepositoryBase<SupplierVerificationRequest>,
          ISupplierVerificationRequestRepository
    {
         public SupplierVerificationRequestRepository(RepositoryContext repositoryContext)
            : base(repositoryContext)
        {

        }
    }
}