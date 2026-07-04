using MasterData.Application.Features.Email.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.LoggerServices;
using Dto;
using Swashbuckle.AspNetCore.Annotations; 
// using SharedKernel.Attributes;

namespace MasterData.API.Controllers;

[ApiController]
public class EmailController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILoggerManager _logger;

    public EmailController(IMediator mediator, ILoggerManager logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Transmits outbound email notifications using template processing.
    /// </summary>
    [HttpPost]
    [Route("api/v1/email/send")]
    // [ValidateModelState]
    [SwaggerOperation("SendEmail")]
    [SwaggerResponse(200, Description = "Email transaction completed successfully")]
    [SwaggerResponse(400, Description = "Bad Request")]
    [SwaggerResponse(500, Description = "Internal Server Error")]
    public async Task<IActionResult> SendEmail(
    [FromBody] SendEmailCommand command)
    {
        _logger.LogInfo(
            $"Sending email to {command.ToEmail}");

        bool result =
            await _mediator.Send(command);


        if (!result)
        {
            return BadRequest(
                new ErrorResponseDto
                {
                    StatusCode = 400,
                    Message = "Email sending failed",
                    Description = "Unable to send email"
                });
        }


        return Ok("Email sent successfully");
    }
}
