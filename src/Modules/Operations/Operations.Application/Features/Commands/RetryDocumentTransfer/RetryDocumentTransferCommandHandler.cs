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

namespace Operations.Application.Features.Commands.RetryDocumentTransfer
{
    public class RetryDocumentTransferCommandHandler : IRequestHandler<RetryDocumentTransferCommand, DocumentTransferRetryResponseDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public RetryDocumentTransferCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<DocumentTransferRetryResponseDto> Handle(RetryDocumentTransferCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Retrying document transfer. TransferId: {request.TransferId}, OrganizationId: {request.OrganizationId}, UserId: {request.UserId}");

            DocumentTransferJob? job = await _repository.DocumentTransferJob.FindFirstByConditionAsync(x =>
                x.Id == request.TransferId && x.OrganizationId == request.OrganizationId);
            if (job == null)
            {
                // The id of a destination was sent: its most recent transfer that can be retried is taken.
                Guid latestJobId = await _repository.DocumentTransferJob
                    .FindByCondition(x => x.DestinationId == request.TransferId && x.OrganizationId == request.OrganizationId
                        && (x.Status == DocumentTransferStatus.FAILED
                            || x.Status == DocumentTransferStatus.FAILED_AUTHENTICATION
                            || x.Status == DocumentTransferStatus.RETRY_PENDING))
                    .OrderByDescending(x => x.DateUpdated)
                    .Select(x => x.Id)
                    .FirstOrDefaultAsync(cancellationToken);
                job = latestJobId == Guid.Empty
                    ? null
                    : await _repository.DocumentTransferJob.FindFirstByConditionAsync(x => x.Id == latestJobId);
            }

            if (job == null)
            {
                _logger.LogError($"Document transfer not found. TransferId: {request.TransferId}, OrganizationId: {request.OrganizationId}");
                throw new NotFoundCustomException("Document transfer not found.", "There is no failed transfer with this id in your organization.");
            }

            if (job.Status is not (DocumentTransferStatus.FAILED or DocumentTransferStatus.FAILED_AUTHENTICATION or DocumentTransferStatus.RETRY_PENDING))
            {
                _logger.LogError($"Document transfer cannot be retried. TransferId: {job.Id}, Status: {job.Status}");
                throw new BadRequestCustomException("Transfer retry is not allowed.", "Only a failed transfer can be retried.");
            }

            job.Status = DocumentTransferStatus.PENDING;
            job.NextAttemptAt = DateTime.UtcNow;
            job.LastErrorCode = null;
            job.LastErrorMessageSafe = null;
            AuditTrail.Add(_repository, job.OrganizationId, job.OperatingUnitId, request.UserId, "DOCUMENT_TRANSFER_MANUAL_RETRY", "DocumentTransferJob", job.Id, null, "SUCCESS");
            await _repository.SaveAsync();

            _logger.LogInfo($"Document transfer queued again. TransferId: {job.Id}");
            return new DocumentTransferRetryResponseDto { TransferId = job.Id, Status = job.Status };
        }
    }
}
