using Contracts.IRepository;
using Identity.Domain.Common;
using Identity.Domain.Dto;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Identity.Application.Features.Queries.GetOrganizationUser
{
    public class GetOrganizationUserQueryHandler
        : IRequestHandler<GetOrganizationUserQuery, List<UserListDto>>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetOrganizationUserQueryHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<List<UserListDto>> Handle(
            GetOrganizationUserQuery request,
            CancellationToken cancellationToken)
        {
            if (!request.OrganizationId.HasValue)
            {
                _logger.LogError("OrganizationId is required.");
                throw new BadRequestCustomException("OrganizationId is required.", "OrganizationId is required.");
            }
            _logger.LogInfo($"Fetching users for OrganizationId: {request.OrganizationId.Value} with LoggedInRoleId: {request.LoggedInRole}");
            Guid organizationId = request.OrganizationId.Value;
            _logger.LogInfo($"OrganizationId: {organizationId}, LoggedInRoleId: {request.LoggedInRole}");
            var loggedInRole = (
                from role in _repository.Role.FindByConditionAsync(x => x.IsActive)
                where role.Id == Guid.Parse(request.LoggedInRole)
                select role.UserRole
            ).FirstOrDefault();

            if (string.IsNullOrEmpty(loggedInRole))
            {
                _logger.LogError($"LoggedInRoleId {request.LoggedInRole} is invalid.");
                throw new BadRequestCustomException("Invalid LoggedInRoleId.", "The provided LoggedInRoleId does not correspond to any active role.");
            }

            IQueryable<string> allowedRoles;

            if (loggedInRole == Common.PLATFORM_ADMINISTRATOR)
            {
                _logger.LogInfo("LoggedInRole is PLATFORM_ADMINISTRATOR. Fetching all roles.");
                allowedRoles = new[]
                {

                    Common.BUYER_NETWORK_ADMIN,
                    Common.SUPPLIER_NETWORK_ADMIN,
                    Common.BUYER_ADMINISTRATOR,
                    Common.SUPPLIER_ADMINISTRATOR,
                    Common.BUYER_USER,
                    Common.SUPPLIER_USER
                }.AsQueryable();
            }
            else if (loggedInRole == Common.BUYER_NETWORK_ADMIN)
            {
                _logger.LogInfo("LoggedInRole is BUYER_NETWORK_ADMIN. Fetching roles for buyer organization.");
                allowedRoles = new[]
                {
                    Common.BUYER_ADMINISTRATOR,
                    Common.BUYER_USER
                }.AsQueryable();
            }
            else if (loggedInRole == Common.BUYER_ADMINISTRATOR)
            {
                _logger.LogInfo("LoggedInRole is BUYER_ADMINISTRATOR. Fetching roles for buyer organization.");
                allowedRoles = new[]
                {
                    Common.BUYER_USER
                }.AsQueryable();
            }
            else if (loggedInRole == Common.SUPPLIER_NETWORK_ADMIN)
            {
                _logger.LogInfo("LoggedInRole is SUPPLIER_NETWORK_ADMIN. Fetching roles for supplier organization.");
                allowedRoles = new[]
                {
                    Common.SUPPLIER_ADMINISTRATOR,
                    Common.SUPPLIER_USER
                }.AsQueryable();
            }
            else if (loggedInRole == Common.SUPPLIER_ADMINISTRATOR)
            {
                _logger.LogInfo("LoggedInRole is SUPPLIER_ADMINISTRATOR. Fetching roles for supplier organization.");
                allowedRoles = new[]
                {
                    Common.SUPPLIER_USER
                }.AsQueryable();
            }
            else
            {
                _logger.LogInfo("Unsupported LoggedInRole.");
                throw new NotFoundCustomException("Unsupported LoggedInRole.", "The provided LoggedInRole is not supported for fetching users.");
            }

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

                where allowedRoles.Contains(role.UserRole)

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

            _logger.LogInfo($"Found {users.Count} users for OrganizationId: {request.OrganizationId.Value} with LoggedInRoleId: {request.LoggedInRole}");
            return await Task.FromResult(users);
        }
    }
}