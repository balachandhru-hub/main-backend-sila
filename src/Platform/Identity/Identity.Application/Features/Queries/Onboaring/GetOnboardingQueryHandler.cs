using Contracts.IRepository;
using Identity.Domain.Dto;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Identity.Application.Features.Queries.Onboarding
{
    public class GetOnboardingQueryHandler
        : IRequestHandler<GetOnboardingQuery, OnboardingResponse>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetOnboardingQueryHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<OnboardingResponse> Handle(
            GetOnboardingQuery request,
            CancellationToken cancellationToken)
        {
            var organization = _repository.Organization
                .FindByConditionAsync(x =>
                    x.Id == request.OrganizationId &&
                    x.IsActive)
                .FirstOrDefault();

            if (organization == null)
            {
                _logger.LogError($"Organization with ID {request.OrganizationId} not found.");

                throw new NotFoundCustomException(
                    "Organization not found",
                    "Organization not found.");
            }

            return new OnboardingResponse
            {
                Id = organization.Id,
                OrganizationName = organization.OrganizationName,
                OrganizationType = organization.OrganizationType.ToString(),
                Email = organization.Email,
                Phone = organization.Phone,
                Country = organization.Country,
                EmailVerified = organization.EmailVerified,
                AddressLine1 = organization.AddressLine1,
                AddressLine2 = organization.AddressLine2,
                City = organization.City,
                State = organization.State,
                PinCode = organization.PinCode
            };
        }
    }
}