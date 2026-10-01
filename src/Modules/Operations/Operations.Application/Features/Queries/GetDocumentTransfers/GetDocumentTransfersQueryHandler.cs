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

namespace Operations.Application.Features.Queries.GetDocumentTransfers
{
    public class GetDocumentTransfersQueryHandler : IRequestHandler<GetDocumentTransfersQuery, DocumentTransfersResponseDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetDocumentTransfersQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<DocumentTransfersResponseDto> Handle(GetDocumentTransfersQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching document transfers. DocumentId: {request.DocumentId}, OrganizationId: {request.OrganizationId}");

            bool exists = await _repository.Document
                .FindByCondition(x => x.Id == request.DocumentId && x.OrganizationId == request.OrganizationId)
                .AnyAsync(cancellationToken);
            if (!exists)
            {
                _logger.LogError($"Document not found. DocumentId: {request.DocumentId}, OrganizationId: {request.OrganizationId}");
                throw new NotFoundCustomException("Document not found.", "The document does not exist in your organization.");
            }

            List<DocumentTransferJob> jobs = await _repository.DocumentTransferJob
                .FindByCondition(x => x.DocumentId == request.DocumentId)
                .OrderByDescending(x => x.DateCreated)
                .ToListAsync(cancellationToken);
            List<Guid> destinationIds = jobs.Select(x => x.DestinationId).Distinct().ToList();
            Dictionary<Guid, string> folderPaths = await _repository.DocumentStorageDestination
                .FindByCondition(x => destinationIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.FolderPath, cancellationToken);

            _logger.LogInfo($"Document transfers fetched. DocumentId: {request.DocumentId}, Count: {jobs.Count}");
            return new DocumentTransfersResponseDto
            {
                DocumentId = request.DocumentId,
                Transfers = jobs.Select(job => new DocumentTransferResponseDto
                {
                    Id = job.Id,
                    DocumentId = job.DocumentId,
                    DestinationId = job.DestinationId,
                    ExternalFileId = job.ExternalFileId,
                    Provider = job.Provider,
                    Status = job.Status,
                    ResolutionSource = job.ResolutionSource,
                    FolderPath = folderPaths.TryGetValue(job.DestinationId, out string? folderPath) ? folderPath : null,
                    ExternalFileName = job.ExternalFileName,
                    ExternalWebUrl = job.ExternalWebUrl,
                    AttemptCount = job.AttemptCount,
                    NextAttemptAt = job.NextAttemptAt,
                    CompletedAt = job.CompletedAt,
                    LastErrorCode = job.LastErrorCode,
                    LastErrorMessage = job.LastErrorMessageSafe
                }).ToList()
            };
        }
    }
}
