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

        [HttpGet]
        [Route("api/v1/buyer/{organizationId}")]
        [ValidateModelState]
        [ApiAuthorization(Name = "GET_MY_BUYER_PROFILE")]
        [SwaggerOperation("GetProfile")]
        [SwaggerResponse(200, type: typeof(OrganizationDto), description: "Fetched the Organization Profile successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad request")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> GetOrganizationProfile(Guid organizationId)
        {
            _logger.LogInfo($"Fetching the Organization Profile for ID: {organizationId}");
            var result = await _mediator.Send(new GetOrganizationProfileQuery(organizationId));
            _logger.LogInfo($"Fetched the Organization Profile for ID: {organizationId}");
            return Ok(result);
        }

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
        
        [HttpGet]
        [Route("api/v1/buyer/getAllbuyer")]
        [ValidateModelState]
        [ApiAuthorization(Name = "GET_ALL_BUYER")]
        [SwaggerOperation("GetAllBuyers")]
        [SwaggerResponse(200, type: typeof(List<OrganizationDto>), description: "Buyers retrieved successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad request")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> GetAllBuyers( [FromQuery] int index = 1,[FromQuery] int limit = 10)
        {
            _logger.LogInfo("Fetching Buyer Profiles");
            var result = await _mediator.Send(new GetAllBuyersQuery
            {
                Index = index,
                Limit = limit
            });

            _logger.LogInfo("Buyer Profiles retrieved successfully");

            return Ok(result);
        }
    }
}