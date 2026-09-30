using Contracts.IRepository;
using Identity.Domain.Dto;
using MediatR;
using SharedKernel.LoggerServices;

namespace Identity.Application.Features.Queries.GetUsersByIds
{
    public class GetUsersByIdsQueryHandler
        : IRequestHandler<GetUsersByIdsQuery, List<UserListDto>>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetUsersByIdsQueryHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<List<UserListDto>> Handle(
            GetUsersByIdsQuery request,
            CancellationToken cancellationToken)
        {
            if (request.UserIds == null || !request.UserIds.Any())
            {
                return new List<UserListDto>();
            }

            var userIds = request.UserIds.Distinct().ToList();

            _logger.LogInfo($"Fetching {userIds.Count} user(s) by id.");

            var users = (
                from user in _repository.User.FindByConditionAsync(x => userIds.Contains(x.Id) && x.IsActive)

                join person in _repository.Person.FindByConditionAsync(x => x.IsActive)
                    on user.PersonId equals person.Id

                join mapping in _repository.UserRoleMapping.FindByConditionAsync(x => x.IsActive)
                    on user.Id equals mapping.UserId into mappingGroup
                from mapping in mappingGroup.DefaultIfEmpty()

                join role in _repository.Role.FindByConditionAsync(x => x.IsActive)
                    on (mapping != null ? mapping.RoleId : Guid.Empty) equals role.Id into roleGroup
                from role in roleGroup.DefaultIfEmpty()

                select new UserListDto
                {
                    PersonId = person.Id,
                    UserId = user.Id,
                    Name = person.Name,
                    Email = person.Email,
                    UserName = user.UserName,
                    RoleId = role != null ? role.Id : Guid.Empty,
                    RoleName = role != null ? role.UserRole : null
                }

            ).ToList();

            _logger.LogInfo($"Found {users.Count} user(s) for the requested ids.");

            return await Task.FromResult(users);
        }
    }
}
