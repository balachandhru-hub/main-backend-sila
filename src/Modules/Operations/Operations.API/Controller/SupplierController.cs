using Operations.Application.Features.Commands.UpsertSupplier;
using Operations.Application.Features.Queries.GetSupplier;
using Operations.Application.Features.Queries.GetSuppliers;
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
    /// Supplier master of the organization.
    /// </summary>
    [ApiController]
    public class SupplierController : BaseController
    {
        private readonly IMediator _mediator;
        private readonly ILoggerManager _logger;

        public SupplierController(IMediator mediator, ILoggerManager logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpGet]
        [Route("api/v1/operations/master-data/suppliers")]
        [ApiAuthorization(Name = "OPERATIONS_VIEW_SUPPLIER")]
        [SwaggerOperation("GetSuppliers")]
        [SwaggerResponse(200, type: typeof(List<SupplierResponseDto>))]
        public async Task<IActionResult> GetSuppliers([FromQuery] SupplierSearchRequestDto request)
        {
            _logger.LogDebug($"Fetching suppliers. Query: {request.Query}, EntityCode: {request.EntityCode}, Status: {request.Status}");
            List<SupplierResponseDto> result = await _mediator.Send(new GetSuppliersQuery
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                Request = request
            });
            _logger.LogDebug($"Suppliers fetched. Count: {result.Count}");
            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/operations/master-data/suppliers/{supplierId:guid}")]
        [ApiAuthorization(Name = "OPERATIONS_VIEW_SUPPLIER")]
        [SwaggerOperation("GetSupplier")]
        [SwaggerResponse(200, type: typeof(SupplierResponseDto))]
        public async Task<IActionResult> GetSupplier([FromRoute] Guid supplierId)
        {
            _logger.LogDebug($"Fetching supplier. SupplierId: {supplierId}");
            SupplierResponseDto result = await _mediator.Send(new GetSupplierQuery
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                SupplierId = supplierId
            });
            _logger.LogDebug($"Supplier fetched. SupplierId: {result.Id}");
            return Ok(result);
        }

        [HttpPut]
        [Route("api/v1/operations/master-data/suppliers")]
        [ApiAuthorization(Name = "OPERATIONS_MANAGE_SUPPLIER")]
        [SwaggerOperation("CreateSupplier")]
        [SwaggerResponse(200, type: typeof(SupplierResponseDto))]
        public async Task<IActionResult> CreateSupplier([FromBody] SupplierUpsertRequestDto request)
        {
            _logger.LogDebug($"Creating supplier. SupplierCode: {request.SupplierCode}");
            SupplierResponseDto result = await _mediator.Send(new UpsertSupplierCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                SupplierId = null,
                Request = request
            });
            _logger.LogDebug($"Supplier created. SupplierId: {result.Id}");
            return Ok(result);
        }

        [HttpPut]
        [Route("api/v1/operations/master-data/suppliers/{supplierId:guid}")]
        [ApiAuthorization(Name = "OPERATIONS_MANAGE_SUPPLIER")]
        [SwaggerOperation("UpdateSupplier")]
        [SwaggerResponse(200, type: typeof(SupplierResponseDto))]
        public async Task<IActionResult> UpdateSupplier([FromRoute] Guid supplierId, [FromBody] SupplierUpsertRequestDto request)
        {
            _logger.LogDebug($"Updating supplier. SupplierId: {supplierId}");
            SupplierResponseDto result = await _mediator.Send(new UpsertSupplierCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                SupplierId = supplierId,
                Request = request
            });
            _logger.LogDebug($"Supplier updated. SupplierId: {result.Id}");
            return Ok(result);
        }
    }
}
