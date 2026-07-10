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
        [Route("api/v1/CreateSupplierProfile")]
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
        [Route("api/v1/GetSupplierProfile")]
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
        [HttpGet]
        [Route("api/v1/supplier/getAllSupplier")]
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

    }
}
