using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Buyer.Application.Features.Queries.GetOrganizationProfile;
using SharedKernel.LoggerServices;
using SharedKernel.Dto;
using SharedKernel.Attributes;
using Buyer.Domain.Dto;
using Buyer.Application.Features.Profile.Commands;
using SharedKernel.Controllers;
using Buyer.Application.Features.Queries.GetAllBuyers;
using Buyer.Application.Features.StatusUpdate.Commands;
using Buyer.Application.Features.Commands.Buyer.UpdateRejectedBuyer;
using Buyer.Application.Features.Commands.Department;
using Microsoft.AspNetCore.Identity;
using Buyer.Application.Features.Commands.CostCenter;
using Buyer.Application.Features.Queries.Department;
using Buyer.Application.Features.Queries.CostCenter;
using Buyer.Application.Features.Commands.DepartmentAndCostCenter;

namespace Buyer.API.Controllers
{
    [ApiController]
    public class BuyerController : BaseController
    {
        private readonly IMediator _mediator;
        private readonly ILoggerManager _logger;

        public BuyerController(IMediator mediator, ILoggerManager logger)
        {
            _mediator = mediator;
            _logger = logger;
        }
        /// <summary>
        /// Get buyer Profile
        /// </summary>

        [HttpGet]
        [Route("api/v1/buyer/profile")]
        [ValidateModelState]
        [ApiAuthorization(Name = "GET_MY_BUYER_PROFILE")]
        [SwaggerOperation("GetProfile")]
        [SwaggerResponse(200, type: typeof(OrganizationDto), description: "Fetched the Organization Profile successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad request")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> GetOrganizationProfile()
        {
            Guid organizationId = GetOrganizationId();
            _logger.LogInfo($"Fetching the Organization Profile for ID: {organizationId}");
            var result = await _mediator.Send(new GetOrganizationProfileQuery(organizationId));
            _logger.LogInfo($"Fetched the Organization Profile for ID: {organizationId}");
            return Ok(result);
        }

        /// <summary>
        /// buyer register
        /// </summary>

        [HttpPost]
        [Route("api/v1/buyer/register")]
        [ValidateModelState]
        [ApiAuthorization(Name = "CREATE_BUYER_PROFILE")]
        [SwaggerOperation("CreateProfile")]
        [SwaggerResponse(200, type: typeof(OrganizationDto), description: "Organization Profile created successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad request")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> CreateOrganizationProfile([FromBody] CreateBuyerDto createBuyerDto)
        {
            _logger.LogInfo($"Creating Organization Profile for Organization: {createBuyerDto.OrganizationName}");
            createBuyerDto.OrganizationId = GetOrganizationId();
            var result = await _mediator.Send(new CreateBuyerProfileCommand(createBuyerDto));
            _logger.LogInfo($"Created Organization Profile for Organization: {createBuyerDto.OrganizationName}");
            return Ok(new SuccessResponseDto { Id = result.ToString(), Message = "Buyer Profile created successfully", Description = "Buyer Profile created successfully", StatusCode = 201 });
        }
        /// <summary>
        /// Get all buyer
        /// </summary>

        [HttpPost]
        [Route("api/v1/buyer/get-all-buyer")]
        [ValidateModelState]
        [ApiAuthorization(Name = "GET_ALL_BUYER")]
        [SwaggerOperation("GetAllBuyers")]
        [SwaggerResponse(200, type: typeof(List<OrganizationDto>), description: "Buyers retrieved successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad request")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> GetAllBuyers([FromBody] GetAllBuyersQuery query)
        {
            _logger.LogInfo("Fetching Buyer Profiles");
            var result = await _mediator.Send(query);

            _logger.LogInfo("Buyer Profiles retrieved successfully");

            return Ok(result);
        }

        /// <summary>
        /// approve/reject
        /// </summary>

        [HttpPut]
        [Route("api/v1/buyer/status")]
        [ValidateModelState]
        [ApiAuthorization(Name = "UPDATE_BUYER_STATUS")]
        [SwaggerOperation("UpdateBuyerStatus")]
        [SwaggerResponse(200, type: typeof(bool), description: "Buyer status updated successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad request")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> UpdateBuyerStatus([FromBody] UpdateBuyerStatusCommand command)
        {
            _logger.LogInfo($"Updating Buyer Status. BuyerId: {command.BuyerId}, Status: {command.Status}");

            var result = await _mediator.Send(command);

            _logger.LogInfo($"Buyer Status updated successfully. BuyerId: {command.BuyerId}");

            return Ok(result);
        }
        [HttpPut]
        [Route("api/v1/buyer/update-rejected-buyer")]
        [ValidateModelState]
        [ApiAuthorization(Name = "UPDATE_REJECTED_BUYER")]
        [SwaggerOperation("UpdateRejectedBuyer")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto), description: "Buyer updated successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(404, type: typeof(ErrorResponseDto), description: "Buyer not found")]
        public async Task<IActionResult> UpdateRejectedBuyer(
            [FromBody] UpdateRejectedBuyerCommand command)
        {
            _logger.LogInfo($"Updating rejected buyer : {command.Buyer.BuyerId}");

            await _mediator.Send(command);

            _logger.LogInfo($"Buyer updated successfully : {command.Buyer.BuyerId}");

            return Ok(new SuccessResponseDto
            {
                StatusCode = 200,
                Message = "Success",
                Description = "Buyer updated successfully.",
                Id = command.Buyer.BuyerId.ToString()
            });
        }

        [HttpPost]
        [Route("api/v1/buyer/department")]
        [ValidateModelState]
        [ApiAuthorization(Name = "CREATE_BUYER_DEPARTMENT")]
        [SwaggerOperation("AddBuyerDepartment")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto), description: "Buyer Department successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(404, type: typeof(ErrorResponseDto), description: "Buyer not found")]
        public async Task<IActionResult> CreateDepartment([FromBody] CreateBuyerDepartmentDto dto, [FromQuery] Guid? buyerId)
        {
            _logger.LogInfo($"Creating Department : {dto.Department}");

            dto.BuyerId = buyerId;

            if (!buyerId.HasValue || buyerId == Guid.Empty)
            {
                dto.OrganizationId = GetOrganizationId();
            }


            var result = await _mediator.Send(new CreateBuyerDepartmentCommand(dto));

            return Ok(new SuccessResponseDto
            {
                Id = result.ToString(),
                Message = "Department created successfully",
                Description = "Department created successfully",
                StatusCode = 201
            });
        }

        [HttpPost]
        [Route("api/v1/buyer/costcenter")]
        [ValidateModelState]
        [ApiAuthorization(Name = "CREATE_BUYER_COSTCENTER")]
        [SwaggerOperation("AddBuyerCostcenter")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto), description: "Buyer CostCenter successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(404, type: typeof(ErrorResponseDto), description: "Buyer not found")]
        public async Task<IActionResult> CreateCostCenter([FromBody] CreateBuyerCostCenterDto dto)
        {
            var result = await _mediator.Send(new CreateBuyerCostCenterCommand(dto));

            return Ok(new SuccessResponseDto
            {
                Id = result.ToString(),
                Message = "Cost Center created successfully",
                Description = "Cost Center created successfully",
                StatusCode = 201
            });
        }
        [HttpGet]
        [Route("api/v1/buyer/all-department")]
        [ValidateModelState]
        [ApiAuthorization(Name = "GET_ALL_BUYER_DEPARTMENT")]
        [SwaggerOperation("GetAllBuyerDepartment")]
        [SwaggerResponse(200, type: typeof(List<BuyerDepartmentDto>), description: "Department data fetched successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(404, type: typeof(ErrorResponseDto), description: "Buyer not found")]
        public async Task<IActionResult> GetDepartment(
       [FromQuery] Guid? buyerId,
       [FromQuery] int index = 0,
       [FromQuery] int limit = 10,
       [FromQuery] string? searchTerm = null)
        {
            var result = await _mediator.Send(new GetBuyerDepartmentQuery
            {
                BuyerId = buyerId,
                OrganizationId = buyerId == null ? GetOrganizationId() : Guid.Empty,
                Index = index,
                Limit = limit,
                SearchTerm = searchTerm
            });

            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/buyer/all-costcenter")]
        [ValidateModelState]
        [ApiAuthorization(Name = "GET_ALL_BUYER_COSTCENTER")]
        [SwaggerOperation("GetAllBuyerCostCenter")]
        [SwaggerResponse(200, type: typeof(List<BuyerCostCenterDto>), description: "Cost Center data fetched successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(404, type: typeof(ErrorResponseDto), description: "Department not found")]
        public async Task<IActionResult> GetAllCostCenter(
            [FromQuery] Guid departmentId,
            [FromQuery] int index = 0,
            [FromQuery] int limit = 10,
            [FromQuery] string? searchTerm = null)
        {
            var result = await _mediator.Send(new GetBuyerCostCenterQuery
            {

                Index = index,
                Limit = limit,
                SearchTerm = searchTerm
            });
            return Ok(result);
        }

        [HttpPut]
        [Route("api/v1/buyer/department/{id}")]
        [ValidateModelState]
        [ApiAuthorization(Name = "UPDATE_BUYER_DEPARTMENT")]
        [SwaggerOperation("UpdateBuyerDepartment")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto), description: "Department updated successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(404, type: typeof(ErrorResponseDto), description: "Department not found")]
        public async Task<IActionResult> UpdateDepartment(Guid id,
            [FromBody] UpdateBuyerDepartmentDto dto)
        {
            var result = await _mediator.Send(new UpdateBuyerDepartmentCommand(id, dto));

            return Ok(new SuccessResponseDto
            {
                Id = result.ToString(),
                Message = "Department updated successfully",
                Description = "Department updated successfully",
                StatusCode = 200
            });
        }

        [HttpDelete]
        [Route("api/v1/buyer/department/{id}")]
        [ApiAuthorization(Name = "DELETE_BUYER_DEPARTMENT")]
        [SwaggerOperation("DeleteBuyerDepartment")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto), description: "Department deleted successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(404, type: typeof(ErrorResponseDto), description: "Department not found")]
        public async Task<IActionResult> DeleteDepartment(Guid id)
        {
            var result = await _mediator.Send(new DeleteBuyerDepartmentCommand(id));

            return Ok(new SuccessResponseDto
            {
                Id = result.ToString(),
                Message = "Department deleted successfully",
                Description = "Department deleted successfully",
                StatusCode = 200
            });
        }


        [HttpPut]
        [Route("api/v1/buyer/costcenter/{id}")]
        [ValidateModelState]
        [ApiAuthorization(Name = "UPDATE_COSTCENTER_DEPARTMENT")]
        [SwaggerOperation("UpdateBuyerCostcenter")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto), description: "CostCenter updated successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(404, type: typeof(ErrorResponseDto), description: "Department not found")]
        public async Task<IActionResult> UpdateCostCenter(Guid id,
            [FromBody] UpdateBuyerCostCenterDto dto)
        {
            var result = await _mediator.Send(new UpdateBuyerCostCenterCommand(id, dto));

            return Ok(new SuccessResponseDto
            {
                Id = result.ToString(),
                Message = "CostCenter updated successfully",
                Description = "CostCenter updated successfully",
                StatusCode = 200
            });
        }

        [HttpDelete]
        [Route("api/v1/buyer/costCenter/{id}")]
        [ApiAuthorization(Name = "DELETE_BUYER_COSTCENTER")]
        [SwaggerOperation("DeleteBuyerCostCenter")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto), description: "Department deleted successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(404, type: typeof(ErrorResponseDto), description: "Department not found")]
        public async Task<IActionResult> DeleteCostCenter(Guid id)
        {
            var result = await _mediator.Send(new DeleteBuyerCostCenterCommand(id));

            return Ok(new SuccessResponseDto
            {
                Id = result.ToString(),
                Message = "CostCenter deleted successfully",
                Description = "CostCenter deleted successfully",
                StatusCode = 200
            });
        }

        [HttpPost]
        [Route("api/v1/buyer/upload-department-costcenter")]
        [ValidateModelState]
        [ApiAuthorization(Name = "UPLOAD_BUYER_DEPARTMENT_COSTCENTER")]
        [SwaggerOperation("UploadDepartmentCostCenter")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto), description: "Department and Cost Center uploaded successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(404, type: typeof(ErrorResponseDto), description: "Buyer not found")]
        public async Task<IActionResult> UploadDepartmentCostCenter([FromForm] UploadDepartmentCostCenterDto dto)
        {
            dto.OrganizationId = GetOrganizationId();

            var result = await _mediator.Send(new UploadDepartmentCostCenterCommand(dto));

            return Ok(new SuccessResponseDto
            {
                Message = "Department and Cost Center uploaded successfully",
                Description = "Department and Cost Center uploaded successfully",
                StatusCode = 200
            });
        }
    }
}