using Buyer.Application.Contracts;
using Buyer.Domain.Dto;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.GetBidCompare
{
    public class GetBidCompareQueryHandler
        : IRequestHandler<GetBidCompareQuery, BidCompareResponseDto>
    {
        private readonly IRepositoryWrapper _repositorywrapper;
        private readonly ISupplierApiClient _supplierApiClient;
        private readonly ILoggerManager _logger;

        public GetBidCompareQueryHandler(
            IRepositoryWrapper repositorywrapper,
            ISupplierApiClient supplierApiClient,
            ILoggerManager logger)
        {
            _repositorywrapper = repositorywrapper;
            _supplierApiClient = supplierApiClient;
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
                    "Please provide a valid RFQId to compare bids.");
            }

            _logger.LogInfo(
                $"Fetching Bid Compare for RFQId: {request.RFQId}");

            // Get quotation/history information from Supplier service
            var result = await _supplierApiClient.GetBidCompare(
                request.RFQId,
                cancellationToken);

            // The Supplier microservice only knows registered suppliers, so
            // SupplierName comes back null for external-supplier bids
            // (their SupplierId is a Buyer-side ExternalSupplier.Id, not a
            // SupplierBusinessProfile.Id). Backfill it from Buyer's own
            // ExternalSupplier data.
            var suppliersMissingName = result.Suppliers
                .Where(x => string.IsNullOrWhiteSpace(x.SupplierName))
                .Select(x => x.SupplierId)
                .Distinct()
                .ToList();

            if (suppliersMissingName.Any())
            {
                var externalSupplierIds = await _repositorywrapper.RFQExternalSupplier
                    .FindByCondition(x =>
                        x.RFQId == request.RFQId &&
                        x.IsActive &&
                        suppliersMissingName.Contains(x.ExternalSupplierId))
                    .Select(x => x.ExternalSupplierId)
                    .ToListAsync(cancellationToken);

                var externalSupplierNames = await _repositorywrapper.ExternalSupplier
                    .FindByCondition(x =>
                        externalSupplierIds.Contains(x.Id) &&
                        x.IsActive)
                    .ToDictionaryAsync(
                        x => x.Id,
                        x => x.SupplierName,
                        cancellationToken);

                foreach (var supplier in result.Suppliers)
                {
                    if (string.IsNullOrWhiteSpace(supplier.SupplierName) &&
                        externalSupplierNames.TryGetValue(supplier.SupplierId, out var externalSupplierName))
                    {
                        supplier.SupplierName = externalSupplierName;
                    }
                }
            }

            // For lot-wise RFQ, Supplier does not have
            // SupplierQuotationItemHistory.
            // Get the original RFQ items from Buyer DB.
            if (result.AddLotOption)
            {
                var rfqItems = await _repositorywrapper.RFQItem
                    .FindByCondition(x =>
                        x.RFQId == request.RFQId &&
                        x.IsActive)
                    .OrderBy(x => x.LineNumber)
                    .ToListAsync(cancellationToken);

                result.RFQItems = rfqItems
                    .Select(x => new BidCompareRFQItemDto
                    {
                        Id = x.Id,
                        Description = x.Description,
                        Quantity = x.Quantity,
                        UOM = x.UOM,
                        MaterialCode = x.MaterialCode,
                        MaterialGroup = x.MaterialGroup,
                        CostCenter = x.CostCenter,
                        LineNumber = x.LineNumber
                    })
                    .ToList();
            }

            _logger.LogInfo(
                $"Bid Compare fetched successfully for RFQId: {request.RFQId}");

            return result;
        }
    }
}