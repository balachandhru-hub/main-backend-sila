using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.ExceptionHandler;

namespace Buyer.Application.Features.Commands.UpdateVerificationRequestStatus
{
    public class UpdateVerificationRequestStatusCommandHandler
        : IRequestHandler<UpdateVerificationRequestStatusCommand, bool>
    {
        private readonly IRepositoryWrapper _repository;

        public UpdateVerificationRequestStatusCommandHandler(
            IRepositoryWrapper repository)
        {
            _repository = repository;
        }

        public async Task<bool> Handle(
            UpdateVerificationRequestStatusCommand request,
            CancellationToken cancellationToken)
        {
            var verificationRequest =
                _repository.SupplierVerificationRequest.FindFirstByCondition(
                    x => x.Id == request.Request.VerificationRequestId &&
                         x.IsActive);

            if (verificationRequest == null)
                throw new NotFoundCustomException(
                    "Verification request not found.",
                    "Verification request not found.");

            verificationRequest.Status = request.Request.Status;

            _repository.SupplierVerificationRequest.Update(verificationRequest);

            await _repository.SaveAsync();

            return true;
        }
    }
}