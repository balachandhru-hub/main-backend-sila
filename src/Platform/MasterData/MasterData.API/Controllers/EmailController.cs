using MasterData.Application.Features.Email.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.LoggerServices;
using Swashbuckle.AspNetCore.Annotations;
using SharedKernel.ExceptionHandler;
using SharedKernel.Attributes;
using SharedKernel.Dto;

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
    [Route("api/v1/masterdata/email/send")]
    [ValidateModelState]
    [SwaggerOperation("SendEmail")]
    [SwaggerResponse(200, type: typeof(SuccessResponseDto), Description = "Email sent successfully")]
    [SwaggerResponse(400, type: typeof(ErrorResponseDto), Description = "Bad Request")]
    [SwaggerResponse(500, type: typeof(ErrorResponseDto), Description = "Internal Server Error")]
    public async Task<IActionResult> SendEmail(
    [FromBody] SendEmailCommand command)
    {
        _logger.LogInfo(
            $"Sending email to {command.ToEmail}");


        await _mediator.Send(command);


        return Ok(new SuccessResponseDto
        {
            StatusCode = 200,
            Message = "Email sent successfully",
            Description = "Email sent successfully"
        });
    }
}
