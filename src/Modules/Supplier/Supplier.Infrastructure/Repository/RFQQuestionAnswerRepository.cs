using Supplier.Domain.Entities;
using Supplier.Infrastructure.Contracts.IRepository;
using Supplier.Infrastructure.DbContext;
namespace Supplier.Infrastructure.Repository
{
public class RFQQuestionAnswerRepository
    : RepositoryBase<SupplierRFQQuestionAnswer>,
      IRFQQuestionAnswerRepository
{
    public RFQQuestionAnswerRepository(RepositoryContext repositoryContext)
        : base(repositoryContext)
    {
    }
}
}


  