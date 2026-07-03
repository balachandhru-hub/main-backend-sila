using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using ExceptionHandler;
using Identity.Domain.Dtos;
using Identity.Application.Features.Auth.Commands.SendEmailVerification;
using SharedKernel.LoggerServices;
using Identity.Application.Features.Auth.Commands.VerifyOtp;
using System.ComponentModel.DataAnnotations;

namespace Identity.API.Controllers
{
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILoggerManager _logger;

        public AuthController(
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
        [Route("api/v1/auth/send-otp")]
      
        [SwaggerOperation("SendOtp")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto), description: "OTP generated successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad request")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> SendOtp([FromBody] SendEmailVerificationCommand command)
        {
            _logger.LogInfo($"Generating OTP for {command.Email}");

            command.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            bool result = await _mediator.Send(command);

            if (!result)
            {
                return BadRequest(new ErrorResponseDto
                {
                    StatusCode = 400,
                    Message = "Unable to generate OTP.",
                    Description = "OTP generation failed."
                });
            }

            return Ok(new SuccessResponseDto
            {
                StatusCode = 200,
                Message = "OTP generated successfully.",
                Description = "OTP generated successfully."
            });
        }
        [HttpPost]
        [Route("api/v1/auth/verify-otp")]
        [SwaggerOperation("VerifyOtp")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto), description: "OTP verified successfully.")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Invalid OTP or OTP expired.")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error.")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpCommand command)
        {
            var result = await _mediator.Send(command);

            if (!result.Success)
            {
                return BadRequest(new ErrorResponseDto
                {
                    StatusCode = 400,
                    Message = result.Message,
                    Description = result.Message
                });
            }

            return Ok(new SuccessResponseDto
            {
                StatusCode = 200,
                Message = result.Message,
                Description = "OTP verified successfully.",
                Id = result.TemporaryVerificationToken
            });
        }
    }
}