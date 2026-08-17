using Buyer.Domain.Common;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.Invitation
{
    public class UpdateSupplierVerificationStatusCommandHandler
        : IRequestHandler<UpdateSupplierVerificationStatusCommand, bool>
    {
        private readonly IRepositoryWrapper _repository;
         private readonly ILoggerManager _logger;

        public UpdateSupplierVerificationStatusCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<bool> Handle(
            UpdateSupplierVerificationStatusCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Updating Supplier Verification Status for RequestId: {request.RequestId}");
            if (!request.Status.Equals(Common.ACCEPT, StringComparison.OrdinalIgnoreCase) &&
                !request.Status.Equals(Common.DECLINE, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError(
                    $"Invalid Status: {request.Status} for RequestId: {request.RequestId}");
                throw new BadRequestCustomException(
                    "Invalid Status",
                    "Status must be Accept or Reject.");
            }

            var verificationRequest =
                await _repository.SupplierVerificationRequest
                    .FindByCondition(x => x.Id == request.RequestId)
                    .FirstOrDefaultAsync(cancellationToken);

            if (verificationRequest == null)
            {
                _logger.LogError(
                    $"Supplier Verification Request not found for RequestId: {request.RequestId}");
                throw new BadRequestCustomException(
                    "Not Found",
                    "Supplier verification request not found.");
            }

            verificationRequest.Status =
                request.Status.Equals(Common.ACCEPT, StringComparison.OrdinalIgnoreCase)
                    ? Common.ACCEPT
                    : Common.DECLINE;

            verificationRequest.Remarks = request.Remarks;

            _repository.SupplierVerificationRequest.Update(verificationRequest);

            await _repository.SaveAsync();

            return true;
        }
    }
}