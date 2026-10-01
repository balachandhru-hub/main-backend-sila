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

namespace Operations.Application.Features.Commands.MatchInvoiceSupplier
{
    public class MatchInvoiceSupplierCommandHandler : IRequestHandler<MatchInvoiceSupplierCommand, InvoiceResponseDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public MatchInvoiceSupplierCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<InvoiceResponseDto> Handle(MatchInvoiceSupplierCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Matching invoice supplier. InvoiceId: {request.InvoiceId}, SupplierId: {request.Request.SupplierId}, OrganizationId: {request.OrganizationId}");

            Invoice? invoice = await _repository.Invoice.GetTrackedAsync(request.InvoiceId, request.OrganizationId, cancellationToken);
            if (invoice == null)
            {
                _logger.LogError($"Invoice not found. InvoiceId: {request.InvoiceId}, OrganizationId: {request.OrganizationId}");
                throw new NotFoundCustomException("Invoice not found.", "The invoice does not exist in your organization.");
            }

            if (request.Request.SupplierId != null)
            {
                Guid supplierId = request.Request.SupplierId.Value;
                bool exists = await _repository.SupplierMaster
                    .FindByCondition(x => x.Id == supplierId && x.OrganizationId == request.OrganizationId
                        && x.Status == StatusKind.ACTIVE && !x.IsBlocked && !x.IsDeleted)
                    .AnyAsync(cancellationToken);
                if (!exists)
                {
                    _logger.LogError($"Supplier not found. SupplierId: {supplierId}, OrganizationId: {request.OrganizationId}");
                    throw new NotFoundCustomException("Supplier not found.", "The supplier was not found in this organization.");
                }

                invoice.SupplierId = supplierId;
            }
            else
            {
                await InvoiceWorkflow.MatchSupplierAsync(_repository, invoice, cancellationToken);
            }

            await _repository.SaveAsync();
            InvoiceResponseDto result = await ResponseBuilder.InvoiceAsync(_repository, invoice, cancellationToken);
            _logger.LogInfo($"Invoice supplier matched. InvoiceId: {invoice.Id}, SupplierId: {invoice.SupplierId}");
            return result;
        }
    }
}
