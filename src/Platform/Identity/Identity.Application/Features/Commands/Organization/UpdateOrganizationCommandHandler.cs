using Contracts.IRepository;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

// Alias to avoid namespace conflict
using OrganizationEntity = Identity.Domain.Entities.Organization;

namespace Identity.Application.Features.Commands.Organization
{
    public class UpdateOrganizationCommandHandler
        : IRequestHandler<UpdateOrganizationCommand, bool>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public UpdateOrganizationCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<bool> Handle(
            UpdateOrganizationCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo(
                $"Updating Organization : {request.Organization.OrganizationId}");

            OrganizationEntity? organization =
                _repository.Organization.FindFirstByCondition(x =>
                    x.Id == request.Organization.OrganizationId &&
                    x.IsActive);

            if (organization == null)
            {
                throw new NotFoundCustomException(
                    "Organization not found.",
                    $"Organization {request.Organization.OrganizationId} not found.");
            }

            organization.OrganizationName = request.Organization.OrganizationName;
            organization.Email = request.Organization.Email;
            organization.Phone = request.Organization.Phone;
            organization.Country = request.Organization.Country;
            organization.AddressLine1 = request.Organization.AddressLine1;
            organization.AddressLine2 = request.Organization.AddressLine2;
            organization.City = request.Organization.City;
            organization.State = request.Organization.State;
            organization.PinCode = request.Organization.PinCode;

            _repository.Organization.Update(organization);

            await _repository.SaveAsync();

            _logger.LogInfo(
                $"Organization updated successfully : {organization.Id}");

            return true;
        }
    }
}