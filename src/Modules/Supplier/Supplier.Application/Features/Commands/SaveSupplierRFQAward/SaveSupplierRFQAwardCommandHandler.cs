using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;
using Supplier.Domain.Common;
using Supplier.Infrastructure.Contracts.IRepository;

namespace Supplier.Application.Features.Commands.SaveSupplierRFQAward
{
    public class SaveSupplierRFQAwardCommandHandler
        : IRequestHandler<SaveSupplierRFQAwardCommand, bool>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public SaveSupplierRFQAwardCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<bool> Handle(
            SaveSupplierRFQAwardCommand request,
            CancellationToken cancellationToken)
        {
            var dto = request.Request;

            if (dto.BuyerRFQId == Guid.Empty)
            {
                _logger.LogError(
                    "BuyerRFQId is required to save the supplier RFQ award.");
                throw new BadRequestCustomException(
                    "RFQ Id is required.",
                    "Please provide a valid BuyerRFQId to save the award.");
            }

            _logger.LogInfo(
                $"Saving supplier RFQ award for BuyerRFQId: {dto.BuyerRFQId}");

            // One BuyerRFQ fans out to one SupplierRFQ per invited supplier —
            // mark the awarded status on every supplier's copy.
            var supplierRfqs = await _repository.SupplierRFQ
                .FindByCondition(x =>
                    x.BuyerRFQId == dto.BuyerRFQId &&
                    x.IsActive)
                .ToListAsync(cancellationToken);

            if (!supplierRfqs.Any())
            {
                _logger.LogError(
                    $"No supplier RFQ found for BuyerRFQId: {dto.BuyerRFQId}");
                throw new NotFoundCustomException(
                    "Supplier RFQ not found.",
                    $"No supplier RFQ exists for BuyerRFQId: {dto.BuyerRFQId}");
            }

            foreach (var supplierRfq in supplierRfqs)
            {
                supplierRfq.Status = Common.AWARDED_STATUS;
            }
            _repository.SupplierRFQ.UpdateRange(supplierRfqs);

            var winnerByItem = dto.Items
                .GroupBy(x => x.BuyerRFQItemId)
                .ToDictionary(x => x.Key, x => x.First().SupplierId);

            if (winnerByItem.Any())
            {
                var supplierRfqIds = supplierRfqs
                    .Select(x => x.Id)
                    .ToList();

                var awardedItemIds = winnerByItem.Keys.ToList();

                var rfqItems = await _repository.SupplierRFQItem
                    .FindByCondition(x =>
                        supplierRfqIds.Contains(x.SupplierRFQId) &&
                        awardedItemIds.Contains(x.BuyerRFQItemId) &&
                        x.IsActive)
                    .ToListAsync(cancellationToken);

                foreach (var item in rfqItems)
                {
                    item.IsAwarded = true;
                    item.AwardedSupplierId =
                        winnerByItem[item.BuyerRFQItemId];
                }
                _repository.SupplierRFQItem.UpdateRange(rfqItems);

                _logger.LogInfo(
                    $"Marked {rfqItems.Count} supplier RFQ item(s) as awarded " +
                    $"for BuyerRFQId: {dto.BuyerRFQId}");
            }

            await _repository.SaveAsync();

            _logger.LogInfo(
                $"Supplier RFQ award saved for BuyerRFQId: {dto.BuyerRFQId}");

            return true;
        }
    }
}
