using Operations.Application.Features.Queries.GetAuditEvents;
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
    /// Audit trail of the receiving process.
    /// </summary>
    [ApiController]
    public class AuditController : BaseController
    {
        private readonly IMediator _mediator;
        private readonly ILoggerManager _logger;

        public AuditController(IMediator mediator, ILoggerManager logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpGet]
        [Route("api/v1/operations/audit-events")]
        [ApiAuthorization(Name = "OPERATIONS_VIEW_AUDIT")]
        [SwaggerOperation("GetAuditEvents")]
        [SwaggerResponse(200, type: typeof(List<AuditEventResponseDto>))]
        public async Task<IActionResult> GetAuditEvents([FromQuery] string? entityType, [FromQuery] Guid? entityId, [FromQuery] int limit = 200)
        {
            _logger.LogDebug($"Fetching audit events. EntityType: {entityType}, EntityId: {entityId}");
            List<AuditEventResponseDto> result = await _mediator.Send(new GetAuditEventsQuery
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                EntityType = entityType,
                EntityId = entityId,
                Limit = limit
            });
            _logger.LogDebug($"Audit events fetched. Count: {result.Count}");
            return Ok(result);
        }
    }
}
