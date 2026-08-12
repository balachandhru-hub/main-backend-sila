using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.CreateDeliveryLocation
{
    public class CreateDeliveryLocationCommandHandler : IRequestHandler<CreateDeliveryLocationCommand, Guid>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public CreateDeliveryLocationCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<Guid> Handle(CreateDeliveryLocationCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Creating Delivery Location for BuyerId: {request.BuyerId}");

            if (request.BuyerId == Guid.Empty)
            {
                _logger.LogError("BuyerId is empty. Cannot create delivery location.");
                throw new PreConditionFailedCustomException("Invalid buyer information.", "BuyerId is required.");
            }

            _logger.LogInfo($"BuyerId validated. Proceeding to create delivery location: {request.Data.LocationName}");

            var location = new BuyerDeliveryLocation
            {
                Id = Guid.NewGuid(),
                BuyerId = request.BuyerId,
                LocationName = request.Data.LocationName,
                AddressLine1 = request.Data.AddressLine1,
                AddressLine2 = request.Data.AddressLine2,
                City = request.Data.City,
                State = request.Data.State,
                Country = request.Data.Country,
                PinCode = request.Data.PinCode,
                ContactPerson = request.Data.ContactPerson,
                ContactPhone = request.Data.ContactPhone,
                IsDefault = request.Data.IsDefault,
                IsActive = true
            };

            _repository.BuyerDeliveryLocation.Create(location);
            _logger.LogInfo($"Saving delivery location to database for BuyerId: {request.BuyerId}");
            await _repository.SaveAsync();

            _logger.LogInfo($"Delivery Location created successfully. Id: {location.Id}");
            return location.Id;
        }
    }
}
