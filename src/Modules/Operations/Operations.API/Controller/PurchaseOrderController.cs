using Operations.Application.Features.Queries.GetPurchaseOrder;
using Operations.Application.Features.Queries.SearchPurchaseOrders;
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
    /// Purchase orders imported from the ERP: lookup by number and search.
    /// </summary>
    [ApiController]
    public class PurchaseOrderController : BaseController
    {
        private readonly IMediator _mediator;
        private readonly ILoggerManager _logger;

        public PurchaseOrderController(IMediator mediator, ILoggerManager logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpGet]
        [Route("api/v1/operations/purchase-orders/search")]
        [ApiAuthorization(Name = "OPERATIONS_VIEW_PURCHASE_ORDER")]
        [SwaggerOperation("SearchPurchaseOrders")]
        [SwaggerResponse(200, type: typeof(List<PurchaseOrderResponseDto>))]
        public async Task<IActionResult> SearchPurchaseOrders([FromQuery] PurchaseOrderSearchRequestDto request)
        {
            _logger.LogDebug($"Searching purchase orders. Query: {request.Query}, OpenOnly: {request.OpenOnly}");
            List<PurchaseOrderResponseDto> result = await _mediator.Send(new SearchPurchaseOrdersQuery
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                Request = request
            });
            _logger.LogDebug($"Purchase orders found. Count: {result.Count}");
            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/operations/purchase-orders/{poNumber}")]
        [ApiAuthorization(Name = "OPERATIONS_VIEW_PURCHASE_ORDER")]
        [SwaggerOperation("GetPurchaseOrder")]
        [SwaggerResponse(200, type: typeof(PurchaseOrderResponseDto))]
        public async Task<IActionResult> GetPurchaseOrder([FromRoute] string poNumber, [FromQuery] string? entityCode)
        {
            _logger.LogDebug($"Fetching purchase order. PoNumber: {poNumber}, EntityCode: {entityCode}");
            PurchaseOrderResponseDto result = await _mediator.Send(new GetPurchaseOrderQuery
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                PoNumber = poNumber,
                EntityCode = entityCode
            });
            _logger.LogDebug($"Purchase order fetched. PurchaseOrderId: {result.Id}");
            return Ok(result);
        }
    }
}
