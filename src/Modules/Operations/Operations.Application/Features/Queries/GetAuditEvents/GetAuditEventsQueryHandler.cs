using MediatR;
using Microsoft.EntityFrameworkCore;
using Operations.Application.Features.Shared;
using Operations.Domain.Common;
using Operations.Domain.Dtos;
using Operations.Domain.Entities;
using Operations.Domain.Enums;
using Operations.Infrastructure.Contracts.IRepository;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Operations.Application.Features.Queries.GetAuditEvents
{
    public class GetAuditEventsQueryHandler : IRequestHandler<GetAuditEventsQuery, List<AuditEventResponseDto>>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetAuditEventsQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<List<AuditEventResponseDto>> Handle(GetAuditEventsQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching audit events. EntityType: {request.EntityType}, EntityId: {request.EntityId}, OrganizationId: {request.OrganizationId}");

            int limit = Math.Clamp(request.Limit, 1, 500);
            string? entityType = string.IsNullOrWhiteSpace(request.EntityType) ? null : request.EntityType.Trim();
            List<AuditEvent> events = await _repository.AuditEvent
                .FindByCondition(x => x.OrganizationId == request.OrganizationId
                    && (entityType == null || x.EntityType == entityType)
                    && (request.EntityId == null || x.EntityId == request.EntityId))
                .OrderByDescending(x => x.DateCreated)
                .Take(limit)
                .ToListAsync(cancellationToken);

            _logger.LogInfo($"Audit events fetched. Count: {events.Count}, OrganizationId: {request.OrganizationId}");
            return events.Select(item => new AuditEventResponseDto
            {
                Id = item.Id,
                OperatingUnitId = item.OperatingUnitId,
                UserId = item.UserId,
                EventType = item.EventType,
                EntityType = item.EntityType,
                EntityId = item.EntityId,
                Reference = item.Reference,
                Result = item.Result,
                CreatedAt = item.DateCreated
            }).ToList();
        }
    }
}
