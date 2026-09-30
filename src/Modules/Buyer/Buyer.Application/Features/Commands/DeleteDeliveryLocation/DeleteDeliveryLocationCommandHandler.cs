using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.DeleteDeliveryLocation
{
    public class DeleteDeliveryLocationCommandHandler : IRequestHandler<DeleteDeliveryLocationCommand, Guid>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public DeleteDeliveryLocationCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<Guid> Handle(DeleteDeliveryLocationCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Soft deleting Delivery Location Id: {request.Id} for BuyerId: {request.BuyerId}");

            var location = await _repository.BuyerDeliveryLocation
                .FindByCondition(x => x.Id == request.Id && x.BuyerId == request.BuyerId)
                .FirstOrDefaultAsync(cancellationToken);

            if (location == null)
            {
                _logger.LogError($"Delivery Location not found. Id: {request.Id}, BuyerId: {request.BuyerId}");
                throw new NotFoundCustomException("Delivery location not found.", "The delivery location does not exist or does not belong to the specified buyer.");
            }

            _logger.LogInfo($"Delivery Location found. Marking as inactive. Id: {request.Id}");

           
            _repository.BuyerDeliveryLocation.Delete(location);
            _logger.LogInfo($"Saving soft delete for Delivery Location Id: {request.Id}");
            await _repository.SaveAsync();

            _logger.LogInfo($"Delivery Location soft deleted successfully. Id: {request.Id}");
            return request.Id;
        }
    }
}
