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

namespace Operations.Application.Features.Queries.GetExtractionHistory
{
    public class GetExtractionHistoryQueryHandler : IRequestHandler<GetExtractionHistoryQuery, List<ExtractionHistoryItemDto>>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetExtractionHistoryQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<List<ExtractionHistoryItemDto>> Handle(GetExtractionHistoryQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching extraction history. InvoiceId: {request.InvoiceId}, OrganizationId: {request.OrganizationId}");

            Invoice? invoice = await _repository.Invoice
                .FindByCondition(x => x.Id == request.InvoiceId && x.OrganizationId == request.OrganizationId)
                .FirstOrDefaultAsync(cancellationToken);
            if (invoice == null)
            {
                _logger.LogError($"Invoice not found. InvoiceId: {request.InvoiceId}, OrganizationId: {request.OrganizationId}");
                throw new NotFoundCustomException("Invoice not found.", "The invoice does not exist in your organization.");
            }

            List<DocumentExtraction> runs = await _repository.DocumentExtraction
                .FindByCondition(x => x.DocumentId == invoice.DocumentId)
                .OrderByDescending(x => x.DateCreated)
                .ToListAsync(cancellationToken);

            _logger.LogInfo($"Extraction history fetched. InvoiceId: {invoice.Id}, Count: {runs.Count}");
            return runs.Select(run => new ExtractionHistoryItemDto
            {
                Id = run.Id,
                DocumentId = run.DocumentId,
                Provider = run.Provider,
                Status = run.Status,
                Trigger = run.Trigger,
                OcrRequestId = run.OcrRequestId,
                ExtractionMethod = run.ExtractionMethod,
                Confidence = run.Confidence,
                ContentHash = run.ContentHash,
                FallbackUsed = run.FallbackUsed,
                ProcessingStartedAt = run.ProcessingStartedAt,
                ProcessingCompletedAt = run.ProcessingCompletedAt,
                ProcessingDurationMs = run.ProcessingDurationMs,
                ErrorCode = run.ErrorCode,
                ErrorMessage = run.ErrorMessage,
                CreatedAt = run.DateCreated
            }).ToList();
        }
    }
}
