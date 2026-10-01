using MediatR;
using Microsoft.EntityFrameworkCore;
using Operations.Application.Features.Shared;
using Operations.Application.Services.Storage;
using Operations.Domain.Common;
using Operations.Domain.Dtos;
using Operations.Domain.Entities;
using Operations.Domain.Enums;
using Operations.Infrastructure.Contracts.IRepository;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Operations.Application.Features.Queries.GetDocumentContent
{
    public class GetDocumentContentQueryHandler : IRequestHandler<GetDocumentContentQuery, FileDownloadDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly IDocumentStorageService _storage;

        public GetDocumentContentQueryHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            IDocumentStorageService storage)
        {
            _repository = repository;
            _logger = logger;
            _storage = storage;
        }

        public async Task<FileDownloadDto> Handle(GetDocumentContentQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching document content. DocumentId: {request.DocumentId}, OrganizationId: {request.OrganizationId}");

            Document? document = await _repository.Document
                .FindByCondition(x => x.Id == request.DocumentId && x.OrganizationId == request.OrganizationId)
                .FirstOrDefaultAsync(cancellationToken);
            if (document == null)
            {
                _logger.LogError($"Document not found. DocumentId: {request.DocumentId}, OrganizationId: {request.OrganizationId}");
                throw new NotFoundCustomException("Document not found.", "The document does not exist in your organization.");
            }

            byte[] content;
            try
            {
                content = await _storage.ReadAsync(document.StorageReference, cancellationToken);
            }
            catch (FileNotFoundException)
            {
                _logger.LogError($"Stored document content not found. DocumentId: {document.Id}");
                throw new NotFoundCustomException("Document content not found.", "The stored file of this document could not be found.");
            }

            _logger.LogInfo($"Document content fetched. DocumentId: {document.Id}, Bytes: {content.Length}");
            return new FileDownloadDto
            {
                FileName = document.OriginalFilename,
                ContentType = document.ContentType,
                FileBytes = content
            };
        }
    }
}
