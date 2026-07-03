using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using SharedKernel.ExceptionHandler;
using Identity.Application.Features.Commands.RegisterOrganization;
using SharedKernel.LoggerServices;
using Identity.Application.Features.Auth.Commands.VerifyOtp;
using System.ComponentModel.DataAnnotations;
using Dto;
using SharedKernel.Attributes;


namespace Identity.API.Controllers
{
    [ApiController]
    public class OrganizationController : ControllerBase
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

        /// <summary>
        /// Generates OTP for email verification.
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/v1/organizations/create")]
        [ValidateModelState]
        [SwaggerOperation("createOrganization")]
        [SwaggerResponse(200, type: typeof(Dto.SuccessResponseDto), description: "Organization created successfully")]
        [SwaggerResponse(400, type: typeof(Dto.ErrorResponseDto), description: "Bad request")]
        [SwaggerResponse(500, type: typeof(Dto.ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> CreateOrganization([FromBody] RegisterOrganizationCommand command)
        {
            _logger.LogInfo($"Creating organization for {command.Email}");
            var verificationToken = Request.Cookies["VerificationToken"];

Console.WriteLine($"COOKIE TOKEN: {verificationToken}");

command.VerificationToken = verificationToken;

              var organizationId = await _mediator.Send(command);

            return Ok(new
            {
                Message = "Organization registered successfully.",
                OrganizationId = organizationId
            });
        }
       
    }
}