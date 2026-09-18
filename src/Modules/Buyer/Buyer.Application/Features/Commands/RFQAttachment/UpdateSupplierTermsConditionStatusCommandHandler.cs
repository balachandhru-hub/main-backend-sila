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
                $"Updating Supplier Terms and Condition status for RFQId: {request.RFQId}. " +
                $"Status: {request.Status}");

            if (request.Status != Common.APPROVED && request.Status != Common.REJECTED)
            {
                throw new BadRequestCustomException(
                    "Invalid status.",
                    "Status must be either APPROVE or REJECT.");
            }

            var rfq = await _repository.RFQ
                .FindByCondition(x => x.Id == request.RFQId)
                .FirstOrDefaultAsync(cancellationToken);

            if (rfq == null)
            {
                _logger.LogError($"RFQ not found for RFQId: {request.RFQId}");
                throw new NotFoundCustomException(
                    "RFQ not found.",
                    $"No RFQ exists with Id: {request.RFQId}.");
            }

            rfq.SupplierTermsAndConditionAccepted = request.Status == Common.APPROVED;
            _repository.RFQ.Update(rfq);
            await _repository.SaveAsync();

            _logger.LogInfo(
                $"Supplier Terms and Condition status updated for RFQId: {request.RFQId}. " +
                $"Accepted: {rfq.SupplierTermsAndConditionAccepted}");

            return rfq.Id;
        }
    }
}
