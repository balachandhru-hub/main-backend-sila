using MediatR;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Attributes;
using SharedKernel.Controllers;
using SharedKernel.Dto;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;
using Swashbuckle.AspNetCore.Annotations;

using Supplier.Application.Features.Commands.SupplierCatalog;
using Supplier.Application.Features.Queries.SupplierCatalog;
using Supplier.Application.Features.Queries.BuyerCatalog;
using Supplier.Domain.Dto;

namespace Supplier.API.Controllers
{
    [ApiController]
    public class CatalogController : BaseController
    {
        private readonly IMediator _mediator;
        private readonly ILoggerManager _logger;

        public CatalogController(
            IMediator mediator,
            ILoggerManager logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// Create Supplier Catalog
        /// </summary>
        [HttpPost]
        [Route("api/v1/supplier/catalog")]
        [ValidateModelState]
        [ApiAuthorization(Name = "CREATE_SUPPLIER_CATALOG")]
        [SwaggerOperation("CreateSupplierCatalog")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto), description: "Supplier catalog created successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> CreateSupplierCatalog(
            [FromBody] CreateSupplierCatalogCommand command)
        {
            command.OrganizationId = GetOrganizationId();

            _logger.LogDebug($"Creating supplier catalog for OrganizationId: {command.OrganizationId}");

            var id = await _mediator.Send(command);

            _logger.LogDebug($"Supplier catalog created successfully. CatalogId: {id}");

            return Ok(new SuccessResponseDto
            {
                StatusCode = 200,
                Message = "Success",
                Description = "Supplier catalog created successfully.",
                Id = id.ToString()
            });
        }

        /// <summary>
        /// Get Supplier Catalog
        /// </summary>
        [HttpGet]
        [Route("api/v1/supplier/catalog")]
        [ValidateModelState]
        [ApiAuthorization(Name = "GET_SUPPLIER_CATALOG")]
        [SwaggerOperation("GetSupplierCatalog")]
        [SwaggerResponse(200, type: typeof(List<GetSupplierCatalogDto>), description: "Supplier catalog retrieved successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> GetSupplierCatalog()
        {
            Guid organizationId = GetOrganizationId();

            _logger.LogDebug($"Fetching supplier catalog for OrganizationId: {organizationId}");

            var query = new GetAllSupplierCatalogQuery
            {
                OrganizationId = organizationId
            };

            var result = await _mediator.Send(query);

            _logger.LogDebug($"Supplier catalog fetched successfully for OrganizationId: {organizationId}");

            return Ok(result);
        }
        /// <summary>
        /// Delete Supplier Catalog
        /// </summary>
        [HttpDelete]
        [Route("api/v1/supplier/catalog/{id}")]
        [ValidateModelState]
        [ApiAuthorization(Name = "DELETE_SUPPLIER_CATALOG")]
        [SwaggerOperation("DeleteSupplierCatalog")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto), description: "Supplier catalog deleted successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(404, type: typeof(ErrorResponseDto), description: "Supplier catalog not found")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> DeleteSupplierCatalog(Guid id)
        {
            _logger.LogDebug($"Deleting supplier catalog: {id}");

            var command = new DeleteSupplierCatalogCommand
            {
                Id = id,
                OrganizationId = GetOrganizationId()
            };

            await _mediator.Send(command);

            _logger.LogDebug($"Supplier catalog deleted successfully: {id}");

            return Ok(new SuccessResponseDto
            {
                StatusCode = 200,
                Message = "Success",
                Description = "Supplier catalog deleted successfully.",
                Id = id.ToString()
            });
        }

        /// <summary>
        /// Update Supplier Catalog
        /// </summary>
        [HttpPut]
        [Route("api/v1/supplier/catalog")]
        [ValidateModelState]
        [ApiAuthorization(Name = "UPDATE_SUPPLIER_CATALOG")]
        [SwaggerOperation("UpdateSupplierCatalog")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto), description: "Supplier catalog updated successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(404, type: typeof(ErrorResponseDto), description: "Supplier catalog not found")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> UpdateSupplierCatalog(
            [FromBody] UpdateSupplierCatalogCommand command)
        {
            command.OrganizationId = GetOrganizationId();

            _logger.LogDebug($"Updating supplier catalog: {command.Catalog.Id}");

            await _mediator.Send(command);

            _logger.LogDebug($"Supplier catalog updated successfully: {command.Catalog.Id}");

            return Ok(new SuccessResponseDto
            {
                StatusCode = 200,
                Message = "Success",
                Description = "Supplier catalog updated successfully.",
                Id = command.Catalog.Id.ToString()
            });
        }

        /// <summary>
        /// Update the SKU, available stock and discount of a catalog product
        /// </summary>
        [HttpPut]
        [Route("api/v1/supplier/catalog/{catalogId}/stock")]
        [ValidateModelState]
        [ApiAuthorization(Name = "UPDATE_SUPPLIER_CATALOG")]
        [SwaggerOperation("UpdateSupplierCatalogStock")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto), description: "Stock updated successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(404, type: typeof(ErrorResponseDto), description: "Supplier catalog not found")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> UpdateSupplierCatalogStock(
            [FromRoute] Guid catalogId,
            [FromBody] UpdateSupplierCatalogStockDto request)
        {
            _logger.LogDebug($"Updating supplier catalog stock: {catalogId}");

            await _mediator.Send(new UpdateSupplierCatalogStockCommand
            {
                Id = catalogId,
                OrganizationId = GetOrganizationId(),
                Stock = request
            });

            _logger.LogDebug($"Supplier catalog stock updated successfully: {catalogId}");

            return Ok(new SuccessResponseDto
            {
                StatusCode = 200,
                Message = "Success",
                Description = "Stock updated successfully.",
                Id = catalogId.ToString()
            });
        }

        [HttpGet]
        [Route("api/v1/supplier/buyer-catalog")]
        [ValidateModelState]
        [ApiAuthorization(Name = "GET_BUYER_CATALOG")]
        [SwaggerOperation("GetBuyerCatalog")]
        [SwaggerResponse(200, type: typeof(List<BuyerCatalogDto>), description: "Buyer catalog fetched successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> GetBuyerCatalog(
            [FromQuery] GetBuyerCatalogQuery query)
        {
            _logger.LogDebug("Fetching buyer catalog.");
            var result = await _mediator.Send(query);

            _logger.LogDebug("Buyer catalog fetched successfully.");
            return Ok(result);
        }


        [HttpGet]
        [Route("api/v1/supplier/buyer-catalog/{catalogId}")]
        [ValidateModelState]
        [ApiAuthorization(Name = "GET_BUYER_CATALOG_BY_ID")]
        [SwaggerOperation("GetBuyerCatalogById")]
        [SwaggerResponse(200, type: typeof(List<BuyerCatalogByIdDto>), description: "Buyer catalog fetched successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(404, type: typeof(ErrorResponseDto), description: "Buyer catalog not found")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> GetBuyerCatalogById(
            [FromRoute] Guid catalogId)
        {
            _logger.LogDebug($"Fetching buyer catalog for CatalogId: {catalogId}");

            var query = new GetBuyerCatalogByIdQuery
            {
                CatalogId = catalogId
            };

            var result = await _mediator.Send(query);

             _logger.LogDebug($"Buyer catalog fetched successfully for CatalogId: {catalogId}");
            return Ok(result);
        }
        [HttpGet]
        [Route("api/v1/supplier/catalog/{catalogId}")]
        [ValidateModelState]
        [ApiAuthorization(Name = "GET_SUPPLIER_CATALOG_BY_ID")]
        [SwaggerOperation("GetSupplierCatalogById")]
        [SwaggerResponse(200, type: typeof(List<SupplierCatalogByIdDto>), description: "Supplier catalog fetched successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(404, type: typeof(ErrorResponseDto), description: "Supplier catalog not found")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> GetSupplierCatalogById(
            [FromRoute] Guid catalogId)
        {
            _logger.LogDebug($"Fetching supplier catalog for CatalogId: {catalogId}");

            var query = new GetSupplierCatalogByIdQuery
            {
                CatalogId = catalogId
            };

            var result = await _mediator.Send(query);

             _logger.LogDebug($"Supplier catalog fetched successfully for CatalogId: {catalogId}");
            return Ok(result);
        }

        /// <summary>
        /// Current stock and price of the given catalog products
        /// </summary>
        [HttpPost]
        [Route("api/v1/supplier/buyer-catalog/stock")]
        [ValidateModelState]
        [ApiAuthorization(Name = "GET_BUYER_CATALOG")]
        [SwaggerOperation("GetBuyerCatalogStock")]
        [SwaggerResponse(200, type: typeof(List<BuyerCatalogStockDto>), description: "Buyer catalog stock fetched successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> GetBuyerCatalogStock(
            [FromBody] GetBuyerCatalogStockQuery query)
        {
            _logger.LogDebug($"Fetching buyer catalog stock. CatalogIds: {query.CatalogIds?.Count ?? 0}");

            List<BuyerCatalogStockDto> result = await _mediator.Send(query);

            _logger.LogDebug($"Buyer catalog stock fetched successfully. Count: {result.Count}");
            return Ok(result);
        }

        /// <summary>
        /// Alternative products that can supply the quantity
        /// </summary>
        [HttpGet]
        [Route("api/v1/supplier/buyer-catalog/{catalogId}/alternatives")]
        [ValidateModelState]
        [ApiAuthorization(Name = "GET_BUYER_CATALOG")]
        [SwaggerOperation("GetBuyerCatalogAlternatives")]
        [SwaggerResponse(200, type: typeof(List<BuyerCatalogStockDto>), description: "Buyer catalog alternatives fetched successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(404, type: typeof(ErrorResponseDto), description: "Buyer catalog not found")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> GetBuyerCatalogAlternatives(
            [FromRoute] Guid catalogId,
            [FromQuery] decimal quantity)
        {
            _logger.LogDebug($"Fetching buyer catalog alternatives for CatalogId: {catalogId}, Quantity: {quantity}");

            GetBuyerCatalogAlternativesQuery query = new GetBuyerCatalogAlternativesQuery
            {
                CatalogId = catalogId,
                Quantity = quantity
            };

            List<BuyerCatalogStockDto> result = await _mediator.Send(query);

            _logger.LogDebug($"Buyer catalog alternatives fetched successfully for CatalogId: {catalogId}. Count: {result.Count}");
            return Ok(result);
        }
    }
}
