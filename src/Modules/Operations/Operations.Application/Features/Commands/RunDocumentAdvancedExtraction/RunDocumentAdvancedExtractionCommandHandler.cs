using MediatR;
using Microsoft.EntityFrameworkCore;
using Operations.Application.Features.Shared;
using Operations.Application.Services.Ocr;
using Operations.Application.Services.Storage;
using Operations.Domain.Common;
using Operations.Domain.Dtos;
using Operations.Domain.Entities;
using Operations.Domain.Enums;
using Operations.Infrastructure.Contracts.IRepository;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Operations.Application.Features.Commands.RunDocumentAdvancedExtraction
{
    public class RunDocumentAdvancedExtractionCommandHandler : IRequestHandler<RunDocumentAdvancedExtractionCommand, AdvancedInvoiceExtractionResponseDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly IDocumentStorageService _storage;
        private readonly IInvoiceOcrPipeline _pipeline;

        public RunDocumentAdvancedExtractionCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            IDocumentStorageService storage,
            IInvoiceOcrPipeline pipeline)
        {
            _repository = repository;
            _logger = logger;
            _storage = storage;
            _pipeline = pipeline;
        }

        public async Task<AdvancedInvoiceExtractionResponseDto> Handle(RunDocumentAdvancedExtractionCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Running advanced extraction. DocumentId: {request.DocumentId}, Trigger: {request.Trigger}, OrganizationId: {request.OrganizationId}, UserId: {request.UserId}");

            Document? document = await _repository.Document.GetTrackedAsync(request.DocumentId, request.OrganizationId, cancellationToken);
            if (document == null)
            {
                _logger.LogError($"Document not found. DocumentId: {request.DocumentId}, OrganizationId: {request.OrganizationId}");
                throw new NotFoundCustomException("Document not found.", "The document does not exist in your organization.");
            }

            Invoice? invoice = await _repository.Invoice.GetTrackedByDocumentAsync(document.Id, request.OrganizationId, cancellationToken);
            AdvancedInvoiceExtractionResponseDto response = await InvoiceExtractionWorkflow.RunAdvancedAsync(
                _repository, _storage, _pipeline, _logger, document, invoice, request.Trigger, request.UserId, cancellationToken);

            _logger.LogInfo($"Advanced extraction completed. DocumentId: {document.Id}, Status: {response.Status}");
            return response;
        }
    }
}
