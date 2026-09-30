using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    /// <summary>
    /// Repository for ExcelMaterialMaster.
    /// </summary>
    public class ExcelMaterialMasterRepository
        : RepositoryBase<ExcelMaterialMaster>,
          IExcelMaterialMasterRepository
    {
        public ExcelMaterialMasterRepository(
            RepositoryContext repositoryContext)
            : base(repositoryContext)
        {
        }
    }
}
