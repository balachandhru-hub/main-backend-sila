using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.LoggerServices;
using Supplier.Infrastructure.Contracts.IRepository;

namespace Supplier.Application.Features.Commands.UpdateSupplierQuotation
{
    public class UpdateSupplierQuotationCommandHandler
        : IRequestHandler<UpdateSupplierQuotationCommand, UpdateSupplierQuotationResultDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public UpdateSupplierQuotationCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<UpdateSupplierQuotationResultDto> Handle(
            UpdateSupplierQuotationCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo(
                $"Updating Supplier Quotation : {request.Quotation.SupplierQuotationId}");

            var quotation = await _repository.SupplierQuotation
                .GetByIdAsync(request.Quotation.SupplierQuotationId);

            if (quotation == null)
                throw new Exception("Supplier quotation not found.");

            var supplierRFQ = await _repository.SupplierRFQ
                .FindByCondition(x => x.Id == quotation.SupplierRFQId)
                .FirstOrDefaultAsync(cancellationToken);

            if (supplierRFQ == null)
                throw new Exception("Supplier RFQ not found.");

            quotation.DeliveryCharge = request.Quotation.DeliveryCharge;
            quotation.DeliveryType = request.Quotation.DeliveryType;

            quotation.Discount = request.Quotation.Discount;
            quotation.DiscountType = request.Quotation.DiscountType;

            quotation.Tax = request.Quotation.Tax;
            quotation.TaxType = request.Quotation.TaxType;

            decimal subTotal = 0;

            if (!supplierRFQ.AddLotOption)
            {
                if (request.Quotation.Items == null || !request.Quotation.Items.Any())
                    throw new Exception("Quotation items are required.");

                foreach (var item in request.Quotation.Items)
                {
                    var quotationItem = await _repository.SupplierQuotationItem
                        .FindByCondition(x =>
                            x.SupplierQuotationId == quotation.Id &&
                            x.SupplierRFQItemId == item.SupplierRFQItemId)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (quotationItem == null)
                        throw new Exception($"Quotation Item not found : {item.SupplierRFQItemId}");

                    // First update -> use request quoted price
                    // Next updates -> use already saved quoted price
                    decimal basePrice = quotationItem.QuotedPrice > 0
                        ? quotationItem.QuotedPrice
                        : item.QuotedPrice;

                    decimal finalQuotedPrice = basePrice;

                    // Discount only (Per Item)
                    if (quotation.Discount.HasValue)
                    {
                        if (string.Equals(
                            quotation.DiscountType,
                            "Percentage",
                            StringComparison.OrdinalIgnoreCase))
                        {
                            finalQuotedPrice -=
                                basePrice * quotation.Discount.Value / 100;
                        }
                        else
                        {
                            finalQuotedPrice -= quotation.Discount.Value;
                        }
                    }

                    quotationItem.QuotedPrice = finalQuotedPrice;

                    _repository.SupplierQuotationItem.Update(quotationItem);

                    subTotal += finalQuotedPrice;
                }
            }
            else
            {
                if (!request.Quotation.TotalPrice.HasValue)
                    throw new Exception("Lot quotation total price is required.");

                subTotal = request.Quotation.TotalPrice.Value;
            }




            decimal total = subTotal;

            // Tax (Per Quotation)
            if (quotation.Tax.HasValue)
            {
                if (string.Equals(
                    quotation.TaxType,
                    "Percentage",
                    StringComparison.OrdinalIgnoreCase))
                {
                    total += total * quotation.Tax.Value / 100;
                }
                else
                {
                    total += quotation.Tax.Value;
                }
            }

            // Delivery (Per Quotation)
            if (quotation.DeliveryCharge.HasValue)
            {
                if (string.Equals(
                    quotation.DeliveryType,
                    "Percentage",
                    StringComparison.OrdinalIgnoreCase))
                {
                    total += total * quotation.DeliveryCharge.Value / 100;
                }
                else
                {
                    total += quotation.DeliveryCharge.Value;
                }
            }

            quotation.TotalPrice = total;

            _repository.SupplierQuotation.Update(quotation);

            await _repository.SaveAsync();

            _logger.LogInfo(
                $"Supplier quotation updated successfully : {quotation.Id}");

            return new UpdateSupplierQuotationResultDto
            {
                QuotationId = quotation.Id,
                BuyerId = quotation.BuyerId,
                SupplierId = quotation.SupplierId
            };
        }
    }
}