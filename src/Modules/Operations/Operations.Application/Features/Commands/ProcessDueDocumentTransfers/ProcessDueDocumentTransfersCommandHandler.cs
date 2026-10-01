using MediatR;
using Microsoft.EntityFrameworkCore;
using Operations.Application.Features.Shared;
using Operations.Application.Services.Graph;
using Operations.Application.Services.Storage;
using Operations.Domain.Common;
using Operations.Domain.Dtos;
using Operations.Domain.Entities;
using Operations.Domain.Enums;
using Operations.Infrastructure.Contracts.IRepository;
using Operations.Infrastructure.Contracts.IServices;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Operations.Application.Features.Commands.ProcessDueDocumentTransfers
{
    public class ProcessDueDocumentTransfersCommandHandler : IRequestHandler<ProcessDueDocumentTransfersCommand, int>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly IDocumentStorageService _storage;
        private readonly IMicrosoftGraphClient _graph;
        private readonly IUserContext _userContext;

        public ProcessDueDocumentTransfersCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            IDocumentStorageService storage,
            IMicrosoftGraphClient graph,
            IUserContext userContext)
        {
            _repository = repository;
            _logger = logger;
            _storage = storage;
            _graph = graph;
            _userContext = userContext;
        }

        public async Task<int> Handle(ProcessDueDocumentTransfersCommand request, CancellationToken cancellationToken)
        {
            List<DocumentTransferJob> due = await _repository.DocumentTransferJob.ListDueAsync(DateTime.UtcNow, 10, cancellationToken);
            if (due.Count == 0)
            {
                return 0;
            }

            _logger.LogInfo($"Processing due document transfers. Count: {due.Count}");
            foreach (DocumentTransferJob item in due)
            {
                await ProcessOneAsync(item.Id, cancellationToken);
            }

            _logger.LogInfo($"Due document transfers processed. Count: {due.Count}");
            return due.Count;
        }

        private async Task ProcessOneAsync(Guid jobId, CancellationToken cancellationToken)
        {
            DocumentTransferJob? job = await _repository.DocumentTransferJob.GetTrackedAsync(jobId, cancellationToken);
            if (job == null || job.Status is DocumentTransferStatus.COMPLETED or DocumentTransferStatus.SKIPPED)
            {
                return;
            }

            // There is no HTTP user in the worker: the audit columns carry the user who saved the document.
            _userContext.SetCurrentUserId(job.UserId);
            job.Status = DocumentTransferStatus.PROCESSING;
            job.AttemptCount++;
            job.LastAttemptAt = DateTime.UtcNow;
            await _repository.SaveAsync();

            try
            {
                DocumentStorageDestination? destination = await _repository.DocumentStorageDestination
                    .FindByCondition(x => x.Id == job.DestinationId && x.OrganizationId == job.OrganizationId)
                    .FirstOrDefaultAsync(cancellationToken);
                if (destination == null || destination.Status != StorageDestinationStatus.ACTIVE || !destination.ExternalTransferEnabled)
                {
                    MarkFailure(job, "DESTINATION_INACTIVE", "The external destination is no longer active.", false, false);
                    await _repository.SaveAsync();
                    return;
                }

                DocumentStorageConnection? connection = await _repository.DocumentStorageConnection.FindFirstByConditionAsync(x =>
                    x.Id == destination.StorageConnectionId && x.OrganizationId == job.OrganizationId);
                if (connection == null || connection.ConnectionStatus != StorageConnectionStatus.CONNECTED)
                {
                    MarkFailure(job, "MICROSOFT_CONNECTION_REQUIRED", "The Microsoft connection requires attention.", false, true);
                    await _repository.SaveAsync();
                    return;
                }

                if (job.Provider != DocumentStorageProvider.MICROSOFT)
                {
                    MarkFailure(job, "PROVIDER_UNSUPPORTED", "This document provider is not enabled.", false, false);
                    await _repository.SaveAsync();
                    return;
                }

                byte[] content = await _storage.ReadAsync(job.Document.StorageReference, cancellationToken);
                Invoice? invoice = await _repository.Invoice
                    .FindByCondition(x => x.DocumentId == job.DocumentId)
                    .FirstOrDefaultAsync(cancellationToken);
                string accessToken = await MicrosoftStorageWorkflow.GetAccessTokenAsync(_graph, connection, cancellationToken);
                GraphDriveItem uploaded = await _graph.UploadFileAsync(
                    accessToken,
                    destination.DriveIdentifier ?? connection.DriveIdentifier ?? string.Empty,
                    destination.FolderIdentifier ?? connection.FolderIdentifier ?? string.Empty,
                    BuildFileName(job.Document, invoice),
                    content,
                    cancellationToken);

                job.Status = DocumentTransferStatus.COMPLETED;
                job.ExternalFileId = uploaded.Id;
                job.ExternalWebUrl = uploaded.WebUrl;
                job.ExternalFileName = uploaded.Name;
                job.CompletedAt = DateTime.UtcNow;
                job.NextAttemptAt = null;
                job.LastErrorCode = null;
                job.LastErrorMessageSafe = null;
                AuditTrail.Add(_repository, job.OrganizationId, job.OperatingUnitId, job.UserId, "DOCUMENT_TRANSFER_COMPLETED", "DocumentTransferJob", job.Id, uploaded.Name, "SUCCESS");
                _logger.LogInfo($"Document transferred. TransferId: {job.Id}, DocumentId: {job.DocumentId}");
            }
            catch (MicrosoftGraphException exception)
            {
                bool authenticationFailure = MicrosoftStorageWorkflow.IsAuthenticationFailure(exception);
                MarkFailure(job, exception.Code, exception.Message, !authenticationFailure, authenticationFailure);
            }
            catch (HttpRequestException exception)
            {
                MarkFailure(job, "GRAPH_NETWORK_ERROR", "Microsoft Graph was temporarily unavailable.", true, false);
                _logger.LogError($"Document transfer failed on the network. TransferId: {job.Id}, Error: {exception.Message}");
            }
            catch (FileNotFoundException)
            {
                MarkFailure(job, "DOCUMENT_CONTENT_NOT_FOUND", "The stored file of this document could not be found.", false, false);
            }

            await _repository.SaveAsync();
        }

        // Retries back off 1, 2, 4, 8 minutes; the fifth failure (or a failure that cannot be retried) is final.
        private void MarkFailure(DocumentTransferJob job, string code, string message, bool retryable, bool authenticationFailure)
        {
            bool permanent = !retryable || job.AttemptCount >= 5;
            job.Status = authenticationFailure
                ? DocumentTransferStatus.FAILED_AUTHENTICATION
                : permanent ? DocumentTransferStatus.FAILED : DocumentTransferStatus.RETRY_PENDING;
            job.NextAttemptAt = permanent ? null : DateTime.UtcNow.AddMinutes(Math.Min(60, Math.Pow(2, Math.Max(0, job.AttemptCount - 1))));
            job.LastErrorCode = code;
            job.LastErrorMessageSafe = message.Length > 1000 ? message[..1000] : message;
            AuditTrail.Add(_repository, job.OrganizationId, job.OperatingUnitId, job.UserId,
                authenticationFailure ? "DOCUMENT_TRANSFER_FAILED_AUTHENTICATION" : permanent ? "DOCUMENT_TRANSFER_FAILED" : "DOCUMENT_TRANSFER_RETRY_SCHEDULED",
                "DocumentTransferJob", job.Id, code, job.Status.ToString());
            _logger.LogError($"Document transfer failed. TransferId: {job.Id}, Code: {code}, Status: {job.Status}");
        }

        private static string BuildFileName(Document document, Invoice? invoice)
        {
            string extension = Path.GetExtension(document.OriginalFilename);
            if (string.IsNullOrWhiteSpace(extension))
            {
                extension = ".pdf";
            }

            string name = string.Join("_", new[]
            {
                Sanitize(invoice?.SupplierNameRaw),
                Sanitize(invoice?.InvoiceNumber),
                invoice?.InvoiceDate?.ToString("yyyyMMdd") ?? string.Empty
            }.Where(part => !string.IsNullOrWhiteSpace(part)));
            return string.IsNullOrWhiteSpace(name) ? $"{document.Id:N}{extension}" : $"{name}{extension}";
        }

        private static string Sanitize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value) || InvoiceWorkflow.IsPendingNumber(value))
            {
                return string.Empty;
            }

            string cleaned = new string(value.Where(char.IsLetterOrDigit).ToArray());
            return cleaned.Length > 80 ? cleaned[..80] : cleaned;
        }
    }
}
