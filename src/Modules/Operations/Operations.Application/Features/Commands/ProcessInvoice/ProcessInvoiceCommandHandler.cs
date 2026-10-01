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

namespace Operations.Application.Features.Commands.ProcessInvoice
{
    public class ProcessInvoiceCommandHandler : IRequestHandler<ProcessInvoiceCommand, InvoiceResponseDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly IDocumentStorageService _storage;
        private readonly IInvoiceOcrPipeline _pipeline;

        public ProcessInvoiceCommandHandler(
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

        public async Task<InvoiceResponseDto> Handle(ProcessInvoiceCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Processing invoice. InvoiceId: {request.InvoiceId}, OrganizationId: {request.OrganizationId}, UserId: {request.UserId}");

            await InvoiceExtractionWorkflow.RunBasicAsync(_repository, _storage, _pipeline, _logger, request.InvoiceId, request.OrganizationId, request.UserId, cancellationToken);
            Invoice invoice = await _repository.Invoice
                .FindByCondition(x => x.Id == request.InvoiceId && x.OrganizationId == request.OrganizationId)
                .FirstAsync(cancellationToken);
            InvoiceResponseDto result = await ResponseBuilder.InvoiceAsync(_repository, invoice, cancellationToken);

            _logger.LogInfo($"Invoice processed. InvoiceId: {invoice.Id}, Status: {invoice.Status}");
            return result;
        }
    }
}
