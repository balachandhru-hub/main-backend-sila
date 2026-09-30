using Buyer.Application.Contracts;
using Buyer.Domain.Dto;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.GetRFQAward
{
    public class GetRFQAwardQueryHandler
        : IRequestHandler<GetRFQAwardQuery, RFQAwardResponseDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ISupplierApiClient _supplierApiClient;
        private readonly ILoggerManager _logger;

        public GetRFQAwardQueryHandler(
            IRepositoryWrapper repository,
            ISupplierApiClient supplierApiClient,
            ILoggerManager logger)
        {
            _repository = repository;
            _supplierApiClient = supplierApiClient;
            _logger = logger;
        }

        public async Task<RFQAwardResponseDto> Handle(
            GetRFQAwardQuery request,
            CancellationToken cancellationToken)
        {
            if (request.RFQId == Guid.Empty)
            {
                throw new BadRequestCustomException(
                    "RFQ Id is required.",
                    "Please provide a valid RFQId to fetch the award.");
            }

            _logger.LogInfo(
                $"Fetching RFQ Award for RFQId: {request.RFQId}");

            var award = await _repository.RFQAward
                .FindByCondition(x =>
                    x.RFQId == request.RFQId &&
                    x.IsActive)
                .Include(x => x.RFQ)
                .FirstOrDefaultAsync(cancellationToken);

            if (award == null)
            {
                throw new NotFoundCustomException(
                    "RFQ award not found.",
                    $"No award was found for RFQId: {request.RFQId}");
            }

            // Lot-wise awards store no rfqaward_item rows — every RFQ line is
            // awarded to the single supplier on the award header.
            var items = award.RFQ.AddLotOption
                ? new List<RFQAwardItem>()
                : await _repository.RFQAwardItem
                    .FindByCondition(x =>
                        x.RFQAwardId == award.Id &&
                        x.IsActive)
                    .Include(x => x.RFQItem)
                    .OrderBy(x => x.RFQItem.LineNumber)
                    .ToListAsync(cancellationToken);

            // Pricing/terms live in the Supplier service (supplier_quotation /
            // supplier_quotation_item). The award only stores references, so a
            // supplier outage degrades the response to reference data only.
            BidCompareResponseDto? bidCompare = null;
            try
            {
                bidCompare = await _supplierApiClient.GetBidCompare(
                    request.RFQId,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"RFQ Award fetched for RFQId: {request.RFQId} but failed " +
                    $"to load quotation data from the Supplier service. " +
                    $"AwardId: {award.Id}, Error: {ex.Message}");
            }

            var quotations = (bidCompare?.Suppliers ?? new List<BidCompareSupplierDto>())
                .SelectMany(x => new[] { x.LatestVersion, x.FirstVersion })
                .Where(x => x != null)
                .GroupBy(x => x.QuotationId)
                .ToDictionary(x => x.Key, x => x.First());

            var quotationItems = quotations.Values
                .SelectMany(x => x.Items)
                .GroupBy(x => x.SupplierQuotationItemId)
                .ToDictionary(x => x.Key, x => x.First());

            BidCompareQuotationDto? awardedQuotation = null;
            if (award.SupplierQuotationId.HasValue)
            {
                quotations.TryGetValue(
                    award.SupplierQuotationId.Value, out awardedQuotation);
            }

            List<RFQAwardItemDto> itemDtos;

            if (award.RFQ.AddLotOption)
            {
                var rfqItems = await _repository.RFQItem
                    .FindByCondition(x =>
                        x.RFQId == award.RFQId &&
                        x.IsActive)
                    .OrderBy(x => x.LineNumber)
                    .ToListAsync(cancellationToken);

                itemDtos = rfqItems
                    .Select(item => new RFQAwardItemDto
                    {
                        Id = Guid.Empty,
                        RFQItemId = item.Id,
                        LineNumber = item.LineNumber,
                        SupplierId = award.SupplierId ?? Guid.Empty,
                        SupplierRFQId = awardedQuotation?.SupplierRFQId,
                        SupplierQuotationId = award.SupplierQuotationId,
                        QuotationVersion = award.QuotationVersion,
                        Quantity = item.Quantity
                    })
                    .ToList();
            }
            else
            {
                itemDtos = items
                    .Select(x =>
                    {
                        BidCompareItemDto? bidItem = null;
                        if (x.SupplierQuotationItemId.HasValue)
                        {
                            quotationItems.TryGetValue(
                                x.SupplierQuotationItemId.Value, out bidItem);
                        }

                        BidCompareQuotationDto? quotation = null;
                        if (x.SupplierQuotationId.HasValue)
                        {
                            quotations.TryGetValue(
                                x.SupplierQuotationId.Value, out quotation);
                        }

                        return new RFQAwardItemDto
                        {
                            Id = x.Id,
                            RFQItemId = x.RFQItemId,
                            LineNumber = x.RFQItem.LineNumber,
                            SupplierId = x.SupplierId,
                            SupplierRFQId = quotation?.SupplierRFQId,
                            SupplierQuotationId = x.SupplierQuotationId,
                            SupplierQuotationItemId = x.SupplierQuotationItemId,
                            SupplierRFQItemId = bidItem?.SupplierRFQItemId,
                            QuotationVersion = x.QuotationVersion,
                            Quantity = x.RFQItem.Quantity,
                            QuotedPrice = bidItem?.QuotedPrice,
                            QuotedAmount = bidItem?.QuotedAmount,
                            Discount = bidItem?.Discount,
                            DiscountType = bidItem?.DiscountType,
                            Tax = bidItem?.Tax,
                            TaxType = bidItem?.TaxType,
                            DeliveryCharge = bidItem?.DeliveryCharge,
                            DeliveryType = bidItem?.DeliveryType,
                            SubTotal = bidItem?.SubTotal
                        };
                    })
                    .ToList();
            }

            return new RFQAwardResponseDto
            {
                Id = award.Id,
                RFQId = award.RFQId,
                RFQNumber = award.RFQ.RFQNumber,
                BuyerId = award.RFQ.BuyerId,
                TotalAwardValue = awardedQuotation != null
                    ? awardedQuotation.TotalPrice
                    : itemDtos.Sum(x => x.SubTotal ?? 0),
                AwardedItems = itemDtos.Count,
                AwardedSuppliers = itemDtos
                    .Select(x => x.SupplierId)
                    .Distinct()
                    .Count(),
                Currency = award.RFQ.Currency,
                Status = award.Status,
                Discount = awardedQuotation?.Discount,
                DiscountType = awardedQuotation?.DiscountType,
                Tax = awardedQuotation?.Tax,
                TaxType = awardedQuotation?.TaxType,
                DeliveryCharge = awardedQuotation?.DeliveryCharge,
                DeliveryType = awardedQuotation?.DeliveryType,
                Remarks = award.Remarks,
                AwardedOn = award.DateCreated,
                Items = itemDtos
            };
        }
    }
}
