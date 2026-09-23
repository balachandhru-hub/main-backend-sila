using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;
using Supplier.Domain.Common;
using Supplier.Infrastructure.Contracts.IRepository;

namespace Supplier.Application.Features.Commands.ResetSupplierRFQAward
{
    public class ResetSupplierRFQAwardCommandHandler
        : IRequestHandler<ResetSupplierRFQAwardCommand, bool>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public ResetSupplierRFQAwardCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<bool> Handle(
            ResetSupplierRFQAwardCommand request,
            CancellationToken cancellationToken)
        {
            var dto = request.Request;

            if (dto.BuyerRFQId == Guid.Empty)
            {
                _logger.LogError(
                    "BuyerRFQId is required to reset the supplier RFQ award.");
                throw new BadRequestCustomException(
                    "RFQ Id is required.",
                    "Please provide a valid BuyerRFQId to reset the award.");
            }

            _logger.LogInfo(
                $"Resetting supplier RFQ award for BuyerRFQId: {dto.BuyerRFQId}");

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
                supplierRfq.Status = Common.RFQ_FREEZING_STATUS;
            }
            _repository.SupplierRFQ.UpdateRange(supplierRfqs);

            var supplierRfqIds = supplierRfqs
                .Select(x => x.Id)
                .ToList();

            var awardedItems = await _repository.SupplierRFQItem
                .FindByCondition(x =>
                    supplierRfqIds.Contains(x.SupplierRFQId) &&
                    x.IsAwarded &&
                    x.IsActive)
                .ToListAsync(cancellationToken);

            if (awardedItems.Any())
            {
                foreach (var item in awardedItems)
                {
                    item.IsAwarded = false;
                    item.AwardedSupplierId = null;
                }
                _repository.SupplierRFQItem.UpdateRange(awardedItems);

                _logger.LogInfo(
                    $"Cleared award on {awardedItems.Count} supplier RFQ item(s) " +
                    $"for BuyerRFQId: {dto.BuyerRFQId}");
            }

            await _repository.SaveAsync();

            _logger.LogInfo(
                $"Supplier RFQ award reset for BuyerRFQId: {dto.BuyerRFQId}");

            return true;
        }
    }
}
