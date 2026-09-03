using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;
using Supplier.Infrastructure.Contracts.IRepository;
using Supplier.Domain.Common;
using Supplier.Domain.Entities;


using Supplier.Domain.Dto;
using Supplier.Application.Contracts;

using Supplier.Domain.Dto;
using Supplier.Application.Contracts;

using Supplier.Domain.Dto;
using Supplier.Application.Contracts;

namespace Supplier.Application.Features.Commands.UpdateSupplierQuotation
{
    public class UpdateSupplierQuotationCommandHandler
        : IRequestHandler<UpdateSupplierQuotationCommand, UpdateSupplierQuotationResultDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly IBuyerApiClient _buyerApiClient;


        public UpdateSupplierQuotationCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            IBuyerApiClient buyerApiClient
          )
        {
            _repository = repository;
            _logger = logger;
            _buyerApiClient=buyerApiClient;
            
        }

        public async Task<UpdateSupplierQuotationResultDto> Handle(
            UpdateSupplierQuotationCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo(
                $"Updating Supplier Quotation : {request.Quotation.SupplierQuotationId}");
            if (string.IsNullOrWhiteSpace(request.TemporaryVerificationToken))
            {
                throw new BadRequestCustomException(
                    "Verification token is required.",
                    "Please verify the OTP before submitting the quotation.");
            }

            var verification = await _repository.SupplierEmailVerification
                .FindByCondition(x =>
                    x.TemporaryVerificationToken ==
                        request.TemporaryVerificationToken &&
                    x.IsVerified)
                .FirstOrDefaultAsync(cancellationToken);

            if (verification == null)
            {
                throw new BadRequestCustomException(
                    "Invalid verification token.",
                    "Please verify the OTP before submitting the quotation.");
            }

            // Check verification token expiry
            if (verification.TemporaryVerificationTokenExpiresOn == null ||
                verification.TemporaryVerificationTokenExpiresOn <= DateTime.UtcNow)
            {
                throw new BadRequestCustomException(
                    "Verification token expired.",
                    "Please verify the OTP again before submitting the quotation.");
            }

            _logger.LogInfo(
                $"OTP verification successful for quotation : {request.Quotation.SupplierQuotationId}");

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

            var existingVersions = await _repository.SupplierQuotationHistory
                .FindByCondition(x => x.SupplierQuotationId == quotation.Id)
                .Select(x => x.Version)
                .ToListAsync(cancellationToken);

            int latestVersion = 0;

            foreach (var version in existingVersions)
            {
                if (string.IsNullOrWhiteSpace(version))
                    continue;

                if (int.TryParse(
                    version.TrimStart('V', 'v'),
                    out int versionNumber))
                {
                    latestVersion = Math.Max(latestVersion, versionNumber);
                }
            }

            string newVersion = $"V{latestVersion + 1}";

            _logger.LogInfo(
                $"New Supplier Quotation Version: {newVersion}");

            quotation.DeliveryCharge = request.Quotation.DeliveryCharge;
            quotation.DeliveryType = request.Quotation.DeliveryType;

            quotation.Discount = request.Quotation.Discount;
            quotation.DiscountType = request.Quotation.DiscountType;

            quotation.Tax = request.Quotation.Tax;
            quotation.TaxType = request.Quotation.TaxType;

            decimal subTotal = 0;

            var quotationItemsForHistory =
                new List<SupplierQuotationItem>();

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
                            x.SupplierRFQItemId == item.SupplierRFQItemId&&
                              x.SupplierId == quotation.SupplierId )
                        .FirstOrDefaultAsync(cancellationToken);

                    if (quotationItem == null)
                    {
                         quotationItem = new SupplierQuotationItem
                            {
                                 Id = Guid.NewGuid(),

                                SupplierQuotationId = quotation.Id,
                                SupplierRFQItemId = item.SupplierRFQItemId,

                                BuyerRFQItemId = item.BuyerRFQItemId,
                                BuyerRFQId = quotation.BuyerRFQId,

                                RFQNumber = quotation.RFQNumber,
                                BuyerId = quotation.BuyerId,
                                SupplierId = quotation.SupplierId,

                                QuotedPrice = item.QuotedPrice,
                            };
                            _repository.SupplierQuotationItem.Create(quotationItem);
                    }
                    else
                    {
                               quotationItem.QuotedPrice = item.QuotedPrice;

                        _repository.SupplierQuotationItem.Update(quotationItem);
                        }

             

                    subTotal += quotationItem.QuotedPrice;


                    quotationItemsForHistory.Add(quotationItem);
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

            //delivery charge
            if (quotation.Discount.HasValue)
            {
                _logger.LogInfo(
                    $"Applying discount for Supplier Quotation : {request.Quotation.SupplierQuotationId}");

                if (string.Equals(
                    quotation.DiscountType,
                    Common.PERCENTAGE,
                    StringComparison.OrdinalIgnoreCase))
                {
                    total -= total * quotation.Discount.Value / 100;
                }
                else
                {
                    total -= quotation.Discount.Value;
                }
            }
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
                    Common.PERCENTAGE,
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

            var quotationHistory = new SupplierQuotationHistory
            {
                Id = Guid.NewGuid(),

                SupplierQuotationId = quotation.Id,

                SupplierRFQId = quotation.SupplierRFQId,

                BuyerRFQId = quotation.BuyerRFQId,

                RFQNumber = quotation.RFQNumber,

                BuyerId = quotation.BuyerId,

                SupplierId = quotation.SupplierId,

                Version = newVersion,

                TotalPrice = quotation.TotalPrice,

                DeliveryCharge = quotation.DeliveryCharge,

                DeliveryType = quotation.DeliveryType,

                Discount = quotation.Discount,

                DiscountType = quotation.DiscountType,

                Tax = quotation.Tax,

                TaxType = quotation.TaxType,

                Status = quotation.Status
            };

            await _repository.SupplierQuotationHistory
                .CreateAsync(quotationHistory);

            foreach (var quotationItem in quotationItemsForHistory)
            {
                var itemHistory = new SupplierQuotationItemHistory
                {
                    Id = Guid.NewGuid(),

                    SupplierQuotationItemId =
                        quotationItem.Id,

                    SupplierQuotationId =
                        quotationItem.SupplierQuotationId,

                    SupplierRFQItemId =
                        quotationItem.SupplierRFQItemId,

                    BuyerRFQItemId =
                        quotationItem.BuyerRFQItemId,

                    BuyerRFQId =
                        quotationItem.BuyerRFQId,

                    RFQNumber =
                        quotationItem.RFQNumber,

                    BuyerId =
                        quotationItem.BuyerId,

                    SupplierId =
                        quotationItem.SupplierId,

                    Version = newVersion,

                    QuotedPrice =
                        quotationItem.QuotedPrice
                };

                await _repository.SupplierQuotationItemHistory
                    .CreateAsync(itemHistory);
            }
            await _repository.SaveAsync();

            _logger.LogInfo(
     $"Supplier quotation updated successfully : {quotation.Id}");

            // Get quotation items
            var quotationItems = await _repository.SupplierQuotationItem
                .FindByCondition(x => x.SupplierQuotationId == quotation.Id)
                .ToListAsync(cancellationToken);

            // Create audit object
            var audit = new QuotationAuditDto
            {
                BuyerRFQId = quotation.BuyerRFQId,

                SupplierQuotationId = quotation.Id,

                TotalPrice = quotation.TotalPrice,

                DeliveryCharge = quotation.DeliveryCharge,

                DeliveryType = quotation.DeliveryType,

                Discount = quotation.Discount,

                DiscountType = quotation.DiscountType,

                Tax = quotation.Tax,

                TaxType = quotation.TaxType,

                Items = quotationItems.Select(item => new QuotationAuditItemDto
                {
                    SupplierQuotationItemId = item.Id,

                    SupplierRfqItemId = item.SupplierRFQItemId,

                    BuyerRfqItemId = item.BuyerRFQItemId,

                    QuotedPrice = item.QuotedPrice

                }).ToList()
            };



            // Send to Buyer
            await _buyerApiClient.StoreQuotationAuditAsync(
                audit,
                cancellationToken);
            return new UpdateSupplierQuotationResultDto
            {
                QuotationId = quotation.Id,
                BuyerId = quotation.BuyerId,
                SupplierId = quotation.SupplierId
            };
        }
    }
}