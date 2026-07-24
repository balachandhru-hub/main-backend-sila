using MediatR;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Attributes;
using SharedKernel.Dto;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;
using Supplier.Application.Features.Commands.Supplier;
using Swashbuckle.AspNetCore.Annotations;
using Supplier.Application.Features.Queries.Supplier;
using Supplier.Domain.Dto;
using SharedKernel.Controllers;
using Supplier.Application.Features.StatusUpdate.Commands;
using Supplier.Application.Features.Commands.Supplier.UpdateRejectedSupplier;
using Supplier.Application.Features.Commands.Supplier.UpdateSupplierStatusOrganization;
using Supplier.Application.Features.Profile.Queries.GetSupplierId;
using Supplier.Application.Features.Commands.SupplierCatalog;
using Supplier.Application.Features.Queries.SupplierCatalog;
using Supplier.Application.Features.Queries.GetSupplier;


namespace Supplier.API.Controllers
{
    [ApiController]

    public class SupplierController : BaseController
    {
        private readonly IMediator _mediator;
        private readonly ILoggerManager _logger;

        public SupplierController(
            IMediator mediator,
            ILoggerManager logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// Create Supplier Profile
        /// </summary>
        [HttpPost]
        [Route("api/v1/supplier/register")]
        [ApiAuthorization(Name = "CREATE_SUPPLIER_PROFILE")]
        [ValidateModelState]
        [SwaggerOperation("CreateSupplierProfile")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto), description: "Upload successful")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> CreateSupplierProfile(
            [FromBody] CreateSupplierProfileCommand command)
        {
            _logger.LogInfo("Creating supplier profile.");

            var supplierId = await _mediator.Send(command);

            _logger.LogInfo("Supplier profile created successfully.");

            return Ok(new
            {
                Success = true,
                SupplierId = supplierId,
                Message = "Supplier profile created successfully."
            });
        }

        /// <summary>
        /// Get Supplier Profile
        /// </summary>
        [HttpGet]
        [Route("api/v1/supplier/profile")]
        [ApiAuthorization(Name = "GET_SUPPLIER_PROFILE")]
        [ValidateModelState]
        [SwaggerOperation("GetSupplierProfile")]
        [SwaggerResponse(200, type: typeof(SupplierProfileDto), description: "Supplier profile fetched successfully")]
        [SwaggerResponse(404, type: typeof(ErrorResponseDto), description: "Supplier not found")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> GetSupplierProfile()
        {
            Guid organizationId = GetOrganizationId();

            var result = await _mediator.Send(new GetSupplierProfileQuery(organizationId));

            _logger.LogInfo($"Supplier profile fetched successfully: {organizationId}");

            return Ok(result);
        }

        /// <summary>
        /// Get All Supplier Profiles
        /// </summary>
        [HttpPost]
        [Route("api/v1/supplier/get-all-supplier")]
        [ValidateModelState]
        [ApiAuthorization(Name = "GET_ALL_SUPPLIER")]
        [SwaggerOperation("GetAllSuppliers")]
        [SwaggerResponse(200, type: typeof(List<SupplierProfileDto>), description: "Suppliers retrieved successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad request")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> GetAllSuppliers([FromBody] GetAllSuppliersQuery query)
        {
            _logger.LogInfo("Fetching Supplier Profiles");

            var result = await _mediator.Send(query);

            _logger.LogInfo("Supplier Profiles retrieved successfully");

            return Ok(result);
        }

        /// <summary>
        /// Update Supplier Status
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        [HttpPut]
        [Route("api/v1/supplier/status")]
        [ValidateModelState]
        [ApiAuthorization(Name = "UPDATE_SUPPLIER_STATUS")]
        [SwaggerOperation("UpdateSupplierStatus")]
        [SwaggerResponse(200, type: typeof(bool), description: "Supplier status updated successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad request")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> UpdateSupplierStatus([FromBody] UpdateSupplierStatusCommand command)
        {
            _logger.LogInfo($"Updating Supplier Status. SupplierId: {command.SupplierId}, Status: {command.Status}");

            var result = await _mediator.Send(command);

            _logger.LogInfo($"Supplier Status updated successfully. SupplierId: {command.SupplierId}");

            return Ok(result);
        }
        [HttpPut]
        [Route("api/v1/supplier/update-rejected-supplier")]
        [ValidateModelState]
        [ApiAuthorization(Name = "UPDATE_REJECTED_SUPPLIER")]
        [SwaggerOperation("UpdateRejectedSupplier")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto), description: "Supplier updated successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(404, type: typeof(ErrorResponseDto), description: "Supplier not found")]
        public async Task<IActionResult> UpdateRejectedSupplier(
            [FromBody] UpdateRejectedSupplierCommand command)
        {
            _logger.LogInfo($"Updating rejected supplier : {command.Supplier.SupplierId}");

            await _mediator.Send(command);

            _logger.LogInfo($"Supplier updated successfully : {command.Supplier.SupplierId}");

            return Ok(new SuccessResponseDto
            {
                StatusCode = 200,
                Message = "Success",
                Description = "Supplier updated successfully.",
                Id = command.Supplier.SupplierId.ToString()
            });
        }
        [HttpPut]
        [Route("api/v1/supplier/internal-status")]
        [ValidateModelState]
        [ApiAuthorization(Name = "UPDATE_SUPPLIER_STATUS_ORGANIZATION")]
        [SwaggerOperation("UpdateSupplierStatus")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto), description: "Supplier updated successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(404, type: typeof(ErrorResponseDto), description: "Supplier not found")]
        public async Task<IActionResult> UpdateSupplierStatus(
            [FromBody] UpdateSupplierStatusOrganizationCommand command)
        {
            await _mediator.Send(command);

            return Ok(new SuccessResponseDto
            {
                StatusCode = 200,
                Message = "Success",
                Description = "Supplier status updated successfully.",
                Id = command.Supplier.OrganizationId.ToString()
            });
        }
        /// <summary>
        /// Get Supplier Id
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("api/v1/supplier/id")]
        [ValidateModelState]
        [ApiAuthorization(Name = "GET_SUPPLIER_ID")]
        [SwaggerOperation("GetSupplierId")]
        [SwaggerResponse(200, type: typeof(Guid), description: "Fetched Supplier Id successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad request")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> GetSupplierId()
        {
            Guid organizationId = GetOrganizationId();

            _logger.LogInfo($"Fetching Supplier Id for Organization: {organizationId}");

            Guid result = await _mediator.Send(
                new GetSupplierIdQuery(organizationId));

            _logger.LogInfo($"Fetched Supplier Id for Organization: {organizationId}");

            return Ok(result);
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

            _logger.LogInfo($"Creating supplier catalog for OrganizationId: {command.OrganizationId}");

            var id = await _mediator.Send(command);

            _logger.LogInfo($"Supplier catalog created successfully. CatalogId: {id}");

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

            _logger.LogInfo($"Fetching supplier catalog for OrganizationId: {organizationId}");

            var query = new GetAllSupplierCatalogQuery
            {
                OrganizationId = organizationId
            };

            var result = await _mediator.Send(query);

            _logger.LogInfo($"Supplier catalog fetched successfully for OrganizationId: {organizationId}");

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
            _logger.LogInfo($"Deleting supplier catalog: {id}");

            var command = new DeleteSupplierCatalogCommand
            {
                Id = id,
                OrganizationId = GetOrganizationId()
            };

            await _mediator.Send(command);

            _logger.LogInfo($"Supplier catalog deleted successfully: {id}");

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

            _logger.LogInfo($"Updating supplier catalog: {command.Catalog.Id}");

            await _mediator.Send(command);

            _logger.LogInfo($"Supplier catalog updated successfully: {command.Catalog.Id}");

            return Ok(new SuccessResponseDto
            {
                StatusCode = 200,
                Message = "Success",
                Description = "Supplier catalog updated successfully.",
                Id = command.Catalog.Id.ToString()
            });
        }


        [HttpPost]
        [Route("api/v1/supplier/rfq-verfied-supplier")]
        [ValidateModelState]
        [ApiAuthorization(Name = "GET_SUPPLIER_LIST")]
        [SwaggerOperation("GetSupplierList")]
        [SwaggerResponse(200, type: typeof(List<SupplierListDto>), description: "Supplier list fetched successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(404, type: typeof(ErrorResponseDto), description: "Supplier not found")]
        public async Task<IActionResult> GetSupplierList(
     [FromBody] GetSupplierListDto supplierListDto,
     CancellationToken cancellationToken)
        {
            var query = new GetSupplierListQuery(supplierListDto);

            var result = await _mediator.Send(query, cancellationToken);

            return Ok(result);
        }
    }
}
