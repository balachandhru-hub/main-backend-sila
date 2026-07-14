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

            if (organization.OrganizationName != request.Organization.OrganizationName)
            {
                organization.OrganizationName = request.Organization.OrganizationName;
            }

            if (organization.Email != request.Organization.Email)
            {
                organization.Email = request.Organization.Email;
            }

            if (organization.Phone != request.Organization.Phone)
            {
                organization.Phone = request.Organization.Phone;
            }

            if (organization.Country != request.Organization.Country)
            {
                organization.Country = request.Organization.Country;
            }

            if (organization.AddressLine1 != request.Organization.AddressLine1)
            {
                organization.AddressLine1 = request.Organization.AddressLine1;
            }

            if (organization.AddressLine2 != request.Organization.AddressLine2)
            {
                organization.AddressLine2 = request.Organization.AddressLine2;
            }

            if (organization.City != request.Organization.City)
            {
                organization.City = request.Organization.City;
            }

            if (organization.State != request.Organization.State)
            {
                organization.State = request.Organization.State;
            }

            if (organization.PinCode != request.Organization.PinCode)
            {
                organization.PinCode = request.Organization.PinCode;
            }

            _repository.Organization.Update(organization);

            await _repository.SaveAsync();

            _logger.LogInfo(
                $"Organization updated successfully : {organization.Id}");

            return true;
        }
    }
}