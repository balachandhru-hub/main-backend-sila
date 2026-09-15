using Contracts.IRepository;
using MediatR;

namespace Identity.Application.Features.Queries.Permissions
{
    /// <summary>
    /// Reads the feature keys granted to a role. Identity owns the role/feature
    /// tables, so this is the single source every service's
    /// ApiAuthorizationAttribute loads permissions from.
    /// </summary>
    public class GetPermissionsQueryHandler : IRequestHandler<GetPermissionsQuery, List<string>>
    {
        private readonly IRepositoryWrapper _repository;

        public GetPermissionsQueryHandler(IRepositoryWrapper repository)
        {
            _repository = repository;
        }

        public Task<List<string>> Handle(
            GetPermissionsQuery request,
            CancellationToken cancellationToken)
        {
            List<string> permissions = (
                from roleFeature in _repository.RoleFeatureMapping.FindByConditionAsync(rf => rf.IsActive)
                join feature in _repository.Feature.FindByConditionAsync(f => f.IsActive)
                    on roleFeature.FeatureId equals feature.Id
                where roleFeature.RoleId == request.RoleId
                        && roleFeature.IsActive
                        && feature.IsActive
                select feature.Key
            ).ToList();

            return Task.FromResult(permissions);
        }
    }
}
