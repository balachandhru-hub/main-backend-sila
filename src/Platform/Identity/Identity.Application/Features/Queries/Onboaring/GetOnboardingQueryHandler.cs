using Contracts.IRepository;
using Identity.Domain.Dto;
using MediatR;
using SharedKernel.ExceptionHandler;
using Microsoft.AspNetCore.Http;
using SharedKernel.LoggerServices;

namespace Identity.Application.Features.Queries.Onboarding
{
    public class GetOnboardingQueryHandler
        : IRequestHandler<GetOnboardingQuery, OnboardingResponse>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILoggerManager _logger;

        public GetOnboardingQueryHandler(
            IRepositoryWrapper repository,
            IHttpContextAccessor httpContextAccessor,
            ILoggerManager logger)
        {
            _repository = repository;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

       public async Task<OnboardingResponse> Handle(GetOnboardingQuery request,CancellationToken cancellationToken)
        {
            _logger.LogInfo("Handling GetOnboardingQuery.");
            var organizationIdClaim = _httpContextAccessor.HttpContext?
                .User
                .FindFirst("OrganizationId")?.Value;

            if (string.IsNullOrWhiteSpace(organizationIdClaim))
            {
                _logger.LogError("Organization claim not found in the token.");
                throw new UnAuthorizedCustomException(
                    "Unauthorized",
                    "Organization claim not found.");
            }

            Guid organizationId = Guid.Parse(organizationIdClaim);

            _logger.LogInfo($"Fetching onboarding details for organization ID: {organizationId}.");
            var organization = _repository.Organization
                .FindByConditionAsync(x =>
                    x.Id == organizationId &&
                    x.IsActive)
                .FirstOrDefault();

            if (organization == null)
            {
                _logger.LogError($"Organization with ID {organizationId} not found or is inactive.");
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