using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;
using Supplier.Infrastructure.Contracts.IRepository;
using Supplier.Domain.Dto;

namespace Supplier.Application.Features.Queries.GetSupplierQuotationHistoryComparison
{
    public class GetSupplierQuotationHistoryComparisonQueryHandler
        : IRequestHandler<
            GetSupplierQuotationHistoryComparisonQuery,
            SupplierQuotationHistoryComparisonDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetSupplierQuotationHistoryComparisonQueryHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<SupplierQuotationHistoryComparisonDto> Handle(
            GetSupplierQuotationHistoryComparisonQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo(
                $"Getting quotation history comparison for SupplierQuotationId: " +
                $"{request.SupplierQuotationId}");

           
            var histories = await _repository.SupplierQuotationHistory
                .FindByCondition(x =>
                    x.SupplierQuotationId == request.SupplierQuotationId)
                .OrderBy(x => x.Version)
                .ToListAsync(cancellationToken);

            if (histories == null || !histories.Any())
            {
                _logger.LogError(
                    $"No quotation history found for SupplierQuotationId: " +
                    $"{request.SupplierQuotationId}");
                throw new NotFoundCustomException(
                    "Quotation history not found.",
                    $"No history found for Supplier Quotation " +
                    $"{request.SupplierQuotationId}.");
            }

            
            var oldHistory = histories.First();

            
            var latestHistory = histories.Last();

            
            var itemHistories = await _repository.SupplierQuotationItemHistory
                .FindByCondition(x =>
                    x.SupplierQuotationId == request.SupplierQuotationId)
                .ToListAsync(cancellationToken);

            var result = new SupplierQuotationHistoryComparisonDto
            {
                SupplierQuotationId =
         request.SupplierQuotationId,

                OldVersion =
         oldHistory.Version,

                LatestVersion =
         latestHistory.Version,

               
                OldTotalPrice =
         oldHistory.TotalPrice,

                LatestTotalPrice =
         latestHistory.TotalPrice,

                TotalPriceDifference =
         latestHistory.TotalPrice -
         oldHistory.TotalPrice,

               
                OldDiscount =
         oldHistory.Discount,

                LatestDiscount =
         latestHistory.Discount,

                DiscountDifference =
         oldHistory.Discount.HasValue &&
         latestHistory.Discount.HasValue
             ? latestHistory.Discount.Value -
               oldHistory.Discount.Value
             : null,

                OldDiscountType =
         oldHistory.DiscountType,

                LatestDiscountType =
         latestHistory.DiscountType,

                
                OldTax =
         oldHistory.Tax,

                LatestTax =
         latestHistory.Tax,

                TaxDifference =
         oldHistory.Tax.HasValue &&
         latestHistory.Tax.HasValue
             ? latestHistory.Tax.Value -
               oldHistory.Tax.Value
             : null,

                OldTaxType =
         oldHistory.TaxType,

                LatestTaxType =
         latestHistory.TaxType,

               
                OldDeliveryCharge =
         oldHistory.DeliveryCharge,

                LatestDeliveryCharge =
         latestHistory.DeliveryCharge,

                DeliveryChargeDifference =
         oldHistory.DeliveryCharge.HasValue &&
         latestHistory.DeliveryCharge.HasValue
             ? latestHistory.DeliveryCharge.Value -
               oldHistory.DeliveryCharge.Value
             : null,

                OldDeliveryType =
         oldHistory.DeliveryType,

                LatestDeliveryType =
         latestHistory.DeliveryType
            };

            
            var oldItems = itemHistories
                .Where(x => x.Version == oldHistory.Version)
                .ToList();

            var latestItems = itemHistories
                .Where(x => x.Version == latestHistory.Version)
                .ToList();

            foreach (var oldItem in oldItems)
            {
                var latestItem = latestItems
                    .FirstOrDefault(x =>
                        x.SupplierRFQItemId == oldItem.SupplierRFQItemId);

                if (latestItem == null)
                {
                    continue;
                }

                result.Items.Add(
                    new SupplierQuotationItemComparisonDto
                    {
                        SupplierQuotationItemId =
                            oldItem.SupplierQuotationItemId,

                        SupplierRFQItemId =
                            oldItem.SupplierRFQItemId,

                        OldVersion =
                            oldItem.Version,

                        OldQuotedPrice =
                            oldItem.QuotedPrice,

                        LatestVersion =
                            latestItem.Version,

                        LatestQuotedPrice =
                            latestItem.QuotedPrice,

                        PriceDifference =
                            latestItem.QuotedPrice -
                            oldItem.QuotedPrice,

                        PriceChanged =
                            oldItem.QuotedPrice !=
                            latestItem.QuotedPrice
                    });
            }

            _logger.LogInfo(
                $"Quotation history comparison completed for " +
                $"SupplierQuotationId: {request.SupplierQuotationId}");

            return result;
        }
    }
}