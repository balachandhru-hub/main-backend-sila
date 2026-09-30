using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;
using Buyer.Domain.Dto;

namespace Buyer.Application.Features.Commands.Buyer.UpdateBuyerStatusOrganization
{
    public class UpdateBuyerStatusOrganizationCommandHandler
        : IRequestHandler<UpdateBuyerStatusOrganizationCommand, bool>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public UpdateBuyerStatusOrganizationCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<bool> Handle(
            UpdateBuyerStatusOrganizationCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo(
                $"Updating buyer status for Organization : {request.Buyer.OrganizationId}");

            var buyer = _repository.BuyerBusinessProfile
                .FindFirstByCondition(x =>
                    x.OrganizationId == request.Buyer.OrganizationId);

            if (buyer == null)
            {
                _logger.LogError(
                    $"Buyer not found for Organization : {request.Buyer.OrganizationId}");

                throw new NotFoundCustomException(
                    "Buyer not found.",
                    "Buyer does not exist.");
            }

            if (buyer.IsActive != request.Buyer.IsActive)
            {
                _logger.LogInfo(
                    $"Updating Buyer IsActive from {buyer.IsActive} to {request.Buyer.IsActive}");

                buyer.IsActive = request.Buyer.IsActive;
            }

            _repository.BuyerBusinessProfile.Update(buyer);

            await _repository.SaveAsync();

            _logger.LogInfo(
                $"Buyer status updated successfully : {buyer.Id}");

            return true;
        }
    }
}