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

using Supplier.Application.Features.Commands.UpdateSupplierQuotation;
using Microsoft.AspNetCore.SignalR;
using Supplier.API.Hubs;

using Supplier.Application.Features.Commands.SubmitVerification;
using Supplier.Application.Features.Queries;





namespace Supplier.API.Controllers
{
    [ApiController]

    public class SupplierController : BaseController
    {
        private readonly IMediator _mediator;
        private readonly ILoggerManager _logger;
        private readonly IHubContext<NotificationHub> _hubContext;
        public SupplierController(
            IMediator mediator,
            ILoggerManager logger,
             IHubContext<NotificationHub> hubContext)
        {
            _mediator = mediator;
            _logger = logger;
            _hubContext = hubContext;
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
            [FromBody] SupplierProfileDto supplierProfileDto)
        {
            _logger.LogDebug("Creating supplier profile.");
            supplierProfileDto.OrganizationId = GetOrganizationId();
            supplierProfileDto.SNID=GetSNID();

            var supplierId = await _mediator.Send(new CreateSupplierProfileCommand(supplierProfileDto));

            _logger.LogDebug("Supplier profile created successfully.");

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
        [SwaggerResponse(200, type: typeof(OrganizationDto), description: "Supplier profile fetched successfully")]
        [SwaggerResponse(404, type: typeof(ErrorResponseDto), description: "Supplier not found")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> GetSupplierProfile([FromQuery] Guid? organizationId)
        {
            Guid orgId = GetOrganizationId();
            string snid=GetSNID();

            var result = await _mediator.Send(new GetSupplierProfileQuery(orgId));

            _logger.LogDebug($"Supplier profile fetched successfully: {orgId}");

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
            _logger.LogDebug("Fetching Supplier Profiles");

            var result = await _mediator.Send(query);

            _logger.LogDebug("Supplier Profiles retrieved successfully");

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
            _logger.LogDebug($"Updating Supplier Status. SupplierId: {command.SupplierId}, Status: {command.Status}");

            var result = await _mediator.Send(command);

            _logger.LogDebug($"Supplier Status updated successfully. SupplierId: {command.SupplierId}");

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
            _logger.LogDebug($"Updating rejected supplier : {command.Supplier.SupplierId}");

            await _mediator.Send(command);

            _logger.LogDebug($"Supplier updated successfully : {command.Supplier.SupplierId}");

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

            _logger.LogDebug($"Fetching Supplier Id for Organization: {organizationId}");

            Guid result = await _mediator.Send(
                new GetSupplierIdQuery(organizationId));

            _logger.LogDebug($"Fetched Supplier Id for Organization: {organizationId}");

            return Ok(result);
        }


        
        [HttpPut]
        [Route("/api/v1/supplier/quotation")]
        [ApiAuthorization(Name = "UPDATE_SUPPLIER_QUOTATION")]
        [ValidateModelState]
        [SwaggerOperation("UpdateSupplierQuotation")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto), description: "Supplier quotation updated successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> UpdateSupplierQuotation(
            [FromBody] UpdateSupplierQuotationDto dto)
        {
            var result = await _mediator.Send(
     new UpdateSupplierQuotationCommand(dto));

            await _hubContext.Clients.All.SendAsync(
          "QuotationSubmitted",
          new
          {
              BuyerId = result.BuyerId,
              SupplierId = result.SupplierId,
              QuotationId = result.QuotationId,
              Message = "Supplier has submitted the quotation."
          });

            return Ok(new SuccessResponseDto
            {
                StatusCode = 200,
                Message = "Success",
                Description = "Supplier quotation updated successfully.",
                Id = result.QuotationId.ToString()
            });
        }


        

        





        [HttpGet]
        [Route("api/v1/supplier/{supplierId}")]
        [ValidateModelState]
        [ApiAuthorization(Name = "GET_SUPPLIER_BY_ID")]
        [SwaggerOperation("GetSupplierById")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto), description: "Supplier catalog updated successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(404, type: typeof(ErrorResponseDto), description: "Supplier catalog not found")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> GetSupplierById(Guid supplierId)
        {
            var result = await _mediator.Send(new GetSupplierByIdQuery
            {
                SupplierId = supplierId
            });

            return Ok(result);
        }

        [HttpPut]
        [Route("api/v1/supplier/submit-verification")]
        [ValidateModelState]
        [ApiAuthorization(Name = "SUBMIT_SUPPLIER_VERIFICATION")]
        [SwaggerOperation("SubmitSupplierVerification")]
        [SwaggerResponse(200, type: typeof(bool), description: "Success")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> SubmitVerification(
             [FromBody] SubmitVerificationDto request)
        {
            var result = await _mediator.Send(
                new SubmitVerificationCommand(request));

            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/supplier/questions-answers")]
        [ValidateModelState]
        [ApiAuthorization(Name = "GET_QUESTIONS_ANSWERS_FOR_SUPPLIER")]
        [SwaggerOperation("GetQuestionsAnswersForSupplier")]
        [SwaggerResponse(200, type: typeof(GetQuestionsAnswersForSupplierDto), description: "Success")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> GetQuestionsAnswersForSupplier(
    Guid requestId)
        {
            var result = await _mediator.Send(
                new GetQuestionsAnswersForSupplierQuery
                {
                    SupplierVerificationRequestId = requestId
                });

            return Ok(result);
        }



        
    }
}

