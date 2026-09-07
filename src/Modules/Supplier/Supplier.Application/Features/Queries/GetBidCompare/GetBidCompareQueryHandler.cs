using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;
using Supplier.Domain.Common;
using Supplier.Domain.Dto;
using Supplier.Domain.Entities;
using Supplier.Infrastructure.Contracts.IRepository;

namespace Supplier.Application.Features.Queries.GetBidCompare
{
    public class GetBidCompareQueryHandler
        : IRequestHandler<GetBidCompareQuery, BidCompareResponseDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetBidCompareQueryHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<BidCompareResponseDto> Handle(
            GetBidCompareQuery request,
            CancellationToken cancellationToken)
        {
            if (request.RFQId == Guid.Empty)
            {
                throw new BadRequestCustomException(
                    "RFQ Id is required.",
                    "Please provide a valid BuyerRFQId to compare bids.");
            }

            _logger.LogInfo(
                $"Fetching Bid Compare for BuyerRFQId: {request.RFQId}");

            // Get Supplier RFQ to know whether this is item-wise or lot-wise
            var supplierRFQ = await _repository.SupplierRFQ
                .FindByCondition(x =>
                    x.BuyerRFQId == request.RFQId &&
                    x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            if (supplierRFQ == null)
            {
                throw new NotFoundCustomException(
                    "Supplier RFQ not found.",
                    $"No Supplier RFQ found for BuyerRFQId: {request.RFQId}.");
            }

            // Get submitted quotation histories
            var histories = await _repository.SupplierQuotationHistory
                .FindByCondition(x =>
                    x.BuyerRFQId == request.RFQId &&
                    x.Status == Common.SUBMITTED_STATUS &&
                    x.IsActive)
                .ToListAsync(cancellationToken);

            if (histories == null || !histories.Any())
            {
                _logger.LogError(
                    $"No quotation history found for BuyerRFQId: {request.RFQId}");

                throw new NotFoundCustomException(
                    "Quotation history not found.",
                    $"No submitted quotation history found for BuyerRFQId: {request.RFQId}.");
            }

            // Get first and latest version for each supplier
            var supplierVersions = histories
                .GroupBy(x => x.SupplierId)
                .Select(g => new
                {
                    SupplierId = g.Key,

                    First = g
                        .OrderBy(x => ParseVersionNumber(x.Version))
                        .First(),

                    Latest = g
                        .OrderByDescending(x => ParseVersionNumber(x.Version))
                        .First()
                })
                .ToList();

            // Get quotation IDs for first/latest versions
            var quotationIds = supplierVersions
                .SelectMany(x => new[]
                {
                    x.First.SupplierQuotationId,
                    x.Latest.SupplierQuotationId
                })
                .Distinct()
                .ToList();

            // For item-wise quotation, item histories will exist.
            // For lot-wise quotation, this will normally be empty.
            var itemHistories = await _repository.SupplierQuotationItemHistory
                .FindByCondition(x =>
                    quotationIds.Contains(x.SupplierQuotationId) &&
                    x.IsActive)
                .ToListAsync(cancellationToken);

            // LineNumber is required only for item-wise quotation.
            var lineNumbers = new Dictionary<Guid, int>();

            if (!supplierRFQ.AddLotOption && itemHistories.Any())
            {
                var supplierRFQItemIds = itemHistories
                    .Select(x => x.SupplierRFQItemId)
                    .Distinct()
                    .ToList();

                lineNumbers = await _repository.SupplierRFQItem
                    .FindByCondition(x =>
                        supplierRFQItemIds.Contains(x.Id) &&
                        x.IsActive)
                    .ToDictionaryAsync(
                        x => x.Id,
                        x => x.LineNumber,
                        cancellationToken);
            }

            // Get supplier names
            var supplierIds = supplierVersions
                .Select(x => x.SupplierId)
                .Distinct()
                .ToList();

            var supplierNames = await _repository.SupplierBusinessProfile
                .FindByCondition(x =>
                    supplierIds.Contains(x.Id) &&
                    x.IsActive)
                .ToDictionaryAsync(
                    x => x.Id,
                    x => x.OrganizationName,
                    cancellationToken);

            var result = new BidCompareResponseDto
            {
                RFQId = request.RFQId,
                RFQNumber = histories.First().RFQNumber,

                // Important for Buyer to know whether it should
                // load RFQItems from Buyer DB.
                AddLotOption = supplierRFQ.AddLotOption
            };

            foreach (var supplierVersion in supplierVersions)
            {
                result.Suppliers.Add(new BidCompareSupplierDto
                {
                    SupplierId = supplierVersion.SupplierId,

                    SupplierName =
                        supplierNames.TryGetValue(
                            supplierVersion.SupplierId,
                            out var name)
                            ? name
                            : null,

                    FirstVersion = MapQuotation(
                        supplierVersion.First,
                        itemHistories,
                        lineNumbers),

                    LatestVersion = MapQuotation(
                        supplierVersion.Latest,
                        itemHistories,
                        lineNumbers)
                });
            }

            _logger.LogInfo(
                $"Bid Compare fetched successfully for BuyerRFQId: {request.RFQId}. " +
                $"Total suppliers: {result.Suppliers.Count}");

            return result;
        }

        private static BidCompareQuotationDto MapQuotation(
            SupplierQuotationHistory history,
            List<SupplierQuotationItemHistory> itemHistories,
            Dictionary<Guid, int> lineNumbers)
        {
            var items = itemHistories
                .Where(x =>
                    x.SupplierQuotationId == history.SupplierQuotationId &&
                    x.Version == history.Version)
                .Select(x => new BidCompareItemDto
                {
                    SupplierQuotationItemId = x.SupplierQuotationItemId,
                    SupplierRFQItemId = x.SupplierRFQItemId,
                    BuyerRFQItemId = x.BuyerRFQItemId,

                    Version = x.Version,

                    QuotedPrice = x.QuotedPrice,
                    DeliveryCharge = x.DeliveryCharge,
                    Tax = x.Tax,
                    Discount = x.Discount,

                    DeliveryType = x.DeliveryType,
                    DiscountType = x.DiscountType,
                    TaxType = x.TaxType,

                    QuotedAmount = x.QuotedAmount,
                    SubTotal = x.SubTotal,

                    LineNumber =
                        lineNumbers.TryGetValue(
                            x.SupplierRFQItemId,
                            out var lineNumber)
                            ? lineNumber
                            : 0
                })
                .OrderBy(x => x.LineNumber)
                .ToList();

            return new BidCompareQuotationDto
            {
                SupplierRFQId = history.SupplierRFQId,
                QuotationId = history.SupplierQuotationId,
                Version = history.Version,

                TotalPrice = history.TotalPrice,
                DeliveryCharge = history.DeliveryCharge,
                Tax = history.Tax,
                Discount = history.Discount,

                DeliveryType = history.DeliveryType,
                DiscountType = history.DiscountType,
                TaxType = history.TaxType,

                Status = history.Status,

                Items = items
            };
        }

        private static int ParseVersionNumber(string version)
        {
            if (!string.IsNullOrWhiteSpace(version) &&
                int.TryParse(
                    version.TrimStart('V', 'v'),
                    out int number))
            {
                return number;
            }

            return 0;
        }
    }
}