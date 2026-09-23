using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using Supplier.Domain.Common;
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
            var supplierRFQForRanking = await _repository.SupplierRFQ
                .FindByCondition(x =>
                    x.BuyerRFQId == request.RFQId &&
                    x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            if (supplierRFQForRanking == null)
            {
                throw new NotFoundCustomException(
                    "Supplier RFQ not found.",
                    $"No active Supplier RFQ found for BuyerRFQId: {request.RFQId}");
            }

            bool addLotOption = supplierRFQForRanking.AddLotOption;


            var quotations = await _repository.SupplierQuotation
                .FindByCondition(x =>
                    x.BuyerRFQId == request.RFQId &&
                    x.SupplierId == supplierId &&
                    x.IsActive)
                .ToListAsync(cancellationToken);
            // Ranking must only ever consider quotations that were actually
            // submitted - every invited supplier gets a DRAFT quotation
            // automatically when the RFQ is created, and without this
            // filter those untouched drafts get ranked alongside the real
            // submissions (and, with a null/zero TotalPrice, can easily
            // outrank the one supplier who actually bid).
            var allQuotations = await _repository.SupplierQuotation
            .FindByCondition(x =>
                x.BuyerRFQId == request.RFQId &&
                x.IsActive &&
                x.Status == Common.SUBMITTED)
            .ToListAsync(cancellationToken);
            var quotationRankings = new Dictionary<Guid, string>();

            if (addLotOption)
            {
                quotationRankings = allQuotations
                    .OrderBy(x => x.TotalPrice)
                    .Select((x, index) => new
                    {
                        QuotationId = x.Id,
                        Rank = $"L{index + 1}"
                    })
                    .ToDictionary(x => x.QuotationId, x => x.Rank);
            }
            var lowestQuotation = await _repository.SupplierQuotation
            .FindByCondition(x =>
                x.BuyerRFQId == request.RFQId &&
                x.IsActive &&
                x.Status == Common.SUBMITTED)
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
            var quotationItemRankings = new Dictionary<Guid, string>();

            if (!addLotOption)
            {
                var quotationIds = allQuotations
                    .Select(x => x.Id)
                    .ToList();

                var allQuotationItems = await _repository.SupplierQuotationItem
                    .FindByCondition(x =>
                        quotationIds.Contains(x.SupplierQuotationId) &&
                        x.IsActive)
                    .ToListAsync(cancellationToken);

                quotationItemRankings = allQuotationItems
                    .GroupBy(x => x.BuyerRFQItemId)
                    .SelectMany(group =>
                        group
                            .OrderBy(x => x.SubTotal)
                            .Select((x, index) => new
                            {
                                QuotationItemId = x.Id,
                                Rank = $"L{index + 1}"
                            }))
                    .ToDictionary(x => x.QuotationItemId, x => x.Rank);
            }

            var response = new GetAllSupplierQuotationBySupplierIdDto();


            foreach (var quotation in quotations)
            {
                _logger.LogInfo($"Processing QuotationId: {quotation.Id} for SupplierId: {supplierId}");
                var supplierRFQ = await _repository.SupplierRFQ
                   .FindByCondition(x =>
                       x.Id == quotation.SupplierRFQId &&
                       x.IsActive)
                   .FirstOrDefaultAsync(cancellationToken);
                var quotationItems = await _repository.SupplierQuotationItem
                    .FindByCondition(x =>
                        x.SupplierQuotationId == quotation.Id &&
                        x.IsActive)
                    .OrderBy(x => x.SupplierRFQItem.LineNumber)
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
                        LineNumber = x.SupplierRFQItem.LineNumber,
                        IsAwarded = x.SupplierRFQItem.IsAwarded && x.SupplierRFQItem.AwardedSupplierId == supplierId


                    })
                    .ToListAsync(cancellationToken);
                if (!addLotOption)
                {
                    foreach (var item in quotationItems)
                    {
                        if (quotationItemRankings.TryGetValue(item.ItemQuotationId, out var itemRank))
                        {
                            item.Rank = itemRank;
                        }
                    }
                }

                var isLotAwarded = addLotOption && quotationItems.Any(i => i.IsAwarded);

                if (addLotOption)
                {
                    foreach (var item in quotationItems)
                    {
                        item.IsAwarded = false;
                    }
                }

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
                    Currency = supplierRFQ?.Currency,

                    DeliveryType = quotation.DeliveryType,
                    Status = quotation.Status,
                    SupplierQuotationItems = quotationItems,
                    Rank = addLotOption &&
       quotationRankings.TryGetValue(quotation.Id, out var quotationRank)
    ? quotationRank
    : null,

                    IsLead = quotation.Id == lowestQuotation?.Id,
                    IsAwarded = isLotAwarded
                };

                response.Suppliers.Add(supplierDto);
            }
            response.Suppliers = response.Suppliers
            .OrderBy(x => x.TotalPrice)
            .ToList();
            _logger.LogInfo($"Successfully fetched {response.Suppliers.Count} Supplier Quotations for BuyerRFQId: {request.RFQId} and SupplierId: {supplierId}");
            return response;
        }
    }
}