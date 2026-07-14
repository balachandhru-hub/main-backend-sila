using Contracts.IRepository;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;




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

           var organization = _repository.Organization.FindFirstByCondition(x =>
                x.Id == request.Organization.OrganizationId &&
                x.IsActive);
            if (organization == null)
            {
                _logger.LogError(
                    $"Organization not found : {request.Organization.OrganizationId}");
                throw new NotFoundCustomException(
                    "Organization not found.",
                    $"Organization {request.Organization.OrganizationId} not found.");
            }

            if (organization.OrganizationName != request.Organization.OrganizationName)
            {
                _logger.LogInfo(
                    $"Updating Organization Name from {organization.OrganizationName} to {request.Organization.OrganizationName}");
                organization.OrganizationName = request.Organization.OrganizationName;
            }

            if (organization.Email != request.Organization.Email)
            {
                _logger.LogInfo(
                    $"Updating Organization Email from {organization.Email} to {request.Organization.Email}");
                organization.Email = request.Organization.Email;
            }

            if (organization.Phone != request.Organization.Phone)
            {
                _logger.LogInfo(
                    $"Updating Organization Phone from {organization.Phone} to {request.Organization.Phone}");
                organization.Phone = request.Organization.Phone;
            }

            if (organization.Country != request.Organization.Country)
            {
                _logger.LogInfo(
                    $"Updating Organization Country from {organization.Country} to {request.Organization.Country}");
                organization.Country = request.Organization.Country;
            }

            if (organization.AddressLine1 != request.Organization.AddressLine1)
            {
                _logger.LogInfo(
                    $"Updating Organization AddressLine1 from {organization.AddressLine1} to {request.Organization.AddressLine1}");
                organization.AddressLine1 = request.Organization.AddressLine1;
            }

            if (organization.AddressLine2 != request.Organization.AddressLine2)
            {
                _logger.LogInfo(
                    $"Updating Organization AddressLine2 from {organization.AddressLine2} to {request.Organization.AddressLine2}");
                organization.AddressLine2 = request.Organization.AddressLine2;
            }

            if (organization.City != request.Organization.City)
            {
                _logger.LogInfo(
                    $"Updating Organization City from {organization.City} to {request.Organization.City}");
                organization.City = request.Organization.City;
            }

            if (organization.State != request.Organization.State)
            {
                _logger.LogInfo(
                    $"Updating Organization State from {organization.State} to {request.Organization.State}");
                organization.State = request.Organization.State;
            }

            if (organization.PinCode != request.Organization.PinCode)
            {
                _logger.LogInfo(
                    $"Updating Organization PinCode from {organization.PinCode} to {request.Organization.PinCode}");
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