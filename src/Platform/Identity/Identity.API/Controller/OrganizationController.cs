using Identity.Application.Features.Commands.Organization;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Attributes;
using SharedKernel.Dto;
using SharedKernel.LoggerServices;
using Swashbuckle.AspNetCore.Annotations;

namespace Identity.API.Controllers
{
    [ApiController]
    public class OrganizationController : BaseController
    {
        private readonly IMediator _mediator;
        private readonly ILoggerManager _logger;

        public OrganizationController(
            IMediator mediator,
            ILoggerManager logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpPut]
        [Route("api/v1/identity/update-organization")]
        [ApiAuthorization(Name = "UPDATE_ORGANIZATION")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto))]
        [SwaggerResponse(401, type: typeof(ErrorResponseDto))]
        [SwaggerResponse(404, type: typeof(ErrorResponseDto))]
        public async Task<IActionResult> UpdateOrganization(
            [FromBody] UpdateOrganizationCommand command)
        {
            var result = await _mediator.Send(command);

            return Ok(result);
        }
    }
}