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

namespace Operations.Application.Features.Queries.GetDocumentStatus
{
    public class GetDocumentStatusQueryHandler : IRequestHandler<GetDocumentStatusQuery, DocumentStatusResponseDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetDocumentStatusQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<DocumentStatusResponseDto> Handle(GetDocumentStatusQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching document status. DocumentId: {request.DocumentId}, OrganizationId: {request.OrganizationId}");

            Invoice? invoice = await _repository.Invoice
                .FindByCondition(x => x.DocumentId == request.DocumentId && x.OrganizationId == request.OrganizationId)
                .Include(x => x.Document)
                .FirstOrDefaultAsync(cancellationToken);
            if (invoice == null)
            {
                _logger.LogError($"Document not found. DocumentId: {request.DocumentId}, OrganizationId: {request.OrganizationId}");
                throw new NotFoundCustomException("Document not found.", "The document does not exist in your organization.");
            }

            return new DocumentStatusResponseDto
            {
                DocumentId = invoice.DocumentId,
                InvoiceId = invoice.Id,
                DocumentStatus = invoice.Document.Status,
                InvoiceStatus = invoice.Status,
                Message = invoice.ExtractionStatus == Common.MANUAL_ENTRY_REQUIRED
                    ? Common.OCR_UNAVAILABLE_MESSAGE
                    : invoice.Document.Status == DocumentStatus.FAILED ? "The invoice could not be read." : null
            };
        }
    }
}
