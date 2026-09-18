using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;
using Supplier.Domain.Common;
using Supplier.Infrastructure.Contracts.IRepository;

namespace Supplier.Application.Features.Commands.RFQAttachment
{
    public class UpdateBuyerTermsConditionStatusCommandHandler
        : IRequestHandler<UpdateBuyerTermsConditionStatusCommand, Guid>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public UpdateBuyerTermsConditionStatusCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<Guid> Handle(
            UpdateBuyerTermsConditionStatusCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo(
                $"Updating Buyer Terms and Condition status for BuyerRFQId: {request.RFQId}. " +
                $"Status: {request.Status}");

            if (request.Status != Common.APPROVED && request.Status != Common.REJECTED)
            {
                throw new BadRequestCustomException(
                    "Invalid status.",
                    "Status must be either APPROVE or REJECT.");
            }

            var supplier = _repository.SupplierBusinessProfile
                .FindFirstByCondition(x =>
                    x.OrganizationId == request.OrganizationId &&
                    x.IsActive);

            if (supplier == null)
            {
                _logger.LogError(
                    $"Supplier not found for OrganizationId: {request.OrganizationId}");
                throw new PreConditionFailedCustomException(
                    "Supplier not found.",
                    $"Supplier not found for the given organization : {request.OrganizationId}");
            }

            // Scoped by SupplierId as well as BuyerRFQId: one BuyerRFQ fans
            // out to one SupplierRFQ per invited supplier, so the caller
            // must only ever be able to update their own RFQ.
            var supplierRFQ = await _repository.SupplierRFQ
                .FindByCondition(x =>
                    x.BuyerRFQId == request.RFQId &&
                    x.SupplierId == supplier.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (supplierRFQ == null)
            {
                _logger.LogError(
                    $"RFQ not found for BuyerRFQId: {request.RFQId} and SupplierId: {supplier.Id}");
                throw new NotFoundCustomException(
                    "RFQ not found.",
                    $"No RFQ exists with BuyerRFQId: {request.RFQId} for this supplier.");
            }

            supplierRFQ.BuyerTermsAndConditionAccepted = request.Status == Common.APPROVED;
            _repository.SupplierRFQ.Update(supplierRFQ);
            await _repository.SaveAsync();

            _logger.LogInfo(
                $"Buyer Terms and Condition status updated for BuyerRFQId: {request.RFQId}. " +
                $"Accepted: {supplierRFQ.BuyerTermsAndConditionAccepted}");

            return supplierRFQ.Id;
        }
    }
}
