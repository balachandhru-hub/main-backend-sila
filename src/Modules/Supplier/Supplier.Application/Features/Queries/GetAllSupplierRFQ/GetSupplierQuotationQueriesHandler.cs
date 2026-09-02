using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using Supplier.Domain.Dto;
using Supplier.Infrastructure.Contracts.IRepository;
using SharedKernel.LoggerServices;

namespace Supplier.Application.Features.Queries.GetSupplierQuotation
{
    public class GetSupplierQuotationByBuyerRFQIdQueryHandler
        : IRequestHandler<GetSupplierQuotationByBuyerRFQIdQuery, GetAllSupplierQuotationDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetSupplierQuotationByBuyerRFQIdQueryHandler(
            IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }


        public async Task<GetAllSupplierQuotationDto> Handle(
            GetSupplierQuotationByBuyerRFQIdQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching Supplier Quotation for BuyerRFQId: {request.RFQId}");

            var supplierRFQs = await _repository.SupplierRFQ
                .FindByCondition(x =>
                    x.BuyerRFQId == request.RFQId &&
                    x.IsActive)
                .ToListAsync(cancellationToken);

            if (!supplierRFQs.Any())
            {
                _logger.LogInfo($"No Supplier RFQ found for BuyerRFQId: {request.RFQId}");
                throw new NotFoundCustomException(
                    "Supplier RFQ not found.",
                    $"No Supplier RFQ found for BuyerRFQId: {request.RFQId}");
            }

            var result = new GetAllSupplierQuotationDto();


            foreach (var supplierRFQ in supplierRFQs)
            {
                _logger.LogInfo($"Processing SupplierRFQId: {supplierRFQ.Id} for BuyerRFQId: {request.RFQId}");
                var quotations = await _repository.SupplierQuotation
                    .FindByCondition(x =>
                        x.SupplierRFQId == supplierRFQ.Id &&
                        x.BuyerRFQId == request.RFQId &&
                        x.IsActive)
                    .ToListAsync(cancellationToken);


                foreach (var quotation in quotations)
                {
                    _logger.LogInfo($"Processing QuotationId: {quotation.Id} for SupplierRFQId: {supplierRFQ.Id}");

                    var supplierId = quotation.SupplierId;


                    var supplier = await _repository.SupplierBusinessProfile
                        .FindByCondition(x =>
                            x.Id == supplierId &&
                            x.IsActive)
                        .FirstOrDefaultAsync(cancellationToken);

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


                    result.Suppliers.Add(
                        new SupplierQuotationBySupplierDto
                        {
                            SupplierRFQId = quotation.SupplierRFQId,


                            SupplierId = quotation.SupplierId,


                            SupplierName = supplier?.OrganizationName,

                            QuotationId = quotation.Id,
                            TotalPrice = quotation.TotalPrice,
                            DeliveryCharge = quotation.DeliveryCharge,
                            Tax = quotation.Tax,
                            Discount = quotation.Discount,
                            DeliveryType = quotation.DeliveryType,
                            Status = quotation.Status,

                            SupplierQuotationItems = quotationItems
                        });
                }
            }

            if (!result.Suppliers.Any())
            {
                _logger.LogInfo($"No active Supplier Quotation found for BuyerRFQId: {request.RFQId}");
                throw new NotFoundCustomException(
                    "Supplier Quotation not found.",
                    $"No active SupplierQuotation found for BuyerRFQId: {request.RFQId}");
            }
            _logger.LogInfo($"Total Supplier Quotations found for BuyerRFQId: {request.RFQId} is {result.Suppliers.Count}");
            return result;
        }
    }
}
