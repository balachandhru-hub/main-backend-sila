using Supplier.Domain.Entities;
using Supplier.Infrastructure.Contracts.IRepository;
using Supplier.Infrastructure.DbContext;
namespace Supplier.Infrastructure.Repository
{
public class RFQQuestionAnswerOptionRepository
    : RepositoryBase<SupplierRFQAnswerOption >,
      IRFQQuestionAnswerOptionRepository
{
    public RFQQuestionAnswerOptionRepository(RepositoryContext repositoryContext)
        : base(repositoryContext)
    {
    }
}
}


  