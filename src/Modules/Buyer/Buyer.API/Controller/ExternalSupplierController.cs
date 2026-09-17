 using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using SharedKernel.LoggerServices;
using SharedKernel.Attributes;
using Buyer.API.Attributes;
using Buyer.Domain.Dto;
using SharedKernel.Controllers;
using Buyer.Application.Features.Queries.GetRFQAttachments;
using Buyer.Application.Features.Queries.GetRFQQuestions;
using SharedKernel.Dto;
using Buyer.Application.Features.Queries.GetCostCenterById;
using Buyer.Application.Features.Queries.Asset.GetDocument;
using Buyer.Application.Features.Queries.GetExternalSupplierName;



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
        [ExternalSessionAuthorization]
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
       [ExternalSessionAuthorization]
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
      [ExternalSessionAuthorization]
        [SwaggerOperation("GetCostCenterById")]
        [SwaggerResponse(200, type: typeof(CostCenterDto), description: "Success")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(404, type: typeof(ErrorResponseDto), description: "Cost Center Not Found")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> GetCostCenterById(
    Guid costCenterId,
    [FromQuery] Guid rfqId)
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
         [HttpGet]
        [Route("api/v1/buyer/externa-asset/{assetId}")]
        [ExternalSessionAuthorization]
        [SwaggerOperation("GetDocument")]
        [SwaggerResponse(statusCode: 200, "Fetched the File Details", typeof(AssetDownloadDto))]
        [SwaggerResponse(statusCode: 404, "Not Found", typeof(ErrorResponseDto))]
        [SwaggerResponse(statusCode: 401, "Unauthorized", typeof(ErrorResponseDto))]
       public async Task<IActionResult> GetDocument(
    [FromRoute] Guid assetId,
    [FromQuery] Guid rfqId)
{
    _logger.LogDebug($"Retrieving document for Asset Id: {assetId}");

    string sessionToken = Request.Headers[ExternalSessionAuthorizationAttribute.SessionTokenHeaderName]
        .FirstOrDefault() ?? string.Empty;

    var result = await _mediator.Send(new GetDocumentQuery(assetId, sessionToken, rfqId));

    _logger.LogDebug($"Document retrieved successfully for Asset Id: {assetId}");

    return Ok(result);
}

        [HttpGet]
        [Route("api/v1/buyer/external-supplier-name")]
        [ExternalSessionAuthorization]
        [SwaggerOperation("GetExternalSupplierName")]
        [SwaggerResponse(200, type: typeof(ExternalSupplierNameDto), description: "Success")]
        [SwaggerResponse(401, type: typeof(ErrorResponseDto), description: "Unauthorized")]
        [SwaggerResponse(404, type: typeof(ErrorResponseDto), description: "External supplier not found")]
        public async Task<IActionResult> GetExternalSupplierName([FromQuery] Guid rfqId)
        {
            var externalSupplierId = GetExternalSupplierId();

            _logger.LogDebug($"Fetching external supplier name for ExternalSupplierId: {externalSupplierId}");

            var result = await _mediator.Send(new GetExternalSupplierNameQuery(externalSupplierId));

            return Ok(result);
        }
    }
}