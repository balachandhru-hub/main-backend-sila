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
            return Ok(result);
        }
        /// <summary>
        /// Get all buyer
        /// </summary>
        
        [HttpPost]
        [Route("api/v1/buyer/getAllbuyer")]
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
    }
}