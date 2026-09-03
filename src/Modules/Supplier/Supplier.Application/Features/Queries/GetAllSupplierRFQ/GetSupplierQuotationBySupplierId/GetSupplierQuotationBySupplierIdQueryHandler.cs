using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using Supplier.Domain.Dto;
using Supplier.Infrastructure.Contracts.IRepository;
using SharedKernel.LoggerServices;

namespace Supplier.Application.Features.Queries.GetSupplierQuotationBySupplierId
{
    public class GetSupplierQuotationBySupplierIdQueryHandler
        : IRequestHandler<GetSupplierQuotationBySupplierIdQuery, GetAllSupplierQuotationBySupplierIdDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetSupplierQuotationBySupplierIdQueryHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<GetAllSupplierQuotationBySupplierIdDto> Handle(
            GetSupplierQuotationBySupplierIdQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching Supplier Quotation for BuyerRFQId: {request.RFQId} and OrganizationId: {request.OrganizationId}");
            var supplier = await _repository.SupplierBusinessProfile
                .FindByCondition(x =>
                    x.OrganizationId == request.OrganizationId &&
                    x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            if (supplier == null)
            {
                _logger.LogInfo($"No active Supplier found for OrganizationId: {request.OrganizationId}");
                throw new NotFoundCustomException(
                    "Supplier not found.",
                    $"No active Supplier found for OrganizationId: {request.OrganizationId}");
            }

            Guid supplierId = supplier.Id;


            var quotations = await _repository.SupplierQuotation
                .FindByCondition(x =>
                    x.BuyerRFQId == request.RFQId &&
                    x.SupplierId == supplierId &&
                    x.IsActive)
                .ToListAsync(cancellationToken);
                var lowestQuotation = await _repository.SupplierQuotation
                .FindByCondition(x =>
                    x.BuyerRFQId == request.RFQId &&
                    x.IsActive)
                .OrderBy(x => x.TotalPrice)
                .FirstOrDefaultAsync(cancellationToken);

            if (quotations == null || quotations.Count == 0)
            {
                _logger.LogInfo($"No active Supplier Quotation found for BuyerRFQId: {request.RFQId} and SupplierId: {supplierId}");
                throw new NotFoundCustomException(
                    "Supplier Quotation not found.",
                    $"No active SupplierQuotation found for BuyerRFQId: {request.RFQId} " +
                    $"and SupplierId: {supplierId}");
            }

            var response = new GetAllSupplierQuotationBySupplierIdDto();


            foreach (var quotation in quotations)
            {
                _logger.LogInfo($"Processing QuotationId: {quotation.Id} for SupplierId: {supplierId}");
                var quotationItems = await _repository.SupplierQuotationItem
                    .FindByCondition(x =>
                        x.SupplierQuotationId == quotation.Id &&
                        x.IsActive)
                    .Select(x => new SupplierQuotationItemDto
                    {
                        QuotedPrice = x.QuotedPrice,
                        ItemQutationId = x.Id
                    })
                    .ToListAsync(cancellationToken);

                var supplierDto = new SupplierQuotationBySupplierIdDto
                {
                    SupplierRFQId = quotation.SupplierRFQId,
                    SupplierId = supplierId,
                    SupplierName = supplier.OrganizationName,
                    QuotationId = quotation.Id,
                    TotalPrice = quotation.TotalPrice,
                    DeliveryCharge = quotation.DeliveryCharge,
                    Tax = quotation.Tax,
                    Discount = quotation.Discount,
                    DeliveryType = quotation.DeliveryType,
                    Status = quotation.Status,
                    SupplierQuotationItems = quotationItems,
                    IsLead = quotation.Id == lowestQuotation?.Id,
                };

                response.Suppliers.Add(supplierDto);
            }
            _logger.LogInfo($"Successfully fetched {response.Suppliers.Count} Supplier Quotations for BuyerRFQId: {request.RFQId} and SupplierId: {supplierId}");
            return response;
        }
    }
}