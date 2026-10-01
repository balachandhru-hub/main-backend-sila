using Operations.Domain.Entities;
using Operations.Infrastructure.Contracts.IRepository;

namespace Operations.Application.Features.Shared
{
    /// <summary>
    /// Adds an audit event to the unit of work. Centralized here because every receiving
    /// command records its steps the same way; the event is stored by the handler's SaveAsync.
    /// </summary>
    internal static class AuditTrail
    {
        public static void Add(
            IRepositoryWrapper repository,
            Guid organizationId,
            Guid? operatingUnitId,
            Guid? userId,
            string eventType,
            string entityType,
            Guid? entityId,
            string? reference,
            string? result = null,
            string? metadataJson = null)
        {
            repository.AuditEvent.Create(new AuditEvent
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                OperatingUnitId = operatingUnitId,
                UserId = userId,
                ChangedByUserId = userId,
                EventType = eventType,
                EntityType = entityType,
                EntityId = entityId,
                Reference = reference != null && reference.Length > 200 ? reference[..200] : reference,
                Result = result,
                MetadataJson = metadataJson
            });
        }
    }
}
