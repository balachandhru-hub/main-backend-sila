using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.UpdateVerificationRequestStatus
{
    public class UpdateVerificationRequestStatusCommandHandler
        : IRequestHandler<UpdateVerificationRequestStatusCommand, bool>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public UpdateVerificationRequestStatusCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<bool> Handle(
            UpdateVerificationRequestStatusCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Updating Verification Request Status : {request.Request.VerificationRequestId} to {request.Request.Status}");
            var verificationRequest =
                _repository.SupplierVerificationRequest.FindFirstByCondition(
                    x => x.Id == request.Request.VerificationRequestId &&
                         x.IsActive);
            _logger.LogInfo($"Verification request fetched successfully : {request.Request.VerificationRequestId}");

            if (verificationRequest == null)
            {
                _logger.LogError($"Verification request not found for RequestId : {request.Request.VerificationRequestId}");
                throw new NotFoundCustomException(
                    "Verification request not found.",
                    "Verification request not found.");
            }


            verificationRequest.Status = request.Request.Status;

            _repository.SupplierVerificationRequest.Update(verificationRequest);

            await _repository.SaveAsync();

            return true;
        }
    }
}