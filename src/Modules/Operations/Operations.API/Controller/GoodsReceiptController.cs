using Operations.Application.Features.Commands.PostGoodsReceipt;
using Operations.Application.Features.Commands.RetryGoodsReceipt;
using Operations.Application.Features.Queries.GetGoodsReceipt;
using Operations.Application.Features.Queries.GetGoodsReceiptByNumber;
using Operations.Application.Features.Queries.GetGoodsReceipts;
using Operations.Application.Features.Queries.ValidateGoodsReceipt;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Operations.Domain.Dtos;
using Operations.Domain.Enums;
using SharedKernel.Attributes;
using SharedKernel.Controllers;
using SharedKernel.Dto;
using SharedKernel.LoggerServices;
using Swashbuckle.AspNetCore.Annotations;

namespace Operations.API.Controllers
{
    /// <summary>
    /// Goods receipts (GRN): validate, post, retry and read.
    /// </summary>
    [ApiController]
    public class GoodsReceiptController : BaseController
    {
        private readonly IMediator _mediator;
        private readonly ILoggerManager _logger;

        public GoodsReceiptController(IMediator mediator, ILoggerManager logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpGet]
        [Route("api/v1/operations/grns")]
        [ApiAuthorization(Name = "OPERATIONS_VIEW_GRN")]
        [SwaggerOperation("GetGoodsReceipts")]
        [SwaggerResponse(200, type: typeof(List<GoodsReceiptResponseDto>))]
        public async Task<IActionResult> GetGoodsReceipts()
        {
            _logger.LogDebug($"Fetching goods receipts.");
            List<GoodsReceiptResponseDto> result = await _mediator.Send(new GetGoodsReceiptsQuery
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId()
            });
            _logger.LogDebug($"Goods receipts fetched. Count: {result.Count}");
            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/operations/grns/{goodsReceiptId:guid}")]
        [ApiAuthorization(Name = "OPERATIONS_VIEW_GRN")]
        [SwaggerOperation("GetGoodsReceipt")]
        [SwaggerResponse(200, type: typeof(GoodsReceiptResponseDto))]
        public async Task<IActionResult> GetGoodsReceipt([FromRoute] Guid goodsReceiptId)
        {
            _logger.LogDebug($"Fetching goods receipt. GoodsReceiptId: {goodsReceiptId}");
            GoodsReceiptResponseDto result = await _mediator.Send(new GetGoodsReceiptQuery
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                GoodsReceiptId = goodsReceiptId
            });
            _logger.LogDebug($"Goods receipt fetched. GoodsReceiptId: {result.Id}");
            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/operations/grns/by-number/{grnNumber}")]
        [ApiAuthorization(Name = "OPERATIONS_VIEW_GRN")]
        [SwaggerOperation("GetGoodsReceiptByNumber")]
        [SwaggerResponse(200, type: typeof(GoodsReceiptResponseDto))]
        public async Task<IActionResult> GetGoodsReceiptByNumber([FromRoute] string grnNumber)
        {
            _logger.LogDebug($"Fetching goods receipt by number. GrnNumber: {grnNumber}");
            GoodsReceiptResponseDto result = await _mediator.Send(new GetGoodsReceiptByNumberQuery
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                GrnNumber = grnNumber
            });
            _logger.LogDebug($"Goods receipt fetched. GoodsReceiptId: {result.Id}");
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/operations/grns/validate")]
        [ApiAuthorization(Name = "OPERATIONS_POST_GRN")]
        [SwaggerOperation("ValidateGoodsReceipt")]
        [SwaggerResponse(200, type: typeof(GrnValidationResponseDto))]
        public async Task<IActionResult> ValidateGoodsReceipt([FromBody] ValidateGrnRequestDto request)
        {
            _logger.LogDebug($"Validating goods receipt. InvoiceId: {request.InvoiceId}, PurchaseOrderId: {request.PurchaseOrderId}");
            GrnValidationResponseDto result = await _mediator.Send(new ValidateGoodsReceiptQuery
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                Request = request
            });
            _logger.LogDebug($"Goods receipt validated. Valid: {result.Valid}");
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/operations/grns")]
        [ApiAuthorization(Name = "OPERATIONS_POST_GRN")]
        [SwaggerOperation("PostGoodsReceipt")]
        [SwaggerResponse(200, type: typeof(GoodsReceiptResponseDto))]
        public async Task<IActionResult> PostGoodsReceipt([FromBody] PostGrnRequestDto request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
        {
            _logger.LogDebug($"Posting goods receipt. InvoiceId: {request.InvoiceId}, PurchaseOrderId: {request.PurchaseOrderId}");
            GoodsReceiptResponseDto result = await _mediator.Send(new PostGoodsReceiptCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                Request = request,
                IdempotencyKey = idempotencyKey
            });
            _logger.LogDebug($"Goods receipt handled. GoodsReceiptId: {result.Id}, Status: {result.Status}");
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/operations/grns/{goodsReceiptId:guid}/retry")]
        [ApiAuthorization(Name = "OPERATIONS_POST_GRN")]
        [SwaggerOperation("RetryGoodsReceipt")]
        [SwaggerResponse(200, type: typeof(GoodsReceiptResponseDto))]
        public async Task<IActionResult> RetryGoodsReceipt([FromRoute] Guid goodsReceiptId)
        {
            _logger.LogDebug($"Retrying goods receipt. GoodsReceiptId: {goodsReceiptId}");
            GoodsReceiptResponseDto result = await _mediator.Send(new RetryGoodsReceiptCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                GoodsReceiptId = goodsReceiptId
            });
            _logger.LogDebug($"Goods receipt retried. GoodsReceiptId: {result.Id}, Status: {result.Status}");
            return Ok(result);
        }
    }
}
