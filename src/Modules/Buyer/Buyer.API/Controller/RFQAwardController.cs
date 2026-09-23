using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using SharedKernel.LoggerServices;
using SharedKernel.Dto;
using SharedKernel.Attributes;
using SharedKernel.Controllers;
using Buyer.Domain.Dto;
using Buyer.Application.Features.Commands.SaveRFQAward;
using Buyer.Application.Features.Commands.UnawardRFQ;
using Buyer.Application.Features.Queries.GetRFQAward;

namespace Buyer.API.Controllers
{
    [ApiController]
    public class RFQAwardController : BaseController
    {
        private readonly IMediator _mediator;
        private readonly ILoggerManager _logger;

        public RFQAwardController(
            IMediator mediator,
            ILoggerManager logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpPost]
        [Route("api/v1/buyer/rfq-award")]
        [ValidateModelState]
        [ApiAuthorization(Name = "SAVE_RFQ_AWARD")]
        [SwaggerOperation("SaveRFQAward")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto), description: "RFQ awarded successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(404, type: typeof(ErrorResponseDto), description: "RFQ Not Found")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> SaveRFQAward(
            [FromBody] SaveRFQAwardDto request)
        {
            _logger.LogDebug($"Saving RFQ Award for RFQ Id: {request.RFQId}");

            Guid awardId = await _mediator.Send(
                new SaveRFQAwardCommand(request));

            _logger.LogDebug(
                $"RFQ Award saved successfully for RFQ Id: {request.RFQId}");

            return Ok(new SuccessResponseDto
            {
                Id = awardId.ToString(),
                StatusCode = 200,
                Message = "Success",
                Description = "RFQ awarded successfully."
            });
        }

        [HttpPut]
        [Route("api/v1/buyer/rfq-award/unaward")]
        [ValidateModelState]
        [ApiAuthorization(Name = "UNAWARD_RFQ")]
        [SwaggerOperation("UnawardRFQ")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto), description: "RFQ award cancelled successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(404, type: typeof(ErrorResponseDto), description: "RFQ award not found")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> UnawardRFQ(
            [FromBody] UnawardRFQDto request)
        {
            _logger.LogDebug($"Cancelling RFQ Award for RFQ Id: {request.RFQId}");

            await _mediator.Send(new UnawardRFQCommand(request));

            _logger.LogDebug(
                $"RFQ Award cancelled successfully for RFQ Id: {request.RFQId}");

            return Ok(new SuccessResponseDto
            {
                StatusCode = 200,
                Message = "Success",
                Description = "RFQ award cancelled. The RFQ is ready to be re-awarded."
            });
        }
    }
}
