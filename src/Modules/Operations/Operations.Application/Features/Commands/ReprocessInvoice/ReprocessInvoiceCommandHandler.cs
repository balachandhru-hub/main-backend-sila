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

namespace Operations.Application.Features.Commands.ReprocessInvoice
{
    public class ReprocessInvoiceCommandHandler : IRequestHandler<ReprocessInvoiceCommand, AdvancedInvoiceExtractionResponseDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly IDocumentStorageService _storage;
        private readonly IInvoiceOcrPipeline _pipeline;

        public ReprocessInvoiceCommandHandler(
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

        public async Task<AdvancedInvoiceExtractionResponseDto> Handle(ReprocessInvoiceCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Re-reading invoice. InvoiceId: {request.InvoiceId}, OrganizationId: {request.OrganizationId}, UserId: {request.UserId}");

            Invoice? invoice = await _repository.Invoice.GetTrackedAsync(request.InvoiceId, request.OrganizationId, cancellationToken);
            if (invoice == null)
            {
                _logger.LogError($"Invoice not found. InvoiceId: {request.InvoiceId}, OrganizationId: {request.OrganizationId}");
                throw new NotFoundCustomException("Invoice not found.", "The invoice does not exist in your organization.");
            }

            AdvancedInvoiceExtractionResponseDto response = await InvoiceExtractionWorkflow.RunAdvancedAsync(
                _repository, _storage, _pipeline, _logger, invoice.Document, invoice, ExtractionTrigger.MANUAL_REREAD, request.UserId, cancellationToken);

            _logger.LogInfo($"Invoice re-read completed. InvoiceId: {invoice.Id}, Status: {response.Status}");
            return response;
        }
    }
}
