using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SilaMe.Api.Auth;
using SilaMe.Api.Data;
using SilaMe.Api.Models;
using SilaMe.Api.Services;
using Xunit;

namespace SilaMe.Api.Tests;

public sealed class DocumentTransferRoutingTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Store_destination_wins_over_organization_fallback_and_queue_is_idempotent()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SilaMeDbContext>();
        var routing = scope.ServiceProvider.GetRequiredService<DocumentRoutingService>();
        var now = DateTime.UtcNow;
        var user = NewUser(now);
        var organization = NewOrganization(now);
        var store = new OrganizationUnit
        {
            Id = Guid.NewGuid(), OrganizationId = organization.Id, Code = "ROUTING-STORE", Name = "Routing Store",
            Kind = OrganizationUnitKind.STORE, CreatedAt = now, UpdatedAt = now,
        };
        var connection = NewConnection(organization, user, now);
        var orgDestination = NewDestination(organization, connection, user, now, null);
        var storeDestination = NewDestination(organization, connection, user, now, store.Id);
        var document = new Document
        {
            Id = Guid.NewGuid(), OrganizationId = organization.Id, OperatingUnitId = store.Id, UploadedByUserId = user.Id,
            DocumentType = DocumentType.INVOICE, OriginalFilename = "routing.pdf", ContentType = "application/pdf",
            StorageProvider = "TEST", StorageReference = "missing-test-content", Status = DocumentStatus.UPLOADED,
            SourceChannel = DocumentSourceChannel.CLOUD_UPLOAD, CreatedAt = now, UpdatedAt = now,
        };
        db.AddRange(user, organization, store, connection, orgDestination, storeDestination, document);
        db.UserOrganizationMemberships.Add(new UserOrganizationMembership
        {
            Id = Guid.NewGuid(), UserId = user.Id, OrganizationId = organization.Id, OrganizationUnitId = store.Id, CreatedAt = now,
        });
        await db.SaveChangesAsync();

        var resolved = await routing.ResolveDestinationAsync(user.Id, organization.Id, store.Id, DocumentType.INVOICE, CancellationToken.None);
        Assert.Equal(storeDestination.Id, resolved?.Destination.Id);
        Assert.Equal("STORE", resolved?.ResolutionSource);

        var session = new Session { Id = Guid.NewGuid(), UserId = user.Id, Application = ApplicationKind.CLOUD, TokenHash = "test", CreatedAt = now, ExpiresAt = now.AddHours(1), LastUsedAt = now };
        await routing.QueueTransferAsync(document, session, CancellationToken.None);
        await routing.QueueTransferAsync(document, session, CancellationToken.None);
        Assert.Equal(1, await db.DocumentTransferJobs.CountAsync(item => item.DocumentId == document.Id && item.DestinationId == storeDestination.Id));
        Assert.Equal(storeDestination.Id, (await db.DocumentTransferJobs.SingleAsync(item => item.DocumentId == document.Id)).DestinationId);
    }

    [Fact]
    public async Task Disconnected_connection_marks_due_job_as_authentication_failure()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SilaMeDbContext>();
        var routing = scope.ServiceProvider.GetRequiredService<DocumentRoutingService>();
        var now = DateTime.UtcNow;
        var user = NewUser(now);
        var organization = NewOrganization(now);
        var connection = NewConnection(organization, user, now);
        connection.ConnectionStatus = StorageConnectionStatus.DISCONNECTED;
        var destination = NewDestination(organization, connection, user, now, null);
        var document = new Document
        {
            Id = Guid.NewGuid(), OrganizationId = organization.Id, UploadedByUserId = user.Id, DocumentType = DocumentType.INVOICE,
            OriginalFilename = "auth-failure.pdf", ContentType = "application/pdf", StorageProvider = "TEST",
            StorageReference = "not-read", Status = DocumentStatus.UPLOADED, SourceChannel = DocumentSourceChannel.MOBILE_UPLOAD,
            CreatedAt = now, UpdatedAt = now,
        };
        var job = new DocumentTransferJob
        {
            Id = Guid.NewGuid(), DocumentId = document.Id, DestinationId = destination.Id, UserId = user.Id, OrganizationId = organization.Id,
            DocumentType = DocumentType.INVOICE, Provider = DocumentStorageProvider.MICROSOFT, Status = DocumentTransferStatus.PENDING,
            NextAttemptAt = now.AddMinutes(-1), CreatedAt = now, UpdatedAt = now,
        };
        db.AddRange(user, organization, connection, destination, document, job);
        await db.SaveChangesAsync();

        await routing.ProcessDueTransfersAsync(CancellationToken.None);

        Assert.Equal(DocumentTransferStatus.FAILED_AUTHENTICATION, (await db.DocumentTransferJobs.SingleAsync(item => item.Id == job.Id)).Status);
        Assert.Equal("MICROSOFT_CONNECTION_REQUIRED", (await db.DocumentTransferJobs.SingleAsync(item => item.Id == job.Id)).LastErrorCode);
    }

    [Fact]
    public async Task Manual_retry_requeues_failed_job_only_with_visible_scope()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SilaMeDbContext>();
        var routing = scope.ServiceProvider.GetRequiredService<DocumentRoutingService>();
        var now = DateTime.UtcNow;
        var user = NewUser(now);
        var organization = NewOrganization(now);
        var connection = NewConnection(organization, user, now);
        var destination = NewDestination(organization, connection, user, now, null);
        var document = new Document
        {
            Id = Guid.NewGuid(), OrganizationId = organization.Id, UploadedByUserId = user.Id, DocumentType = DocumentType.INVOICE,
            OriginalFilename = "retry.pdf", ContentType = "application/pdf", StorageProvider = "TEST", StorageReference = "retry",
            Status = DocumentStatus.UPLOADED, SourceChannel = DocumentSourceChannel.CLOUD_UPLOAD, CreatedAt = now, UpdatedAt = now,
        };
        var job = new DocumentTransferJob
        {
            Id = Guid.NewGuid(), DocumentId = document.Id, DestinationId = destination.Id, UserId = user.Id, OrganizationId = organization.Id,
            DocumentType = DocumentType.INVOICE, Provider = DocumentStorageProvider.MICROSOFT, Status = DocumentTransferStatus.FAILED,
            LastErrorCode = "GRAPH_UPLOAD_FAILED", CreatedAt = now, UpdatedAt = now,
        };
        db.AddRange(user, organization, connection, destination, document, job);
        db.UserOrganizationMemberships.Add(new UserOrganizationMembership
        {
            Id = Guid.NewGuid(), UserId = user.Id, OrganizationId = organization.Id, CreatedAt = now,
        });
        await db.SaveChangesAsync();

        await routing.RetryAsync(new Session { Id = Guid.NewGuid(), UserId = user.Id, Application = ApplicationKind.CLOUD, TokenHash = "test", CreatedAt = now, ExpiresAt = now.AddHours(1), LastUsedAt = now }, job.Id, CancellationToken.None);

        var saved = await db.DocumentTransferJobs.SingleAsync(item => item.Id == job.Id);
        Assert.Equal(DocumentTransferStatus.PENDING, saved.Status);
        Assert.Null(saved.LastErrorCode);
    }

    [Fact]
    public async Task Worker_uploads_stored_pdf_to_the_selected_graph_destination_and_persists_safe_metadata()
    {
        factory.GraphHandler.Reset();
        factory.GraphHandler.EnqueueResponse(HttpStatusCode.OK,
            """{"id":"graph-file-a","name":"AcmeFoods_INV42_20260916.pdf","webUrl":"https://sharepoint.test/sites/a/INV42.pdf"}""");

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SilaMeDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IDocumentStorageService>();
        var routing = scope.ServiceProvider.GetRequiredService<DocumentRoutingService>();
        var now = DateTime.UtcNow;
        var user = NewUser(now);
        var organization = NewOrganization(now);
        var connection = NewConnection(organization, user, now);
        connection.CredentialReference = ProtectToken("graph-token-a", now.AddHours(1));
        connection.SiteIdentifier = "site-a";
        connection.DriveIdentifier = "drive-a";
        connection.FolderIdentifier = "folder-a";
        var destination = NewDestination(organization, connection, user, now, null);
        destination.SiteIdentifier = "site-a";
        destination.DriveIdentifier = "drive-a";
        destination.FolderIdentifier = "folder-a";
        var pdf = CreatePdfFixture();
        var stored = await storage.StoreAsync(new MemoryStream(pdf), "known.pdf", CancellationToken.None);
        var document = NewDocument(organization, user, stored, now, "known.pdf");
        var invoice = NewInvoice(document, user, now, "Acme Foods", "INV/42");
        db.AddRange(user, organization, connection, destination, document, invoice);
        db.UserOrganizationMemberships.Add(Membership(user, organization, now));
        await db.SaveChangesAsync();

        var session = NewSession(user, now);
        await routing.QueueTransferAsync(document, session, CancellationToken.None);
        await routing.QueueTransferAsync(document, session, CancellationToken.None);
        await routing.ProcessDueTransfersAsync(CancellationToken.None);

        var request = Assert.Single(factory.GraphHandler.Requests);
        Assert.Equal(HttpMethod.Put, request.Method);
        Assert.Equal(
            "https://graph.microsoft.com/v1.0/drives/drive-a/items/folder-a:/AcmeFoods_INV42_20260916.pdf:/content",
            request.Uri.ToString());
        Assert.Equal(pdf, request.Content);
        Assert.StartsWith("%PDF", Encoding.ASCII.GetString(request.Content));

        var job = await db.DocumentTransferJobs.SingleAsync(item => item.DocumentId == document.Id);
        Assert.Equal(DocumentTransferStatus.COMPLETED, job.Status);
        Assert.Equal("graph-file-a", job.ExternalFileId);
        Assert.Equal("https://sharepoint.test/sites/a/INV42.pdf", job.ExternalWebUrl);
        Assert.Equal("AcmeFoods_INV42_20260916.pdf", job.ExternalFileName);
        Assert.Equal(1, await db.DocumentTransferJobs.CountAsync(item => item.DocumentId == document.Id));

        var response = await routing.GetTransfersAsync(session, document.Id, CancellationToken.None);
        var serialized = JsonSerializer.Serialize(response);
        Assert.Contains("graph-file-a", serialized);
        Assert.Contains("sharepoint.test", serialized);
        Assert.DoesNotContain("graph-token-a", serialized);
        Assert.DoesNotContain("TokenEncryptionKey", serialized);
        Assert.DoesNotContain("access_token", serialized, StringComparison.OrdinalIgnoreCase);

        var sessions = scope.ServiceProvider.GetRequiredService<ISessionService>();
        var (_, rawToken) = await sessions.CreateAsync(user, ApplicationKind.CLOUD, CancellationToken.None);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Cookie", $"sila_me_session={rawToken}");
        client.DefaultRequestHeaders.Add("X-Sila-Route-Slug", "sila-dev");
        var transferResponse = await client.GetAsync($"/api/v1/documents/{document.Id}/transfers");
        Assert.Equal(HttpStatusCode.OK, transferResponse.StatusCode);
        var transferJson = await transferResponse.Content.ReadAsStringAsync();
        Assert.Contains("graph-file-a", transferJson);
        Assert.DoesNotContain("graph-token-a", transferJson);
        Assert.DoesNotContain("TokenEncryptionKey", transferJson);
        Assert.DoesNotContain("access_token", transferJson, StringComparison.OrdinalIgnoreCase);

        await using var storedContent = await storage.GetAsync(stored.Reference, CancellationToken.None);
        using var storedMemory = new MemoryStream();
        await storedContent.CopyToAsync(storedMemory);
        Assert.Equal(pdf, storedMemory.ToArray());
    }

    [Fact]
    public async Task Worker_keeps_user_destinations_isolated_and_does_not_reupload_completed_jobs()
    {
        factory.GraphHandler.Reset();
        factory.GraphHandler.EnqueueResponse(HttpStatusCode.OK,
            """{"id":"graph-file-user-a","name":"user-a.pdf","webUrl":"https://sharepoint.test/a/user-a.pdf"}""");
        factory.GraphHandler.EnqueueResponse(HttpStatusCode.OK,
            """{"id":"graph-file-user-b","name":"user-b.pdf","webUrl":"https://sharepoint.test/b/user-b.pdf"}""");

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SilaMeDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IDocumentStorageService>();
        var routing = scope.ServiceProvider.GetRequiredService<DocumentRoutingService>();
        var now = DateTime.UtcNow;
        var organization = NewOrganization(now);
        var userA = NewUser(now);
        var userB = NewUser(now);
        var connectionA = NewConnection(organization, userA, now);
        connectionA.Name = "Routing SharePoint A";
        connectionA.CredentialReference = ProtectToken("graph-token-a", now.AddHours(1));
        connectionA.SiteIdentifier = "site-a";
        connectionA.DriveIdentifier = "drive-a";
        connectionA.FolderIdentifier = "folder-a";
        var connectionB = NewConnection(organization, userB, now);
        connectionB.Name = "Routing SharePoint B";
        connectionB.CredentialReference = ProtectToken("graph-token-b", now.AddHours(1));
        connectionB.SiteIdentifier = "site-b";
        connectionB.DriveIdentifier = "drive-b";
        connectionB.FolderIdentifier = "folder-b";
        var destinationA = NewDestination(organization, connectionA, userA, now, null);
        destinationA.SiteIdentifier = "site-a";
        destinationA.DriveIdentifier = "drive-a";
        destinationA.FolderIdentifier = "folder-a";
        var destinationB = NewDestination(organization, connectionB, userB, now, null);
        destinationB.SiteIdentifier = "site-b";
        destinationB.DriveIdentifier = "drive-b";
        destinationB.FolderIdentifier = "folder-b";
        var storedA = await storage.StoreAsync(new MemoryStream(CreatePdfFixture()), "a.pdf", CancellationToken.None);
        var storedB = await storage.StoreAsync(new MemoryStream(CreatePdfFixture()), "b.pdf", CancellationToken.None);
        var documentA = NewDocument(organization, userA, storedA, now, "a.pdf");
        var documentB = NewDocument(organization, userB, storedB, now, "b.pdf");
        db.AddRange(organization, userA, userB, connectionA, connectionB, destinationA, destinationB, documentA, documentB);
        db.UserOrganizationMemberships.AddRange(Membership(userA, organization, now), Membership(userB, organization, now));
        db.UserDocumentStorageAssignments.AddRange(
            new UserDocumentStorageAssignment
            {
                Id = Guid.NewGuid(), UserId = userA.Id, DocumentStorageDestinationId = destinationA.Id,
                Provider = DocumentStorageProvider.MICROSOFT, ExternalTransferEnabled = true,
                Status = StorageAssignmentStatus.VALIDATED, CreatedByUserId = userA.Id, CreatedAt = now, UpdatedAt = now,
            },
            new UserDocumentStorageAssignment
            {
                Id = Guid.NewGuid(), UserId = userB.Id, DocumentStorageDestinationId = destinationB.Id,
                Provider = DocumentStorageProvider.MICROSOFT, ExternalTransferEnabled = true,
                Status = StorageAssignmentStatus.VALIDATED, CreatedByUserId = userB.Id, CreatedAt = now, UpdatedAt = now,
            });
        await db.SaveChangesAsync();

        await routing.QueueTransferAsync(documentA, NewSession(userA, now), CancellationToken.None);
        await routing.QueueTransferAsync(documentB, NewSession(userB, now), CancellationToken.None);
        await routing.ProcessDueTransfersAsync(CancellationToken.None);
        await routing.ProcessDueTransfersAsync(CancellationToken.None);

        var requests = factory.GraphHandler.Requests;
        Assert.Equal(2, requests.Count);
        Assert.Contains(requests, request => request.Uri.AbsoluteUri.Contains("/drives/drive-a/items/folder-a:", StringComparison.Ordinal));
        Assert.Contains(requests, request => request.Uri.AbsoluteUri.Contains("/drives/drive-b/items/folder-b:", StringComparison.Ordinal));
        Assert.Equal(1, await db.DocumentTransferJobs.CountAsync(item => item.DocumentId == documentA.Id));
        Assert.Equal(1, await db.DocumentTransferJobs.CountAsync(item => item.DocumentId == documentB.Id));
        Assert.All(await db.DocumentTransferJobs.Where(item => item.DocumentId == documentA.Id || item.DocumentId == documentB.Id).ToListAsync(),
            job => Assert.Equal(DocumentTransferStatus.COMPLETED, job.Status));
    }

    [Fact]
    public async Task Worker_retries_transient_graph_failure_without_losing_sila_document()
    {
        factory.GraphHandler.Reset();
        factory.GraphHandler.EnqueueResponse(HttpStatusCode.ServiceUnavailable, """{"error":"temporary"}""");
        factory.GraphHandler.EnqueueResponse(HttpStatusCode.OK,
            """{"id":"graph-file-retry","name":"retry.pdf","webUrl":"https://sharepoint.test/retry.pdf"}""");

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SilaMeDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IDocumentStorageService>();
        var routing = scope.ServiceProvider.GetRequiredService<DocumentRoutingService>();
        var now = DateTime.UtcNow;
        var user = NewUser(now);
        var organization = NewOrganization(now);
        var connection = NewConnection(organization, user, now);
        connection.CredentialReference = ProtectToken("retry-token", now.AddHours(1));
        var destination = NewDestination(organization, connection, user, now, null);
        var pdf = CreatePdfFixture();
        var stored = await storage.StoreAsync(new MemoryStream(pdf), "retry.pdf", CancellationToken.None);
        var document = NewDocument(organization, user, stored, now, "retry.pdf");
        db.AddRange(user, organization, connection, destination, document);
        db.UserOrganizationMemberships.Add(Membership(user, organization, now));
        await db.SaveChangesAsync();

        await routing.QueueTransferAsync(document, NewSession(user, now), CancellationToken.None);
        await routing.ProcessDueTransfersAsync(CancellationToken.None);
        var failed = await db.DocumentTransferJobs.SingleAsync(item => item.DocumentId == document.Id);
        Assert.Equal(DocumentTransferStatus.RETRY_PENDING, failed.Status);
        failed.NextAttemptAt = DateTime.UtcNow.AddMinutes(-1);
        await db.SaveChangesAsync();

        await routing.ProcessDueTransfersAsync(CancellationToken.None);

        var completed = await db.DocumentTransferJobs.SingleAsync(item => item.Id == failed.Id);
        Assert.Equal(DocumentTransferStatus.COMPLETED, completed.Status);
        Assert.Equal(2, factory.GraphHandler.Requests.Count);
        await using var storedContent = await storage.GetAsync(stored.Reference, CancellationToken.None);
        Assert.Equal(pdf, await ReadBytesAsync(storedContent));
    }

    [Fact]
    public async Task Worker_marks_graph_auth_failure_without_losing_sila_document()
    {
        factory.GraphHandler.Reset();
        factory.GraphHandler.EnqueueResponse(HttpStatusCode.Unauthorized, """{"error":"expired"}""");

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SilaMeDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IDocumentStorageService>();
        var routing = scope.ServiceProvider.GetRequiredService<DocumentRoutingService>();
        var now = DateTime.UtcNow;
        var user = NewUser(now);
        var organization = NewOrganization(now);
        var connection = NewConnection(organization, user, now);
        connection.CredentialReference = ProtectToken("expired-token", now.AddHours(1));
        var destination = NewDestination(organization, connection, user, now, null);
        var pdf = CreatePdfFixture();
        var stored = await storage.StoreAsync(new MemoryStream(pdf), "auth.pdf", CancellationToken.None);
        var document = NewDocument(organization, user, stored, now, "auth.pdf");
        db.AddRange(user, organization, connection, destination, document);
        db.UserOrganizationMemberships.Add(Membership(user, organization, now));
        await db.SaveChangesAsync();

        await routing.QueueTransferAsync(document, NewSession(user, now), CancellationToken.None);
        await routing.ProcessDueTransfersAsync(CancellationToken.None);

        var job = await db.DocumentTransferJobs.SingleAsync(item => item.DocumentId == document.Id);
        Assert.Equal(DocumentTransferStatus.FAILED_AUTHENTICATION, job.Status);
        Assert.Equal("TOKEN_EXPIRED", job.LastErrorCode);
        Assert.Equal(1, await db.DocumentTransferJobs.CountAsync(item => item.DocumentId == document.Id));
        await using var storedContent = await storage.GetAsync(stored.Reference, CancellationToken.None);
        Assert.Equal(pdf, await ReadBytesAsync(storedContent));
    }

    private static User NewUser(DateTime now)
    {
        var id = Guid.NewGuid();
        return new User { Id = id, Email = $"routing-{id:N}@silame.local", NormalizedEmail = $"ROUTING-{id:N}@SILAME.LOCAL", DisplayName = "Routing Test", PasswordHash = "unused", CreatedAt = now, UpdatedAt = now };
    }

    private static Organization NewOrganization(DateTime now)
    {
        var id = Guid.NewGuid();
        return new Organization { Id = id, Code = $"ROUTE-{id:N}"[..16].ToUpperInvariant(), Name = "Routing Test Organization", Kind = OrganizationKind.CUSTOMER, CreatedAt = now, UpdatedAt = now };
    }

    private static DocumentStorageConnection NewConnection(Organization organization, User user, DateTime now) =>
        new()
        {
            Id = Guid.NewGuid(), OrganizationId = organization.Id, Provider = DocumentStorageProvider.MICROSOFT,
            Name = "Routing SharePoint", ConnectionStatus = StorageConnectionStatus.CONNECTED, SiteIdentifier = "site",
            DriveIdentifier = "drive", FolderIdentifier = "folder", FolderPath = "/Invoices", DisplayName = "Invoices",
            CreatedByUserId = user.Id, CreatedAt = now, UpdatedAt = now,
        };

    private static DocumentStorageDestination NewDestination(Organization organization, DocumentStorageConnection connection, User user, DateTime now, Guid? operatingUnitId) =>
        new()
        {
            Id = Guid.NewGuid(), OrganizationId = organization.Id, OperatingUnitId = operatingUnitId,
            StorageConnectionId = connection.Id, Provider = DocumentStorageProvider.MICROSOFT, DocumentType = DocumentType.INVOICE,
            FolderPath = operatingUnitId is null ? "/Invoices" : "/Invoices/Store", SiteIdentifier = "site", DriveIdentifier = "drive",
            FolderIdentifier = "folder", ExternalTransferEnabled = true, Status = StorageDestinationStatus.ACTIVE,
            ValidatedAt = now, CreatedByUserId = user.Id, CreatedAt = now, UpdatedAt = now,
        };

    private static Document NewDocument(Organization organization, User user, StoredDocument stored, DateTime now, string filename) =>
        new()
        {
            Id = Guid.NewGuid(), OrganizationId = organization.Id, UploadedByUserId = user.Id, DocumentType = DocumentType.INVOICE,
            OriginalFilename = filename, ContentType = "application/pdf", FileSizeBytes = stored.Size,
            StorageProvider = stored.Provider, StorageReference = stored.Reference, Status = DocumentStatus.UPLOADED,
            SourceChannel = DocumentSourceChannel.MOBILE_UPLOAD, CreatedAt = now, UpdatedAt = now,
        };

    private static Invoice NewInvoice(Document document, User user, DateTime now, string supplier, string number) =>
        new()
        {
            Id = Guid.NewGuid(), OrganizationId = document.OrganizationId, DocumentId = document.Id,
            InvoiceNumber = number, InvoiceDate = new DateOnly(2026, 9, 16), SupplierNameRaw = supplier,
            InvoiceType = InvoiceType.MATERIAL, Status = InvoiceStatus.UPLOADED,
            CreatedByUserId = user.Id, CreatedAt = now, UpdatedAt = now,
        };

    private static UserOrganizationMembership Membership(User user, Organization organization, DateTime now) =>
        new()
        {
            Id = Guid.NewGuid(), UserId = user.Id, OrganizationId = organization.Id,
            CreatedAt = now, Status = StatusKind.ACTIVE,
        };

    private static Session NewSession(User user, DateTime now) =>
        new()
        {
            Id = Guid.NewGuid(), UserId = user.Id, Application = ApplicationKind.MOBILE, TokenHash = $"test-{user.Id:N}",
            CreatedAt = now, ExpiresAt = now.AddHours(1), LastUsedAt = now,
        };

    private static string ProtectToken(string accessToken, DateTime expiresAt)
    {
        var plaintext = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
        {
            AccessToken = accessToken,
            RefreshToken = (string?)null,
            ExpiresAt = expiresAt,
        }));
        var key = SHA256.HashData(Encoding.UTF8.GetBytes("test-token-encryption-key"));
        var nonce = Enumerable.Range(1, 12).Select(item => (byte)item).ToArray();
        var tag = new byte[16];
        var ciphertext = new byte[plaintext.Length];
        using var aes = new AesGcm(key, 16);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);
        return $"v1:{Base64Url(nonce)}:{Base64Url(tag)}:{Base64Url(ciphertext)}";
    }

    private static string Base64Url(byte[] value) =>
        Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] CreatePdfFixture()
    {
        var objects = new[]
        {
            "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n",
            "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n",
            "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 10 10] /Contents 4 0 R >>\nendobj\n",
            "4 0 obj\n<< /Length 0 >>\nstream\n\nendstream\nendobj\n",
        };
        using var output = new MemoryStream();
        using var writer = new StreamWriter(output, Encoding.ASCII, 1024, leaveOpen: true);
        writer.Write("%PDF-1.4\n");
        writer.Flush();
        var offsets = new List<long> { 0 };
        foreach (var item in objects)
        {
            offsets.Add(output.Position);
            writer.Write(item);
            writer.Flush();
        }
        var xrefOffset = output.Position;
        writer.Write($"xref\n0 {offsets.Count}\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1)) writer.Write($"{offset:0000000000} 00000 n \n");
        writer.Write($"trailer\n<< /Size {offsets.Count} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF\n");
        writer.Flush();
        return output.ToArray();
    }

    private static async Task<byte[]> ReadBytesAsync(Stream stream)
    {
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory);
        return memory.ToArray();
    }
}