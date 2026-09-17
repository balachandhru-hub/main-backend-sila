using Buyer.Application.Contracts;
using Buyer.Domain.Common;
using Buyer.Domain.Dto;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.SaveRFQAward
{
    public class SaveRFQAwardCommandHandler
        : IRequestHandler<SaveRFQAwardCommand, Guid>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ISupplierApiClient _supplierApiClient;
        private readonly ILoggerManager _logger;

        public SaveRFQAwardCommandHandler(
            IRepositoryWrapper repository,
            ISupplierApiClient supplierApiClient,
            ILoggerManager logger)
        {
            _repository = repository;
            _supplierApiClient = supplierApiClient;
            _logger = logger;
        }

        public async Task<Guid> Handle(
            SaveRFQAwardCommand request,
            CancellationToken cancellationToken)
        {
            var dto = request.Request;

            if (dto.RFQId == Guid.Empty)
            {
                _logger.LogError("RFQ Id is required. Please provide a valid RFQId to save the award.");
                throw new BadRequestCustomException(
                    "RFQ Id is required.",
                    "Please provide a valid RFQId to save the award.");
            }

            if (dto.Selections == null || !dto.Selections.Any())
            {
                _logger.LogError("Selections are required. Please provide at least one line item selection to award.");
                throw new BadRequestCustomException(
                    "Selections are required.",
                    "Please provide at least one line item selection to award.");
            }

            if (dto.Selections.Select(x => x.RFQItemId).Distinct().Count()
                != dto.Selections.Count)
            {
                _logger.LogError("Duplicate line item selection. Each line item can be awarded to only one supplier.");
                throw new BadRequestCustomException(
                    "Duplicate line item selection.",
                    "Each line item can be awarded to only one supplier.");
            }

            _logger.LogInfo($"Saving RFQ Award for RFQId: {dto.RFQId}");

            var rfq = await _repository.RFQ
                .FindByCondition(x =>
                    x.Id == dto.RFQId &&
                    x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            if (rfq == null)
            {
                _logger.LogError($"RFQ not found. No RFQ was found with RFQId: {dto.RFQId}");
                throw new NotFoundCustomException(
                    "RFQ not found.",
                    $"No RFQ was found with RFQId: {dto.RFQId}");
            }

            var alreadyAwarded = await _repository.RFQAward
                .FindByCondition(x =>
                    x.RFQId == dto.RFQId &&
                    x.IsActive)
                .AnyAsync(cancellationToken);

            if (alreadyAwarded)
            {
                _logger.LogError($"RFQ already awarded. An active award already exists for RFQId: {dto.RFQId}");
                throw new BadRequestCustomException(
                    "RFQ already awarded.",
                    $"An active award already exists for RFQId: {dto.RFQId}");
            }

            // Quotation/bid data lives in the Supplier service.
            var bidCompare = await _supplierApiClient.GetBidCompare(
                dto.RFQId,
                cancellationToken);

            var award = new RFQAward
            {
                Id = Guid.NewGuid(),
                RFQId = rfq.Id,
                Status = Common.RFQ_AWARDED_STATUS,
                Remarks = dto.Remarks
            };

            List<RFQAwardItem> awardItems;
            List<SupplierRFQAwardItemRequestDto> syncItems;

            if (bidCompare.AddLotOption)
            {
                // Lot-wise awards write no rfqaward_item rows: the whole RFQ
                // goes to the single supplier referenced on the award header.
                awardItems = new List<RFQAwardItem>();
                syncItems = await ApplyLotAward(
                    dto, bidCompare, rfq, award, cancellationToken);
            }
            else
            {
                awardItems = await BuildItemAwardItems(
                    dto, bidCompare, rfq, award, cancellationToken);
                syncItems = awardItems
                    .Select(x => new SupplierRFQAwardItemRequestDto
                    {
                        BuyerRFQItemId = x.RFQItemId,
                        SupplierId = x.SupplierId
                    })
                    .ToList();
            }

            _repository.RFQAward.Create(award);
            _repository.RFQAwardItem.CreateRange(awardItems);

            rfq.Status = Common.RFQ_AWARDED_STATUS;
            _repository.RFQ.Update(rfq);

            await _repository.SaveAsync();

            // Push the award to the Supplier service so it can flag its own
            // line items. Buyer award tables stay the source of truth, so a
            // sync failure is logged but does not fail the award save.
            try
            {
                await _supplierApiClient.SaveSupplierRFQAward(
                    new SupplierRFQAwardRequestDto
                    {
                        BuyerRFQId = rfq.Id,
                        Items = syncItems
                    },
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"RFQ Award saved for RFQId: {dto.RFQId} but failed to " +
                    $"sync award to the Supplier service. AwardId: {award.Id}, " +
                    $"Error: {ex.Message}");
            }

            _logger.LogInfo(
                $"RFQ Award saved successfully for RFQId: {dto.RFQId}. " +
                $"AwardId: {award.Id}");

            return award.Id;
        }

        private async Task<List<RFQAwardItem>> BuildItemAwardItems(
            SaveRFQAwardDto dto,
            BidCompareResponseDto bidCompare,
            RFQ rfq,
            RFQAward award,
            CancellationToken cancellationToken)
        {
            var itemIds = dto.Selections
                .Select(x => x.RFQItemId)
                .Distinct()
                .ToList();

            var rfqItems = await _repository.RFQItem
                .FindByCondition(x =>
                    itemIds.Contains(x.Id) &&
                    x.RFQId == rfq.Id &&
                    x.IsActive)
                .ToListAsync(cancellationToken);

            if (rfqItems.Count != itemIds.Count)
            {
                _logger.LogError($"Invalid line item selection. One or more selected line items do not belong to RFQId: {rfq.Id}");
                throw new BadRequestCustomException(
                    "Invalid line item selection.",
                    "One or more selected line items do not belong to this RFQ.");
            }

            var supplierMap = bidCompare.Suppliers
                .ToDictionary(x => x.SupplierId);

            var awardItems = new List<RFQAwardItem>();

            foreach (var selection in dto.Selections)
            {
                if (!supplierMap.TryGetValue(
                        selection.SupplierId,
                        out var supplier) ||
                    supplier.LatestVersion == null)
                {
                    _logger.LogError($"Invalid supplier selection. Supplier {selection.SupplierId} has no submitted bid for RFQId: {rfq.Id}");
                    throw new BadRequestCustomException(
                        "Invalid supplier selection.",
                        $"Supplier {selection.SupplierId} has no submitted bid for this RFQ.");
                }

                var bidItem = supplier.LatestVersion.Items
                    .FirstOrDefault(x =>
                        x.BuyerRFQItemId == selection.RFQItemId);

                if (bidItem == null)
                {
                    _logger.LogError($"Invalid line item selection. Supplier {selection.SupplierId} has no bid for line item {selection.RFQItemId} in RFQId: {rfq.Id}");
                    throw new BadRequestCustomException(
                        "Invalid line item selection.",
                        $"Supplier {selection.SupplierId} has no bid for line item {selection.RFQItemId}.");
                }

                var rfqItem = rfqItems.First(x => x.Id == selection.RFQItemId);

                awardItems.Add(new RFQAwardItem
                {
                    Id = Guid.NewGuid(),
                    RFQAwardId = award.Id,
                    RFQItemId = rfqItem.Id,
                    SupplierId = selection.SupplierId,
                    SupplierQuotationId = supplier.LatestVersion.QuotationId,
                    SupplierQuotationItemId = bidItem.SupplierQuotationItemId,
                    QuotationVersion = !string.IsNullOrWhiteSpace(bidItem.Version)
                        ? bidItem.Version
                        : supplier.LatestVersion.Version
                });
            }

            return awardItems;
        }

        private async Task<List<SupplierRFQAwardItemRequestDto>> ApplyLotAward(
            SaveRFQAwardDto dto,
            BidCompareResponseDto bidCompare,
            RFQ rfq,
            RFQAward award,
            CancellationToken cancellationToken)
        {
            var supplierIds = dto.Selections
                .Select(x => x.SupplierId)
                .Distinct()
                .ToList();

            if (supplierIds.Count != 1)
            {
                _logger.LogError($"Invalid supplier selection. A lot-wise RFQ must be awarded to a single supplier. RFQId: {rfq.Id}, SupplierIds: {string.Join(", ", supplierIds)}");
                throw new BadRequestCustomException(
                    "Invalid supplier selection.",
                    "A lot-wise RFQ must be awarded to a single supplier.");
            }

            var supplier = bidCompare.Suppliers
                .FirstOrDefault(x => x.SupplierId == supplierIds[0]);

            if (supplier?.LatestVersion == null)
            {
                _logger.LogError($"Invalid supplier selection. Supplier {supplierIds[0]} has no submitted bid for RFQId: {rfq.Id}");
                throw new BadRequestCustomException(
                    "Invalid supplier selection.",
                    $"Supplier {supplierIds[0]} has no submitted bid for this RFQ.");
            }

            award.SupplierId = supplier.SupplierId;
            award.SupplierQuotationId = supplier.LatestVersion.QuotationId;
            award.QuotationVersion = supplier.LatestVersion.Version;

            // A lot award covers every buyer line item. The supplier service
            // still needs the per-item pairs to flag its supplier_rfqitem rows.
            var rfqItems = await _repository.RFQItem
                .FindByCondition(x =>
                    x.RFQId == rfq.Id &&
                    x.IsActive)
                .ToListAsync(cancellationToken);

            return rfqItems
                .Select(item => new SupplierRFQAwardItemRequestDto
                {
                    BuyerRFQItemId = item.Id,
                    SupplierId = supplier.SupplierId
                })
                .ToList();
        }
    }
}
