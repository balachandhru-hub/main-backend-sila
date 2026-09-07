using Contracts.IRepository;
using Identity.Domain.Common;
using Identity.Domain.Dto;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Identity.Application.Features.Queries.GetOrganizationUserRFQ
{
    public class GetOrganizationUserQueryRFQHandler
        : IRequestHandler<GetOrganizationUserRFQQuery, List<UserListDto>>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetOrganizationUserQueryRFQHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<List<UserListDto>> Handle(
            GetOrganizationUserRFQQuery request,
            CancellationToken cancellationToken)
        {
            if (!request.OrganizationId.HasValue)
            {
                _logger.LogError("OrganizationId is required.");
                throw new BadRequestCustomException("OrganizationId is required.", "OrganizationId is required.");
            }
            _logger.LogInfo($"Fetching users for OrganizationId: {request.OrganizationId.Value}");
            Guid organizationId = request.OrganizationId.Value;

            var users =
            (

                from person in _repository.Person.FindByConditionAsync(x =>
                    x.OrganizationId == organizationId &&
                    x.IsActive)

                join user in _repository.User.FindByConditionAsync(x => x.IsActive)
                    on person.Id equals user.PersonId

                join mapping in _repository.UserRoleMapping.FindByConditionAsync(x => x.IsActive)
                    on user.Id equals mapping.UserId

                join role in _repository.Role.FindByConditionAsync(x => x.IsActive)
                    on mapping.RoleId equals role.Id

                select new UserListDto
                {
                    PersonId = person.Id,
                    UserId = user.Id,
                    Name = person.Name,
                    Email = person.Email,
                    UserName = user.UserName,
                    RoleId = role.Id,
                    RoleName = role.UserRole
                }

            ).ToList();

            _logger.LogInfo($"Found {users.Count} users for OrganizationId: {request.OrganizationId.Value}");
            return await Task.FromResult(users);
        }
    }
}