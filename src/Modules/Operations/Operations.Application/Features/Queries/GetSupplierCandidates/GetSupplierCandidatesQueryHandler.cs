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

namespace Operations.Application.Features.Queries.GetSupplierCandidates
{
    public class GetSupplierCandidatesQueryHandler : IRequestHandler<GetSupplierCandidatesQuery, SupplierMatchResponseDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetSupplierCandidatesQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<SupplierMatchResponseDto> Handle(GetSupplierCandidatesQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching supplier candidates. InvoiceId: {request.InvoiceId}, OrganizationId: {request.OrganizationId}");

            Invoice? invoice = await _repository.Invoice
                .FindByCondition(x => x.Id == request.InvoiceId && x.OrganizationId == request.OrganizationId)
                .FirstOrDefaultAsync(cancellationToken);
            if (invoice == null)
            {
                _logger.LogError($"Invoice not found. InvoiceId: {request.InvoiceId}, OrganizationId: {request.OrganizationId}");
                throw new NotFoundCustomException("Invoice not found.", "The invoice does not exist in your organization.");
            }

            string normalized = InvoiceWorkflow.Normalize(invoice.SupplierNameRaw);
            List<SupplierMaster> suppliers = await _repository.SupplierMaster.FindCandidatesAsync(
                request.OrganizationId, invoice.SupplierTaxNumberRaw, normalized, 20, cancellationToken);
            List<SupplierMatchCandidateDto> candidates = suppliers.Select(supplier =>
            {
                bool byTaxNumber = invoice.SupplierTaxNumberRaw != null && supplier.TaxNumber == invoice.SupplierTaxNumberRaw;
                bool byName = supplier.NormalizedName == normalized;
                return new SupplierMatchCandidateDto
                {
                    SupplierId = supplier.Id,
                    SupplierCode = supplier.SupplierCode,
                    Name = supplier.Name,
                    TaxNumber = supplier.TaxNumber,
                    Confidence = byTaxNumber ? 1m : byName ? 0.95m : 0.8m,
                    MatchReason = byTaxNumber ? "TRN" : byName ? "NAME" : "ALIAS"
                };
            }).ToList();

            _logger.LogInfo($"Supplier candidates fetched. InvoiceId: {invoice.Id}, Count: {candidates.Count}");
            return new SupplierMatchResponseDto
            {
                InvoiceId = invoice.Id,
                SupplierId = invoice.SupplierId,
                SupplierName = invoice.SupplierNameRaw,
                Candidates = candidates,
                RequiresSelection = invoice.SupplierId == null && candidates.Count != 1
            };
        }
    }
}
