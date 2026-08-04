using Supplier.Domain.Entities;
using Supplier.Infrastructure.Contracts.IRepository;
using Supplier.Infrastructure.DbContext;
namespace Supplier.Infrastructure.Repository
{
public class SupplierVerificationAnswerOptionRepository
    : RepositoryBase<SupplierVerificationAnswerOption>,
      ISupplierVerificationAnswerOptionRepository
{
    public SupplierVerificationAnswerOptionRepository(RepositoryContext repositoryContext)
        : base(repositoryContext)
    {
    }
}
}


  