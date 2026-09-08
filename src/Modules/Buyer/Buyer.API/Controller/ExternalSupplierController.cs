 using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using SharedKernel.LoggerServices;
using SharedKernel.Attributes;
using Buyer.Domain.Dto;
using SharedKernel.Controllers;
using Buyer.Application.Features.Queries.GetRFQAttachments;
using Buyer.Application.Features.Queries.GetRFQQuestions;
using SharedKernel.Dto;
using Buyer.Application.Features.Queries.GetCostCenterById;



namespace Buyer.API.Controllers
{
    [ApiController]
    public class ExternalSupplierController : BaseController
    {
        private readonly IMediator _mediator;
        private readonly ILoggerManager _logger;

        public ExternalSupplierController(
            IMediator mediator,
            ILoggerManager logger)
        {
            _mediator = mediator;
            _logger = logger;
        }
 [HttpGet]
        [Route("api/v1/buyer/external-rfq-attachments")]
        [ValidateModelState]
        [ApiSessionAuthorization]
        [SwaggerOperation("GetRFQAttachments")]
        [SwaggerResponse(200, type: typeof(GetRFQAttachmentsDto), description: "Success")]
        public async Task<IActionResult> GetRFQAttachments([FromQuery] Guid rfqId)
        {
            var result = await _mediator.Send(new GetRFQAttachmentsQuery
            {
                RFQId = rfqId
            });

            return Ok(result);
        }
        [HttpGet]
        [Route("api/v1/buyer/external-internal-rfq-questions")]
        [ValidateModelState]
       [ApiSessionAuthorization]
        [SwaggerOperation("GetRFQQuestions")]
        [SwaggerResponse(200, type: typeof(List<RFQQuestionResponseDto>), description: "Fetched RFQ Questions successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad request")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> GetRFQQuestions(
            [FromQuery] Guid rfqId)
        {
            _logger.LogDebug($"Fetching RFQ Questions for RFQ Id: {rfqId}");

            var result = await _mediator.Send(
                new GetRFQQuestionsQuery(rfqId));

            _logger.LogDebug($"Fetched RFQ Questions successfully for RFQ Id: {rfqId}");

            return Ok(result);
        }
          [HttpGet]
        [Route("api/v1/buyer/external-cost-center/{costCenterId}")]
        [ValidateModelState]
      [ApiSessionAuthorization]
        [SwaggerOperation("GetCostCenterById")]
        [SwaggerResponse(200, type: typeof(CostCenterDto), description: "Success")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(404, type: typeof(ErrorResponseDto), description: "Cost Center Not Found")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> GetCostCenterById(
    Guid costCenterId)
        {
            _logger.LogDebug(
                $"Fetching Cost Center for CostCenterId: {costCenterId}");

            var result = await _mediator.Send(
                new GetCostCenterByIdQuery
                {
                    CostCenterId = costCenterId
                });

            _logger.LogDebug(
                $"Cost Center fetched successfully for CostCenterId: {costCenterId}");

            return Ok(result);
        }
    }
}