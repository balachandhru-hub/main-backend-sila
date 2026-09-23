using Buyer.Application.Contracts;
using Buyer.Domain.Common;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.UnawardRFQ
{
    public class UnawardRFQCommandHandler
        : IRequestHandler<UnawardRFQCommand, bool>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ISupplierApiClient _supplierApiClient;
        private readonly ILoggerManager _logger;

        public UnawardRFQCommandHandler(
            IRepositoryWrapper repository,
            ISupplierApiClient supplierApiClient,
            ILoggerManager logger)
        {
            _repository = repository;
            _supplierApiClient = supplierApiClient;
            _logger = logger;
        }

        public async Task<bool> Handle(
            UnawardRFQCommand request,
            CancellationToken cancellationToken)
        {
            var dto = request.Request;

            if (dto.RFQId == Guid.Empty)
            {
                _logger.LogError("RFQ Id is required. Please provide a valid RFQId to cancel the award.");
                throw new BadRequestCustomException(
                    "RFQ Id is required.",
                    "Please provide a valid RFQId to cancel the award.");
            }

            _logger.LogInfo($"Cancelling RFQ Award for RFQId: {dto.RFQId}");

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

            var award = await _repository.RFQAward
                .FindByCondition(x =>
                    x.RFQId == dto.RFQId &&
                    x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            if (award == null)
            {
                _logger.LogError($"RFQ not awarded. No active award exists for RFQId: {dto.RFQId}");
                throw new NotFoundCustomException(
                    "RFQ not awarded.",
                    $"No active award exists for RFQId: {dto.RFQId}");
            }

            var awardItems = await _repository.RFQAwardItem
                .FindByCondition(x =>
                    x.RFQAwardId == award.Id &&
                    x.IsActive)
                .ToListAsync(cancellationToken);

            award.IsActive = false;
            _repository.RFQAward.Update(award);

            if (awardItems.Any())
            {
                foreach (var item in awardItems)
                {
                    item.IsActive = false;
                }
                _repository.RFQAwardItem.UpdateRange(awardItems);
            }

            rfq.Status = Common.RFQ_FREEZING_STATUS;
            _repository.RFQ.Update(rfq);

            await _repository.SaveAsync();

            // Push the reset to the Supplier service so it can clear its own
            // awarded flags. Buyer award tables stay the source of truth, so
            // a sync failure is logged but does not fail the un-award.
            try
            {
                await _supplierApiClient.ResetSupplierRFQAward(
                    rfq.Id,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"RFQ Award cancelled for RFQId: {dto.RFQId} but failed to " +
                    $"sync reset to the Supplier service. AwardId: {award.Id}, " +
                    $"Error: {ex.Message}");
            }

            _logger.LogInfo(
                $"RFQ Award cancelled successfully for RFQId: {dto.RFQId}. " +
                $"AwardId: {award.Id}");

            return true;
        }
    }
}
