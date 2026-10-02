using Buyer.Application.Features.Commands.CreateOutlet;
using Buyer.Application.Features.Commands.CreateProperty;
using Buyer.Application.Features.Commands.SetUserOutlets;
using Buyer.Application.Features.Commands.UpdateOutlet;
using Buyer.Application.Features.Commands.UpdateProperty;
using Buyer.Application.Features.Queries.GetOutlets;
using Buyer.Application.Features.Queries.GetOutletUsers;
using Buyer.Application.Features.Queries.GetProperties;
using Buyer.Domain.Dtos;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Attributes;
using SharedKernel.Controllers;
using SharedKernel.Dto;
using SharedKernel.LoggerServices;
using Swashbuckle.AspNetCore.Annotations;

namespace Buyer.API.Controllers
{
    /// <summary>
    /// Properties (plants) of the buyer, their outlets (storage locations) and the users of the outlets.
    /// </summary>
    [ApiController]
    public class PropertyController : BaseController
    {
        private readonly IMediator _mediator;
        private readonly ILoggerManager _logger;

        public PropertyController(IMediator mediator, ILoggerManager logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpGet]
        [Route("api/v1/buyer/properties")]
        [ApiAuthorization(Name = "GET_PROPERTY")]
        [SwaggerOperation("GetProperties")]
        [SwaggerResponse(200, type: typeof(List<PropertyResponseDto>))]
        public async Task<IActionResult> Properties()
        {
            _logger.LogDebug("Fetching properties.");
            List<PropertyResponseDto> result = await _mediator.Send(new GetPropertiesQuery
            {
                OrganizationId = GetOrganizationId()
            });
            _logger.LogDebug($"Properties fetched. Count: {result.Count}");
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/buyer/properties")]
        [ApiAuthorization(Name = "MANAGE_OUTLET")]
        [SwaggerOperation("CreateProperty")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto))]
        public async Task<IActionResult> CreateProperty([FromBody] PropertyWriteDto request)
        {
            _logger.LogDebug($"Creating property. PlantCode: {request.PlantCode}");
            Guid id = await _mediator.Send(new CreatePropertyCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                Request = request
            });
            _logger.LogDebug($"Property created. PropertyId: {id}");
            return Ok(new SuccessResponseDto
            {
                Id = id.ToString(),
                StatusCode = 200,
                Message = "Success",
                Description = "Property created."
            });
        }

        [HttpPut]
        [Route("api/v1/buyer/properties/{propertyId}")]
        [ApiAuthorization(Name = "MANAGE_OUTLET")]
        [SwaggerOperation("UpdateProperty")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto))]
        public async Task<IActionResult> UpdateProperty([FromRoute] Guid propertyId, [FromBody] PropertyWriteDto request)
        {
            _logger.LogDebug($"Updating property. PropertyId: {propertyId}");
            await _mediator.Send(new UpdatePropertyCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                PropertyId = propertyId,
                Request = request
            });
            _logger.LogDebug($"Property updated. PropertyId: {propertyId}");
            return Ok(new SuccessResponseDto
            {
                Id = propertyId.ToString(),
                StatusCode = 200,
                Message = "Success",
                Description = "Property updated."
            });
        }

        [HttpGet]
        [Route("api/v1/buyer/outlets")]
        [ApiAuthorization(Name = "GET_OUTLET")]
        [SwaggerOperation("GetOutlets")]
        [SwaggerResponse(200, type: typeof(List<OutletResponseDto>))]
        public async Task<IActionResult> Outlets()
        {
            _logger.LogDebug("Fetching outlets.");
            List<OutletResponseDto> result = await _mediator.Send(new GetOutletsQuery
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId()
            });
            _logger.LogDebug($"Outlets fetched. Count: {result.Count}");
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/buyer/outlets")]
        [ApiAuthorization(Name = "CREATE_OUTLET")]
        [SwaggerOperation("CreateOutlet")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto))]
        public async Task<IActionResult> CreateOutlet([FromBody] OutletWriteDto request)
        {
            _logger.LogDebug($"Creating outlet. OutletName: {request.OutletName}");
            Guid id = await _mediator.Send(new CreateOutletCommand
            {
                OrganizationId = GetOrganizationId(),
                Request = request
            });
            _logger.LogDebug($"Outlet created. OutletId: {id}");
            return Ok(new SuccessResponseDto
            {
                Id = id.ToString(),
                StatusCode = 200,
                Message = "Success",
                Description = "Outlet created."
            });
        }

        [HttpPut]
        [Route("api/v1/buyer/outlets/{outletId}")]
        [ApiAuthorization(Name = "MANAGE_OUTLET")]
        [SwaggerOperation("UpdateOutlet")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto))]
        public async Task<IActionResult> UpdateOutlet([FromRoute] Guid outletId, [FromBody] OutletWriteDto request)
        {
            _logger.LogDebug($"Updating outlet. OutletId: {outletId}");
            await _mediator.Send(new UpdateOutletCommand
            {
                OrganizationId = GetOrganizationId(),
                OutletId = outletId,
                Request = request
            });
            _logger.LogDebug($"Outlet updated. OutletId: {outletId}");
            return Ok(new SuccessResponseDto
            {
                Id = outletId.ToString(),
                StatusCode = 200,
                Message = "Success",
                Description = "Outlet updated."
            });
        }

        [HttpGet]
        [Route("api/v1/buyer/outlets/users")]
        [ApiAuthorization(Name = "MANAGE_OUTLET")]
        [SwaggerOperation("GetOutletUsers")]
        [SwaggerResponse(200, type: typeof(List<OutletUserMappingDto>))]
        public async Task<IActionResult> OutletUsers()
        {
            _logger.LogDebug("Fetching outlet users.");
            List<OutletUserMappingDto> result = await _mediator.Send(new GetOutletUsersQuery
            {
                OrganizationId = GetOrganizationId()
            });
            _logger.LogDebug($"Outlet users fetched. Count: {result.Count}");
            return Ok(result);
        }

        [HttpPut]
        [Route("api/v1/buyer/outlets/users/{userId}")]
        [ApiAuthorization(Name = "MANAGE_OUTLET")]
        [SwaggerOperation("SetUserOutlets")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto))]
        public async Task<IActionResult> SetUserOutlets([FromRoute] Guid userId, [FromBody] OutletUsersWriteDto request)
        {
            _logger.LogDebug($"Assigning outlets to user. UserId: {userId}");
            await _mediator.Send(new SetUserOutletsCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = userId,
                Request = request
            });
            _logger.LogDebug($"Outlets assigned to user. UserId: {userId}");
            return Ok(new SuccessResponseDto
            {
                Id = userId.ToString(),
                StatusCode = 200,
                Message = "Success",
                Description = "Outlets assigned."
            });
        }
    }
}
