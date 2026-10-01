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

namespace Operations.Application.Features.Queries.GetDocument
{
    public class GetDocumentQueryHandler : IRequestHandler<GetDocumentQuery, DocumentResponseDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetDocumentQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<DocumentResponseDto> Handle(GetDocumentQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching document. DocumentId: {request.DocumentId}, OrganizationId: {request.OrganizationId}");

            Document? document = await _repository.Document
                .FindByCondition(x => x.Id == request.DocumentId && x.OrganizationId == request.OrganizationId)
                .FirstOrDefaultAsync(cancellationToken);
            if (document == null)
            {
                _logger.LogError($"Document not found. DocumentId: {request.DocumentId}, OrganizationId: {request.OrganizationId}");
                throw new NotFoundCustomException("Document not found.", "The document does not exist in your organization.");
            }

            Guid invoiceId = await _repository.Invoice
                .FindByCondition(x => x.DocumentId == document.Id)
                .Select(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken);
            _logger.LogInfo($"Document fetched. DocumentId: {document.Id}");
            return ResponseBuilder.DocumentResponse(document, invoiceId, "SAVED", null);
        }
    }
}
