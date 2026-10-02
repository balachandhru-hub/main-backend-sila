using Microsoft.EntityFrameworkCore;
using SilaMe.Api.Auth;
using SilaMe.Api.Data;
using SilaMe.Api.DTOs;
using SilaMe.Api.Models;
using SilaMe.Api.Tenancy;

namespace SilaMe.Api.Services;

public sealed record ResolvedDocumentDestination(
    DocumentStorageDestination Destination,
    string ResolutionSource);

public sealed class DocumentRoutingService(
    SilaMeDbContext db,
    IDocumentStorageService storage,
    MicrosoftSharePointService microsoft,
    ILogger<DocumentRoutingService> logger)
{
    public async Task<ResolvedDocumentDestination?> ResolveDestinationAsync(
        Guid userId,
        Guid organizationId,
        Guid? operatingUnitId,
        DocumentType documentType,
        CancellationToken cancellationToken)
    {
        var assignment = await db.UserDocumentStorageAssignments
            .AsNoTracking()
            .Include(item => item.DocumentStorageDestination)
            .Where(item => item.UserId == userId &&
                item.DocumentStorageDestinationId != null &&
                item.Status == StorageAssignmentStatus.VALIDATED &&
                item.DocumentStorageDestination!.OrganizationId == organizationId &&
                item.DocumentStorageDestination.DocumentType == documentType &&
                item.DocumentStorageDestination.Status == StorageDestinationStatus.ACTIVE &&
                item.DocumentStorageDestination.ExternalTransferEnabled)
            .Select(item => item.DocumentStorageDestination!)
            .FirstOrDefaultAsync(cancellationToken);
        if (assignment is not null)
        {
            return new(assignment, "USER");
        }

        var destinations = await db.DocumentStorageDestinations
            .AsNoTracking()
            .Where(item => item.OrganizationId == organizationId &&
                item.DocumentType == documentType &&
                item.Status == StorageDestinationStatus.ACTIVE &&
                item.ExternalTransferEnabled)
            .ToListAsync(cancellationToken);

        if (operatingUnitId is not null)
        {
            var units = await db.OrganizationUnits
                .AsNoTracking()
                .Where(item => item.OrganizationId == organizationId)
                .ToDictionaryAsync(item => item.Id, cancellationToken);
            var current = operatingUnitId.Value;
            while (units.TryGetValue(current, out var unit))
            {
                var destination = destinations.FirstOrDefault(item => item.OperatingUnitId == unit.Id);
                if (destination is not null)
                {
                    var source = unit.Kind is OrganizationUnitKind.STORE or OrganizationUnitKind.OUTLET or OrganizationUnitKind.KITCHEN
                        ? "STORE"
                        : unit.Kind is OrganizationUnitKind.PROPERTY or OrganizationUnitKind.HOTEL ? "PROPERTY" : "STORE";
                    return new(destination, source);
                }
                if (unit.ParentUnitId is null) break;
                current = unit.ParentUnitId.Value;
            }
        }

        var organizationDestination = destinations.FirstOrDefault(item => item.OperatingUnitId == null);
        return organizationDestination is null ? null : new(organizationDestination, "ORGANIZATION");
    }

    public async Task QueueTransferAsync(
        Document document,
        Session session,
        CancellationToken cancellationToken)
    {
        var destination = await ResolveDestinationAsync(
            session.CustomerUserId(),
            document.OrganizationId,
            document.OperatingUnitId,
            document.DocumentType,
            cancellationToken);
        var now = DateTime.UtcNow;
        if (destination is null)
        {
            db.AuditEvents.Add(new AuditEvent
            {
                Id = Guid.NewGuid(),
                OrganizationId = document.OrganizationId,
                OperatingUnitId = document.OperatingUnitId,
                UserId = session.UserId,
                EventType = "DOCUMENT_TRANSFER_SKIPPED",
                EntityType = "Document",
                EntityId = document.Id,
                Reference = document.OriginalFilename,
                Result = "NO_DESTINATION",
                CreatedAt = now,
            });
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        var exists = await db.DocumentTransferJobs.AnyAsync(
            item => item.DocumentId == document.Id && item.DestinationId == destination.Destination.Id,
            cancellationToken);
        if (exists) return;

        db.DocumentTransferJobs.Add(new DocumentTransferJob
        {
            Id = Guid.NewGuid(),
            DocumentId = document.Id,
            DestinationId = destination.Destination.Id,
            UserId = session.CustomerUserId(),
            OrganizationId = document.OrganizationId,
            OperatingUnitId = document.OperatingUnitId,
            DocumentType = document.DocumentType,
            Provider = destination.Destination.Provider,
            Status = DocumentTransferStatus.PENDING,
            ResolutionSource = destination.ResolutionSource,
            NextAttemptAt = now,
            CreatedAt = now,
            UpdatedAt = now,
        });
        db.AuditEvents.Add(new AuditEvent
        {
            Id = Guid.NewGuid(),
            OrganizationId = document.OrganizationId,
            OperatingUnitId = document.OperatingUnitId,
            UserId = session.UserId,
            EventType = "DOCUMENT_TRANSFER_QUEUED",
            EntityType = "DocumentTransferJob",
            EntityId = document.Id,
            Reference = destination.Destination.Id.ToString(),
            MetadataJson = $"{{\"resolutionSource\":\"{destination.ResolutionSource}\",\"provider\":\"{destination.Destination.Provider}\"}}",
            Result = "SUCCESS",
            CreatedAt = now,
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DocumentTransferResponse>> GetTransfersAsync(
        Session session,
        Guid documentId,
        CancellationToken cancellationToken)
    {
        var document = await db.Documents.AsNoTracking().SingleOrDefaultAsync(item => item.Id == documentId, cancellationToken)
            ?? throw new OperationalException("DOCUMENT_NOT_FOUND", "The document was not found.");
        await EnsureVisibleAsync(session, document.OrganizationId, document.OperatingUnitId, cancellationToken);
        return await db.DocumentTransferJobs.AsNoTracking()
            .Include(item => item.Destination)
            .Where(item => item.DocumentId == documentId)
            .OrderByDescending(item => item.CreatedAt)
            .Select(item => new DocumentTransferResponse(
                item.DocumentId,
                item.DestinationId,
                item.ExternalFileId,
                item.Provider,
                item.Status,
                item.ResolutionSource,
                item.Destination.FolderPath,
                item.ExternalFileName,
                item.ExternalWebUrl,
                item.AttemptCount,
                item.NextAttemptAt,
                item.CompletedAt,
                item.LastErrorCode,
                item.LastErrorMessageSafe))
            .ToListAsync(cancellationToken);
    }

    public async Task RetryAsync(Session session, Guid jobId, CancellationToken cancellationToken)
    {
        var job = await db.DocumentTransferJobs
            .Include(item => item.Document)
            .SingleOrDefaultAsync(item => item.Id == jobId, cancellationToken)
            ?? throw new OperationalException("TRANSFER_NOT_FOUND", "The document transfer was not found.");
        await EnsureVisibleAsync(session, job.OrganizationId, job.OperatingUnitId, cancellationToken);
        if (job.Status is not (DocumentTransferStatus.FAILED or DocumentTransferStatus.FAILED_AUTHENTICATION or DocumentTransferStatus.RETRY_PENDING))
            throw new OperationalException("TRANSFER_RETRY_NOT_ALLOWED", "This transfer is not ready for manual retry.");
        job.Status = DocumentTransferStatus.PENDING;
        job.NextAttemptAt = DateTime.UtcNow;
        job.LastErrorCode = null;
        job.LastErrorMessageSafe = null;
        job.UpdatedAt = DateTime.UtcNow;
        db.AuditEvents.Add(new AuditEvent
        {
            Id = Guid.NewGuid(),
            OrganizationId = job.OrganizationId,
            OperatingUnitId = job.OperatingUnitId,
            UserId = session.UserId,
            EventType = "DOCUMENT_TRANSFER_MANUAL_RETRY",
            EntityType = "DocumentTransferJob",
            EntityId = job.Id,
            Result = "SUCCESS",
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ProcessDueTransfersAsync(CancellationToken cancellationToken)
    {
        var jobs = await db.DocumentTransferJobs
            .Where(item => (item.Status == DocumentTransferStatus.PENDING || item.Status == DocumentTransferStatus.RETRY_PENDING) &&
                (item.NextAttemptAt == null || item.NextAttemptAt <= DateTime.UtcNow))
            .OrderBy(item => item.NextAttemptAt)
            .Take(10)
            .ToListAsync(cancellationToken);
        foreach (var job in jobs)
        {
            await ProcessOneAsync(job.Id, cancellationToken);
        }
    }

    private async Task ProcessOneAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await db.DocumentTransferJobs
            .Include(item => item.Document)
                .ThenInclude(item => item.Invoice)
                    .ThenInclude(item => item!.Supplier)
            .Include(item => item.Destination)
                .ThenInclude(item => item.StorageConnection)
            .SingleOrDefaultAsync(item => item.Id == jobId, cancellationToken);
        if (job is null || job.Status is DocumentTransferStatus.COMPLETED or DocumentTransferStatus.SKIPPED) return;

        job.Status = DocumentTransferStatus.PROCESSING;
        job.AttemptCount++;
        job.LastAttemptAt = DateTime.UtcNow;
        job.UpdatedAt = DateTime.UtcNow;
        db.AuditEvents.Add(new AuditEvent
        {
            Id = Guid.NewGuid(), OrganizationId = job.OrganizationId, OperatingUnitId = job.OperatingUnitId, UserId = job.UserId,
            EventType = "DOCUMENT_TRANSFER_PROCESSING", EntityType = "DocumentTransferJob", EntityId = job.Id,
            Result = "STARTED", CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync(cancellationToken);
        try
        {
            if (job.Destination.Status != StorageDestinationStatus.ACTIVE ||
                !job.Destination.ExternalTransferEnabled)
                throw new TransferFailure("DESTINATION_INACTIVE", "The external destination is no longer active.", false);
            if (job.Destination.StorageConnection.ConnectionStatus != StorageConnectionStatus.CONNECTED)
                throw new TransferFailure("MICROSOFT_CONNECTION_REQUIRED", "The Microsoft connection requires attention.", false, true);

            await using var content = await storage.GetAsync(job.Document.StorageReference, cancellationToken);
            var filename = BuildFilename(job.Document);
            var uploaded = job.Provider switch
            {
                DocumentStorageProvider.MICROSOFT => await microsoft.UploadFileAsync(
                    job.Destination.StorageConnectionId,
                    job.UserId,
                    job.Destination.SiteIdentifier ?? job.Destination.StorageConnection.SiteIdentifier!,
                    job.Destination.DriveIdentifier ?? job.Destination.StorageConnection.DriveIdentifier!,
                    job.Destination.FolderIdentifier ?? job.Destination.StorageConnection.FolderIdentifier!,
                    filename,
                    content,
                    cancellationToken),
                _ => throw new TransferFailure("PROVIDER_UNSUPPORTED", "This document provider is not enabled.", false),
            };
            job.Status = DocumentTransferStatus.COMPLETED;
            job.ExternalFileId = uploaded.Id;
            job.ExternalWebUrl = uploaded.WebUrl;
            job.ExternalFileName = uploaded.Name;
            job.CompletedAt = DateTime.UtcNow;
            job.NextAttemptAt = null;
            job.LastErrorCode = null;
            job.LastErrorMessageSafe = null;
            job.UpdatedAt = DateTime.UtcNow;
            db.AuditEvents.Add(new AuditEvent
            {
                Id = Guid.NewGuid(), OrganizationId = job.OrganizationId, OperatingUnitId = job.OperatingUnitId, UserId = job.UserId,
                EventType = "DOCUMENT_TRANSFER_COMPLETED", EntityType = "DocumentTransferJob", EntityId = job.Id,
                Reference = uploaded.Name, Result = "SUCCESS", CreatedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (TransferFailure failure)
        {
            await MarkFailureAsync(job, failure.Code, failure.Message, failure.Retryable, cancellationToken, failure.AuthenticationFailure);
        }
        catch (MicrosoftIntegrationException exception)
        {
            var authFailure = exception.Code is "CREDENTIAL_NOT_FOUND" or "CREDENTIAL_INVALID" or "GRAPH_FORBIDDEN" or "TOKEN_EXPIRED";
            await MarkFailureAsync(job, exception.Code, exception.Message, !authFailure, cancellationToken, authFailure);
        }
        catch (HttpRequestException exception)
        {
            await MarkFailureAsync(job, "GRAPH_NETWORK_ERROR", "Microsoft Graph was temporarily unavailable.", true, cancellationToken);
            logger.LogWarning(exception, "SharePoint transfer failed transiently for {JobId}", job.Id);
        }
        catch (FileNotFoundException exception)
        {
            await MarkFailureAsync(job, "DOCUMENT_CONTENT_NOT_FOUND", exception.Message, false, cancellationToken);
        }
    }

    private async Task MarkFailureAsync(
        DocumentTransferJob job,
        string code,
        string message,
        bool retryable,
        CancellationToken cancellationToken,
        bool authenticationFailure = false)
    {
        var permanent = !retryable || job.AttemptCount >= 5;
        job.Status = authenticationFailure
            ? DocumentTransferStatus.FAILED_AUTHENTICATION
            : permanent ? DocumentTransferStatus.FAILED : DocumentTransferStatus.RETRY_PENDING;
        var delayMinutes = Math.Min(60, Math.Pow(2, Math.Max(0, job.AttemptCount - 1)));
        job.NextAttemptAt = permanent ? null : DateTime.UtcNow.AddMinutes(delayMinutes);
        job.LastErrorCode = code;
        job.LastErrorMessageSafe = message.Length > 1000 ? message[..1000] : message;
        job.UpdatedAt = DateTime.UtcNow;
        db.AuditEvents.Add(new AuditEvent
        {
            Id = Guid.NewGuid(), OrganizationId = job.OrganizationId, OperatingUnitId = job.OperatingUnitId, UserId = job.UserId,
            EventType = authenticationFailure ? "DOCUMENT_TRANSFER_FAILED_AUTHENTICATION" : permanent ? "DOCUMENT_TRANSFER_FAILED" : "DOCUMENT_TRANSFER_RETRY_SCHEDULED",
            EntityType = "DocumentTransferJob", EntityId = job.Id, Reference = code, Result = job.Status.ToString(), CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureVisibleAsync(Session session, Guid organizationId, Guid? operatingUnitId, CancellationToken cancellationToken)
    {
        var visible = await db.UserOrganizationMemberships.AnyAsync(item =>
            item.UserId == session.UserId && item.OrganizationId == organizationId && item.Status == StatusKind.ACTIVE &&
            (operatingUnitId == null || item.OrganizationUnitId == null || item.OrganizationUnitId == operatingUnitId), cancellationToken);
        if (!visible) throw new OperationalException("ACCESS_SCOPE_DENIED", "The document is outside your accessible scope.");
    }

    private static string BuildFilename(Document document)
    {
        var invoice = document.Invoice;
        var supplier = SanitizePart(invoice?.Supplier?.SupplierCode);
        if (string.IsNullOrWhiteSpace(supplier)) supplier = SanitizePart(invoice?.SupplierNameRaw);
        var number = SanitizePart(invoice?.InvoiceNumber);
        var date = invoice?.InvoiceDate?.ToString("yyyyMMdd");
        var name = string.Join("_", new[] { supplier, number, date }.Where(item => !string.IsNullOrWhiteSpace(item)));
        return string.IsNullOrWhiteSpace(name) ? $"{document.Id:N}.pdf" : $"{name}.pdf";
    }

    private static string SanitizePart(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.StartsWith("PENDING-", StringComparison.OrdinalIgnoreCase)) return string.Empty;
        var cleaned = new string(value.Where(char.IsLetterOrDigit).ToArray());
        return cleaned.Length > 80 ? cleaned[..80] : cleaned;
    }

    private sealed class TransferFailure(string code, string message, bool retryable, bool authenticationFailure = false) : Exception(message)
    {
        public string Code { get; } = code;
        public bool Retryable { get; } = retryable;
        public bool AuthenticationFailure { get; } = authenticationFailure;
    }
}

public sealed class DocumentTransferWorker(
    TenantJobRunner jobs,
    ILogger<DocumentTransferWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await jobs.ForEachActiveEnvironmentAsync(async (services, context, token) =>
                {
                    logger.LogInformation("Document transfer worker Tenant={TenantId} Environment={EnvironmentId}", context.TenantId, context.EnvironmentId);
                    await services.GetRequiredService<DocumentRoutingService>().ProcessDueTransfersAsync(token);
                }, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception exception)
            {
                logger.LogError(exception, "Document transfer worker iteration failed.");
            }
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}