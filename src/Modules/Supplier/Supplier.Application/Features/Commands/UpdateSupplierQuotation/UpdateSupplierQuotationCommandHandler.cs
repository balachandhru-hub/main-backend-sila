using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;
using Supplier.Infrastructure.Contracts.IRepository;
using Supplier.Domain.Common;

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
            {
                _logger.LogError(
                    $"Supplier Quotation not found : {request.Quotation.SupplierQuotationId}");
                throw new NotFoundCustomException(
                    "Supplier Quotation not found.",
                    $"Supplier Quotation with ID {request.Quotation.SupplierQuotationId} was not found.");
            }

            var supplierRFQ = await _repository.SupplierRFQ
                .FindByCondition(x => x.Id == quotation.SupplierRFQId)
                .FirstOrDefaultAsync(cancellationToken);

            if (supplierRFQ == null)
            {
                _logger.LogError(
                    $"Supplier RFQ not found for Quotation ID : {request.Quotation.SupplierQuotationId}");
                throw new NotFoundCustomException(
                    "Supplier RFQ not found.",
                    $"Supplier RFQ with ID {quotation.SupplierRFQId} was not found.");
            }

            quotation.DeliveryCharge = request.Quotation.DeliveryCharge;
            quotation.DeliveryType = request.Quotation.DeliveryType;

            quotation.Discount = request.Quotation.Discount;
            quotation.DiscountType = request.Quotation.DiscountType;

            quotation.Tax = request.Quotation.Tax;
            quotation.TaxType = request.Quotation.TaxType;

            decimal subTotal = 0;

            if (!supplierRFQ.AddLotOption)
            {
                _logger.LogInfo(
                    $"Updating item-wise quotation for Supplier Quotation : {request.Quotation.SupplierQuotationId}");
                if (request.Quotation.Items == null || !request.Quotation.Items.Any())
                {
                    _logger.LogError(
                        $"Quotation items are required for Supplier Quotation : {request.Quotation.SupplierQuotationId}");
                    throw new BadRequestCustomException(
                        "Quotation items are required.",
                        "Please provide quotation items for item-wise quotation.");
                }
                    

                foreach (var item in request.Quotation.Items)
                {
                    _logger.LogInfo(
                        $"Updating Quotation Item : {item.SupplierRFQItemId} for Supplier Quotation : {request.Quotation.SupplierQuotationId}");
                    var quotationItem = await _repository.SupplierQuotationItem
                        .FindByCondition(x =>
                            x.SupplierQuotationId == quotation.Id &&
                            x.SupplierRFQItemId == item.SupplierRFQItemId)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (quotationItem == null)
                    {
                        _logger.LogError(
                            $"Quotation Item not found : {item.SupplierRFQItemId}");
                        throw new BadRequestCustomException(
                            "Quotation Item not found.",
                            $"Quotation Item with ID {item.SupplierRFQItemId} was not found.");
                    }

                   
                    decimal basePrice = quotationItem.QuotedPrice > 0
                        ? quotationItem.QuotedPrice
                        : item.QuotedPrice;

                    decimal finalQuotedPrice = basePrice;

               
                    if (quotation.Discount.HasValue)
                    {
                        _logger.LogInfo(
                            $"Applying discount for Quotation Item : {item.SupplierRFQItemId}");
                        if (string.Equals(
                            quotation.DiscountType,
                            Common.PERCENTAGE,
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
                {
                    _logger.LogError(
                        $"Total price is required for lot quotation : {request.Quotation.SupplierQuotationId}");
                    throw new BadRequestCustomException(
                        "Total price is required.",
                        "Please provide total price for lot quotation.");
                }

                subTotal = request.Quotation.TotalPrice.Value;
            }




            decimal total = subTotal;

            // Tax (Per Quotation)
            if (quotation.Tax.HasValue)
            {
                if (string.Equals(
                    quotation.TaxType,
                    Common.PERCENTAGE,
                    StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInfo(
                        $"Applying tax for Supplier Quotation : {request.Quotation.SupplierQuotationId}");
                    total += total * quotation.Tax.Value / 100;
                }
                else
                {
                    _logger.LogInfo(
                        $"Applying tax for Supplier Quotation : {request.Quotation.SupplierQuotationId}");
                    total += quotation.Tax.Value;
                }
            }

            // Delivery (Per Quotation)
            if (quotation.DeliveryCharge.HasValue)
            {
                _logger.LogInfo(
                    $"Applying delivery charge for Supplier Quotation : {request.Quotation.SupplierQuotationId}");
                if (string.Equals(
                    quotation.DeliveryType,
                    "Percentage",
                    StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInfo(
                        $"Applying delivery charge for Supplier Quotation : {request.Quotation.SupplierQuotationId}");
                    total += total * quotation.DeliveryCharge.Value / 100;
                }
                else
                {
                    _logger.LogInfo(
                        $"Applying delivery charge for Supplier Quotation : {request.Quotation.SupplierQuotationId}");
                    total += quotation.DeliveryCharge.Value;
                }
            }

            quotation.TotalPrice = total;
            quotation.Status = Common.SUBMITTED_STATUS;

       

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