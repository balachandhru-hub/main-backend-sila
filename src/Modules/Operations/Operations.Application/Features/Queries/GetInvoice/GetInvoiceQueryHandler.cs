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

namespace Operations.Application.Features.Queries.GetInvoice
{
    public class GetInvoiceQueryHandler : IRequestHandler<GetInvoiceQuery, InvoiceResponseDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetInvoiceQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<InvoiceResponseDto> Handle(GetInvoiceQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching invoice. InvoiceId: {request.InvoiceId}, OrganizationId: {request.OrganizationId}");

            Invoice? invoice = await _repository.Invoice
                .FindByCondition(x => x.Id == request.InvoiceId && x.OrganizationId == request.OrganizationId)
                .FirstOrDefaultAsync(cancellationToken);
            if (invoice == null)
            {
                _logger.LogError($"Invoice not found. InvoiceId: {request.InvoiceId}, OrganizationId: {request.OrganizationId}");
                throw new NotFoundCustomException("Invoice not found.", "The invoice does not exist in your organization.");
            }

            InvoiceResponseDto result = await ResponseBuilder.InvoiceAsync(_repository, invoice, cancellationToken);
            _logger.LogInfo($"Invoice fetched. InvoiceId: {invoice.Id}");
            return result;
        }
    }
}
