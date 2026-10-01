using Operations.Application.Features.Commands.CreateOrganizationUnit;
using Operations.Application.Features.Commands.DeleteOrganizationUnit;
using Operations.Application.Features.Commands.UpdateOrganizationUnit;
using Operations.Application.Features.Queries.GetOrganizationUnits;
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
    /// Units of the organization: property, hotel, outlet, kitchen, store and storage location.
    /// </summary>
    [ApiController]
    public class UnitController : BaseController
    {
        private readonly IMediator _mediator;
        private readonly ILoggerManager _logger;

        public UnitController(IMediator mediator, ILoggerManager logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpGet]
        [Route("api/v1/operations/units")]
        [ApiAuthorization(Name = "OPERATIONS_VIEW_UNIT")]
        [SwaggerOperation("GetUnits")]
        [SwaggerResponse(200, type: typeof(List<OrganizationUnitResponseDto>))]
        public async Task<IActionResult> GetUnits()
        {
            _logger.LogDebug($"Fetching organization units.");
            List<OrganizationUnitResponseDto> result = await _mediator.Send(new GetOrganizationUnitsQuery
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId()
            });
            _logger.LogDebug($"Organization units fetched. Count: {result.Count}");
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/operations/units")]
        [ApiAuthorization(Name = "OPERATIONS_MANAGE_UNIT")]
        [SwaggerOperation("CreateUnit")]
        [SwaggerResponse(200, type: typeof(OrganizationUnitResponseDto))]
        public async Task<IActionResult> CreateUnit([FromBody] CreateOrganizationUnitRequestDto request)
        {
            _logger.LogDebug($"Creating organization unit. Code: {request.Code}, Kind: {request.Kind}");
            OrganizationUnitResponseDto result = await _mediator.Send(new CreateOrganizationUnitCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                Request = request
            });
            _logger.LogDebug($"Organization unit created. UnitId: {result.Id}");
            return Ok(result);
        }

        [HttpPut]
        [Route("api/v1/operations/units/{unitId:guid}")]
        [ApiAuthorization(Name = "OPERATIONS_MANAGE_UNIT")]
        [SwaggerOperation("UpdateUnit")]
        [SwaggerResponse(200, type: typeof(OrganizationUnitResponseDto))]
        public async Task<IActionResult> UpdateUnit([FromRoute] Guid unitId, [FromBody] UpdateOrganizationUnitRequestDto request)
        {
            _logger.LogDebug($"Updating organization unit. UnitId: {unitId}");
            OrganizationUnitResponseDto result = await _mediator.Send(new UpdateOrganizationUnitCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                UnitId = unitId,
                Request = request
            });
            _logger.LogDebug($"Organization unit updated. UnitId: {result.Id}");
            return Ok(result);
        }

        [HttpDelete]
        [Route("api/v1/operations/units/{unitId:guid}")]
        [ApiAuthorization(Name = "OPERATIONS_MANAGE_UNIT")]
        [SwaggerOperation("DeleteUnit")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto))]
        public async Task<IActionResult> DeleteUnit([FromRoute] Guid unitId)
        {
            _logger.LogDebug($"Deleting organization unit. UnitId: {unitId}");
            Guid result = await _mediator.Send(new DeleteOrganizationUnitCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                UnitId = unitId
            });
            _logger.LogDebug($"Organization unit deleted. UnitId: {result}");
            return Ok(new SuccessResponseDto
            {
                Id = result.ToString(),
                StatusCode = 200,
                Message = "Success",
                Description = "Unit deleted."
            });
        }
    }
}
