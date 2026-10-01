using Operations.Domain.Entities;
using Operations.Infrastructure.Contracts.IRepository;
using Operations.Infrastructure.DbContext;

namespace Operations.Infrastructure.Repository
{
    public class InvoiceOcrConfigurationRepository : RepositoryBase<InvoiceOcrConfiguration>, IInvoiceOcrConfigurationRepository
    {
        public InvoiceOcrConfigurationRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }
    }
}
