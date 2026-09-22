using Buyer.Domain.Common;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.RFQAttachment
{
    public class UpdateSupplierTermsConditionStatusCommandHandler
        : IRequestHandler<UpdateSupplierTermsConditionStatusCommand, Guid>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public UpdateSupplierTermsConditionStatusCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<Guid> Handle(
            UpdateSupplierTermsConditionStatusCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo(
                $"Updating Supplier Terms and Condition status for RFQId: {request.RFQId}, " +
                $"SupplierId: {request.SupplierId}. Status: {request.Status}");

            if (request.Status != Common.APPROVED && request.Status != Common.REJECTED)
            {
                throw new BadRequestCustomException(
                    "Invalid status.",
                    "Status must be either APPROVE or REJECT.");
            }

            var rfqSupplierMapping = await _repository.RFQSupplierMapping
                .FindByCondition(x =>
                    x.RFQId == request.RFQId &&
                    x.SupplierId == request.SupplierId &&
                    x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            if (rfqSupplierMapping == null)
            {
                _logger.LogError(
                    $"RFQ Supplier mapping not found for RFQId: {request.RFQId}, " +
                    $"SupplierId: {request.SupplierId}");
                throw new NotFoundCustomException(
                    "RFQ not found for the given supplier.",
                    $"No RFQ exists with Id: {request.RFQId} for SupplierId: {request.SupplierId}.");
            }

            rfqSupplierMapping.SupplierTermsAndConditionAccepted = request.Status == Common.APPROVED;
            _repository.RFQSupplierMapping.Update(rfqSupplierMapping);
            await _repository.SaveAsync();

            _logger.LogInfo(
                $"Supplier Terms and Condition status updated for RFQId: {request.RFQId}, " +
                $"SupplierId: {request.SupplierId}. Accepted: {rfqSupplierMapping.SupplierTermsAndConditionAccepted}");

            return rfqSupplierMapping.RFQId;
        }
    }
}
