using Identity.Application.Features.Commands.Organization;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Attributes;
using SharedKernel.Dto;
using SharedKernel.LoggerServices;
using Swashbuckle.AspNetCore.Annotations;
using Identity.Application.Features.Commands.OrganizationStatus;

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
           [HttpPut]
            [Route("api/v1/organization/enable-disable")]
            [ValidateModelState]
            [ApiAuthorization(Name = "ENABLE_DISABLE_ORGANIZATION")]
            [SwaggerOperation("EnableDisableOrganization")]
            [SwaggerResponse(200, type: typeof(SuccessResponseDto), description: "Organization updated successfully")]
            [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
            [SwaggerResponse(404, type: typeof(ErrorResponseDto), description: "Organization not found")]
            public async Task<IActionResult> EnableDisableOrganization(
                [FromBody] EnableDisableOrganizationCommand command)
            {
                command.OrganizationId = GetOrganizationId();

                _logger.LogInfo($"Updating organization status : {command.OrganizationId}");

                await _mediator.Send(command);

                _logger.LogInfo($"Organization updated successfully : {command.OrganizationId}");

                return Ok(new SuccessResponseDto
                {
                    StatusCode = 200,
                    Message = "Success",
                    Description = "Organization updated successfully.",
                    Id = command.OrganizationId.ToString()
                });
            }
    }
}