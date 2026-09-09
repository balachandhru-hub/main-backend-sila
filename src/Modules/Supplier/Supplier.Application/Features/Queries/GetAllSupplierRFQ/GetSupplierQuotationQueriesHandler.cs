using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using Supplier.Domain.Dto;
using Supplier.Infrastructure.Contracts.IRepository;
using SharedKernel.LoggerServices;
using  Supplier.Domain.Common;

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
            var addLotOption = supplierRFQs.First().AddLotOption;

            var result = new GetAllSupplierQuotationDto();


            foreach (var supplierRFQ in supplierRFQs)
            {
                _logger.LogInfo($"Processing SupplierRFQId: {supplierRFQ.Id} for BuyerRFQId: {request.RFQId}");
                var quotations = await _repository.SupplierQuotation
                    .FindByCondition(x =>
                        x.SupplierRFQId == supplierRFQ.Id &&
                        x.BuyerRFQId == request.RFQId &&
                        x.IsActive&&
                        x.Status == Common.SUBMITTED)
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
                            ItemQuotationId = x.Id,
                            SupplierRFQItemId = x.SupplierRFQItemId,
                            BuyerRFQItemId = x.BuyerRFQItemId,
                            DeliveryCharge = x.DeliveryCharge,
                            DeliveryType = x.DeliveryType,

                            Discount = x.Discount,
                            DiscountType = x.DiscountType,

                            Tax = x.Tax,
                            TaxType = x.TaxType,

                            QuotedAmount = x.QuotedAmount,
                            SubTotal = x.SubTotal,
                             LineNumber = x.SupplierRFQItem.LineNumber
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
                              Currency = supplierRFQ.Currency,
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
             // ADD RANKING LOGIC
            if (addLotOption)
            {
                // AddLotOption = true
                // Rank based on quotation TotalPrice

                var rankedSuppliers = result.Suppliers
                    .OrderBy(x => x.TotalPrice)
                    .Select((x, index) => new
                    {
                        Supplier = x,
                        Rank = $"L{index + 1}"
                    })
                    .ToList();

                foreach (var item in rankedSuppliers)
                {
                    item.Supplier.Rank = item.Rank;
                    item.Supplier.IsLead = item.Rank == "L1";
                }

                result.Suppliers = result.Suppliers
                    .OrderBy(x => x.TotalPrice)
                    .ToList();
            }
            else
            {
                // AddLotOption = false
                // Rank each RFQ item separately based on SubTotal

                var rankedItems = result.Suppliers
                    .SelectMany(x => x.SupplierQuotationItems)
                    .GroupBy(x => x.BuyerRFQItemId)
                    .SelectMany(group =>
                        group
                            .OrderBy(x => x.SubTotal)
                            .Select((x, index) => new
                            {
                                Item = x,
                                Rank = $"L{index + 1}"
                            }))
                    .ToList();

                foreach (var item in rankedItems)
                {
                    item.Item.Rank = item.Rank;
                }
            result.Suppliers = result.Suppliers
                .OrderBy(x => x.TotalPrice)
                .ToList();
            var lowestTotalPrice = result.Suppliers.Min(x => x.TotalPrice);


            foreach (var supplier in result.Suppliers)
            {
                supplier.IsLead = supplier.TotalPrice == lowestTotalPrice;
            }
            }
            _logger.LogInfo($"Total Supplier Quotations found for BuyerRFQId: {request.RFQId} is {result.Suppliers.Count}");
            return result;
        }
    }
}
