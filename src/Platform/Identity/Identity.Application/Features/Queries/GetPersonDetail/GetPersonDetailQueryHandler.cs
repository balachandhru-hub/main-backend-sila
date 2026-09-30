using Contracts.IRepository;
using Identity.Domain.Dto;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Identity.Application.Features.Queries.GetPersonDetail
{
    public class GetPersonDetailQueryHandler
        : IRequestHandler<GetPersonDetailQuery, PersonDetailDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetPersonDetailQueryHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<PersonDetailDto> Handle(
            GetPersonDetailQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching person details for PersonId: {request.PersonId}");

            var person =

                (
                    from p in _repository.Person.FindByConditionAsync(x =>
                        x.Id == request.PersonId &&
                        x.IsActive)

                    join u in _repository.User.FindByConditionAsync(x => x.IsActive)
                        on p.Id equals u.PersonId

                    join urm in _repository.UserRoleMapping.FindByConditionAsync(x => x.IsActive)
                        on u.Id equals urm.UserId

                    join r in _repository.Role.FindByConditionAsync(x => x.IsActive)
                        on urm.RoleId equals r.Id
                    
                    join o in _repository.Organization.FindByConditionAsync(x => x.IsActive)
                        on p.OrganizationId equals o.Id

                    select new PersonDetailDto
                    {
                        PersonId = p.Id,
                        UserId = u.Id,
                        OrganizationId = p.OrganizationId,
                        Name = p.Name,
                        Email = p.Email,
                        Phone = p.Phone,
                        UserName = u.UserName,
                        AddressLine = p.AddressLine,
                        Country = p.Country,
                        RoleId = r.Id,
                        RoleName = r.UserRole,
                        OrganizationName = o.OrganizationName,
                        OrganizationEmail = o.Email
                    }

                ).FirstOrDefault();

            if (person == null)
            {
                _logger.LogError($"Person not found. PersonId: {request.PersonId}");
                throw new BadRequestCustomException(
                    "Person not found.",
                    "Invalid PersonId.");
            }
            _logger.LogInfo($"Fetched person details for PersonId: {request.PersonId}");
            return await Task.FromResult(person);
        }
    }
}