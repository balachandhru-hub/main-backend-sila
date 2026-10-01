using Operations.Domain.Entities;
using Operations.Infrastructure.Contracts.IRepository;
using Operations.Infrastructure.DbContext;

namespace Operations.Infrastructure.Repository
{
    public class MicrosoftAuthorizationStateRepository : RepositoryBase<MicrosoftAuthorizationState>, IMicrosoftAuthorizationStateRepository
    {
        public MicrosoftAuthorizationStateRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }
    }
}
