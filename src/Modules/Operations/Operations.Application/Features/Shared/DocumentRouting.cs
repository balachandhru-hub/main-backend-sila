using Microsoft.EntityFrameworkCore;
using Operations.Domain.Common;
using Operations.Domain.Entities;
using Operations.Domain.Enums;
using Operations.Infrastructure.Contracts.IRepository;

namespace Operations.Application.Features.Shared
{
    /// <summary>
    /// Decides where a saved document is copied to (the uploading user's own destination, then
    /// the destination of the operating unit or one of its parents, then the organization's) and
    /// queues the transfer for the transfer worker.
    /// </summary>
    internal static class DocumentRouting
    {
        public static async Task<(DocumentStorageDestination Destination, string Source)?> ResolveDestinationAsync(
            IRepositoryWrapper repository,
            Guid userId,
            Guid organizationId,
            Guid? operatingUnitId,
            DocumentType documentType,
            CancellationToken cancellationToken)
        {
            List<DocumentStorageDestination> destinations = await repository.DocumentStorageDestination
                .FindByCondition(x => x.OrganizationId == organizationId
                    && x.DocumentType == documentType
                    && x.Status == StorageDestinationStatus.ACTIVE
                    && x.ExternalTransferEnabled)
                .ToListAsync(cancellationToken);
            if (destinations.Count == 0)
            {
                return null;
            }

            UserDocumentStorageAssignment? assignment = await repository.UserDocumentStorageAssignment
                .FindByCondition(x => x.UserId == userId && x.DocumentStorageDestinationId != null && x.Status == StorageAssignmentStatus.VALIDATED)
                .FirstOrDefaultAsync(cancellationToken);
            DocumentStorageDestination? userDestination = assignment == null
                ? null
                : destinations.FirstOrDefault(item => item.Id == assignment.DocumentStorageDestinationId);
            if (userDestination != null)
            {
                return (userDestination, Common.RESOLUTION_USER);
            }

            if (operatingUnitId != null)
            {
                Dictionary<Guid, OrganizationUnit> units = await repository.OrganizationUnit
                    .FindByCondition(x => x.OrganizationId == organizationId)
                    .ToDictionaryAsync(x => x.Id, cancellationToken);
                Guid current = operatingUnitId.Value;
                HashSet<Guid> visited = new HashSet<Guid>();
                while (visited.Add(current) && units.TryGetValue(current, out OrganizationUnit? unit))
                {
                    DocumentStorageDestination? unitDestination = destinations.FirstOrDefault(item => item.OperatingUnitId == unit.Id);
                    if (unitDestination != null)
                    {
                        string source = unit.Kind is OrganizationUnitKind.PROPERTY or OrganizationUnitKind.HOTEL
                            ? Common.RESOLUTION_PROPERTY
                            : Common.RESOLUTION_STORE;
                        return (unitDestination, source);
                    }

                    if (unit.ParentUnitId == null)
                    {
                        break;
                    }

                    current = unit.ParentUnitId.Value;
                }
            }

            DocumentStorageDestination? organizationDestination = destinations.FirstOrDefault(item => item.OperatingUnitId == null);
            return organizationDestination == null ? null : (organizationDestination, Common.RESOLUTION_ORGANIZATION);
        }

        /// <summary>Adds the transfer job (or a "skipped" audit event) to the unit of work; the handler saves.</summary>
        public static async Task QueueTransferAsync(IRepositoryWrapper repository, Document document, Guid userId, CancellationToken cancellationToken)
        {
            (DocumentStorageDestination Destination, string Source)? resolved = await ResolveDestinationAsync(
                repository, userId, document.OrganizationId, document.OperatingUnitId, document.DocumentType, cancellationToken);
            if (resolved == null)
            {
                AuditTrail.Add(repository, document.OrganizationId, document.OperatingUnitId, userId,
                    "DOCUMENT_TRANSFER_SKIPPED", "Document", document.Id, document.OriginalFilename, "NO_DESTINATION");
                return;
            }

            DocumentStorageDestination destination = resolved.Value.Destination;
            repository.DocumentTransferJob.Create(new DocumentTransferJob
            {
                Id = Guid.NewGuid(),
                DocumentId = document.Id,
                DestinationId = destination.Id,
                UserId = userId,
                OrganizationId = document.OrganizationId,
                OperatingUnitId = document.OperatingUnitId,
                DocumentType = document.DocumentType,
                Provider = destination.Provider,
                Status = DocumentTransferStatus.PENDING,
                ResolutionSource = resolved.Value.Source,
                NextAttemptAt = DateTime.UtcNow
            });
            AuditTrail.Add(repository, document.OrganizationId, document.OperatingUnitId, userId,
                "DOCUMENT_TRANSFER_QUEUED", "DocumentTransferJob", document.Id, destination.Id.ToString(), "SUCCESS",
                $"{{\"resolutionSource\":\"{resolved.Value.Source}\",\"provider\":\"{destination.Provider}\"}}");
        }
    }
}
