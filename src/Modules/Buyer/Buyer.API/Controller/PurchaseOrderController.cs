using Buyer.Application.Features.Queries.GetPurchaseOrders;
using Buyer.Application.Features.Queries.GetSupplierPurchaseOrders;
using Buyer.Domain.Dtos;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Attributes;
using SharedKernel.Controllers;
using SharedKernel.LoggerServices;
using Swashbuckle.AspNetCore.Annotations;

namespace Buyer.API.Controllers
{
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
        [Route("api/v1/buyer/purchase-orders")]
        [ApiAuthorization(Name = "GET_PURCHASE_ORDER")]
        [SwaggerOperation("GetPurchaseOrders")]
        [SwaggerResponse(200, type: typeof(List<PurchaseOrderListItemDto>))]
        public async Task<IActionResult> List([FromQuery] int index = 0, [FromQuery] int limit = 20)
        {
            _logger.LogDebug($"Fetching purchase orders. Index: {index}, Limit: {limit}");
            List<PurchaseOrderListItemDto> result = await _mediator.Send(new GetPurchaseOrdersQuery
            {
                OrganizationId = GetOrganizationId(),
                Index = index,
                Limit = limit
            });
            _logger.LogDebug($"Purchase orders fetched. Count: {result.Count}");
            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/buyer/purchase-orders/supplier")]
        [ApiAuthorization(Name = "GET_SUPPLIER_PURCHASE_ORDER")]
        [SwaggerOperation("GetSupplierPurchaseOrders")]
        [SwaggerResponse(200, type: typeof(List<PurchaseOrderListItemDto>))]
        public async Task<IActionResult> ListForSupplier([FromQuery] int index = 0, [FromQuery] int limit = 20)
        {
            _logger.LogDebug($"Fetching purchase orders of the supplier. Index: {index}, Limit: {limit}");
            List<PurchaseOrderListItemDto> result = await _mediator.Send(new GetSupplierPurchaseOrdersQuery
            {
                Index = index,
                Limit = limit
            });
            _logger.LogDebug($"Purchase orders of the supplier fetched. Count: {result.Count}");
            return Ok(result);
        }
    }
}
