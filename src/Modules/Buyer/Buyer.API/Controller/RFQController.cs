using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using SharedKernel.LoggerServices;
using SharedKernel.Dto;
using SharedKernel.Attributes;
using Buyer.Domain.Dto;
using SharedKernel.Controllers;
using Buyer.Application.Features.Commands.CreateRFQ;
using Buyer.Domain.Dtos;
using Buyer.Application.Features.Queries.GetAllRFQ;
using Buyer.Application.Features.Queries.GetRFQAttachments;
using Buyer.Application.Features.Queries.GetRFQQuestions;

namespace Buyer.API.Controllers
{
    [ApiController]
    public class RFQController : BaseController
    {
        private readonly IMediator _mediator;
        private readonly ILoggerManager _logger;

        public RFQController(
            IMediator mediator,
            ILoggerManager logger)
        {
            _mediator = mediator;
            _logger = logger;
        }
        [HttpPost]
        [Route("api/v1/buyer/createrfq")]
        [ValidateModelState]
        [ApiAuthorization(Name = "CREATE_RFQ")]
        [SwaggerOperation("CreateRFQ")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto), description: "RFQ created successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        public async Task<IActionResult> CreateRFQ(
            [FromBody] CreateRFQDto rfq)
        {
            Guid organizationId = GetOrganizationId();
            _logger.LogDebug("Creating RFQ");

            Guid rfqId = await _mediator.Send(
        new CreateRFQCommand(organizationId, rfq));

            _logger.LogDebug("RFQ created successfully");

            return Ok(new SuccessResponseDto
            {
                Id = rfqId.ToString(),
                StatusCode = 200,
                Message = "Success",
                Description = "RFQ created successfully."
            });
        }
        [HttpPost]
        [Route("api/v1/buyer/rfq-master-data")]
        [ValidateModelState]
        [ApiAuthorization(Name = "GET_RFQ_MASTER_DATA")]
        [SwaggerOperation("GetRFQList")]
        [SwaggerResponse(200, type: typeof(List<RFQListDto>), description: "Success")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> GetRFQList(
     [FromBody] GetRFQListQuery query)
        {
            var result = await _mediator.Send(query);

            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/buyer/rfq-by-id")]
        [ValidateModelState]
        [ApiAuthorization(Name = "GET_RFQ_BY_ID")]
        [SwaggerOperation("GetRFQById")]
        [SwaggerResponse(200, type: typeof(GetRFQByIdDto), description: "Success")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> GetRFQById([FromQuery] Guid rfqId)
        {
            var result = await _mediator.Send(new GetRFQByIdQuery
            {
                RFQId = rfqId
            });

            return Ok(result);
        }
        [HttpGet]
        [Route("api/v1/buyer/rfq-attachments")]
        [ValidateModelState]
        [ApiAuthorization(Name = "GET_RFQ_ATTACHMENTS")]
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
        [Route("api/v1/buyer/internal-rfq-questions")]
        [ValidateModelState]
        [ApiAuthorization(Name = "GET_RFQ_QUESTIONS")]
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

    }
}