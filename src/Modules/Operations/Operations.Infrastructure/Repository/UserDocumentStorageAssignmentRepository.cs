using Operations.Domain.Entities;
using Operations.Infrastructure.Contracts.IRepository;
using Operations.Infrastructure.DbContext;

namespace Operations.Infrastructure.Repository
{
    public class UserDocumentStorageAssignmentRepository : RepositoryBase<UserDocumentStorageAssignment>, IUserDocumentStorageAssignmentRepository
    {
        public UserDocumentStorageAssignmentRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }
    }
}
