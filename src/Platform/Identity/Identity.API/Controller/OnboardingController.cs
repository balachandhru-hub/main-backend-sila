using Identity.Application.Features.Queries.Onboarding;
using Identity.Domain.Dto;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Attributes;
using SharedKernel.Dto;
using SharedKernel.LoggerServices;
using Swashbuckle.AspNetCore.Annotations;


namespace Identity.API.Controllers
{
    [ApiController]
    public class OnboardingController : BaseController
    {
        private readonly IMediator _mediator;
        private readonly ILoggerManager _logger;

        public OnboardingController(
            IMediator mediator,
            ILoggerManager logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpGet]
        [Route("api/v1/onboarding")]
        [ApiAuthorization(Name = "GET_ONBOARDING")]
        [SwaggerResponse(200, type: typeof(OnboardingResponse), description: "Onboarding details fetched successfully.")]
        [SwaggerResponse(401, type: typeof(ErrorResponseDto), description: "Unauthorized")]
        [SwaggerResponse(404, type: typeof(ErrorResponseDto), description: "Onboarding details not found")]
        public async Task<IActionResult> GetOnboarding()
        {
            _logger.LogInfo("Fetching onboarding details.");

            Guid organizationId = GetOrganizationId();

            var result = await _mediator.Send(new GetOnboardingQuery
            {
                OrganizationId = organizationId
            });

            _logger.LogInfo("Onboarding details fetched successfully.");

            return Ok(result);
        }
    }
}