
using Buyer.Domain.Dto;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.UpdateDeliveryLocation
{
    public class UpdateDeliveryLocationCommandHandler
        : IRequestHandler<UpdateDeliveryLocationCommand, Guid>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public UpdateDeliveryLocationCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<Guid> Handle(
            UpdateDeliveryLocationCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo(
                $"Updating Delivery Location : {request.Id}");
            var data = request.Data;

           
            if (data.BuyerId == Guid.Empty)
            {
                _logger.LogError(
                    "Invalid buyer information. BuyerId is required.");
                throw new PreConditionFailedCustomException(
                    "Invalid buyer information.",
                    "BuyerId is required."
                );
            }

           _logger.LogInfo(
                $"Fetching Delivery Location for BuyerId: {data.BuyerId} and LocationId: {request.Id}");
            var location = await _repository.BuyerDeliveryLocation
                .FindByCondition(x =>
                    x.Id == request.Id &&
                    x.BuyerId == data.BuyerId)
                .FirstOrDefaultAsync(cancellationToken);

           
            if (location == null)
            {
                _logger.LogError(
                    $"Delivery Location NOT found for BuyerId: {data.BuyerId} and LocationId: {request.Id}");
                throw new NotFoundCustomException(
                    "Delivery location not found.",
                    "The delivery location does not exist or does not belong to the specified buyer."
                );
            }

            _logger.LogInfo(
                $"Found Delivery Location for BuyerId: {data.BuyerId} and LocationId: {request.Id}");
           

            if (data.LocationName != null)
            {
                _logger.LogInfo(
                    $"Updating LocationName for Delivery Location Id: {request.Id}");
                location.LocationName = data.LocationName;
            }

            if (data.AddressLine1 != null)
            {
                _logger.LogInfo(
                    $"Updating AddressLine1 for Delivery Location Id: {request.Id}");
                location.AddressLine1 = data.AddressLine1;
            }

            if (data.AddressLine2 != null)
            {
                _logger.LogInfo(
                    $"Updating AddressLine2 for Delivery Location Id: {request.Id}");
                location.AddressLine2 = data.AddressLine2;
            }

            if (data.City != null)
            {
                _logger.LogInfo(
                    $"Updating City for Delivery Location Id: {request.Id}");
                location.City = data.City;
            }

            if (data.State != null)
            {
                _logger.LogInfo(
                    $"Updating State for Delivery Location Id: {request.Id}");
                location.State = data.State;
            }

            if (data.Country != null)
            {
                _logger.LogInfo(
                    $"Updating Country for Delivery Location Id: {request.Id}");
                location.Country = data.Country;
            }

            if (data.PinCode != null)
            {
                _logger.LogInfo(
                    $"Updating PinCode for Delivery Location Id: {request.Id}");
                location.PinCode = data.PinCode;
            }

            if (data.ContactPerson != null)
            {
                _logger.LogInfo(
                    $"Updating ContactPerson for Delivery Location Id: {request.Id}");
                location.ContactPerson = data.ContactPerson;
            }

            if (data.ContactPhone != null)
            {
                _logger.LogInfo(
                    $"Updating ContactPhone for Delivery Location Id: {request.Id}");
                location.ContactPhone = data.ContactPhone;
            }

          if (data.IsDefault.HasValue)
            {
                _logger.LogInfo(
                    $"Updating IsDefault for Delivery Location Id: {request.Id}");

            
                if (data.IsDefault.Value)
                {
                    var existingDefaultLocations = _repository.BuyerDeliveryLocation
                        .FindByCondition(x =>
                            x.BuyerId == data.BuyerId &&
                            x.Id != request.Id &&
                            x.IsDefault &&
                            x.IsActive)
                        .ToList();

                    foreach (var existingLocation in existingDefaultLocations)
                    {
                        existingLocation.IsDefault = false;

                       

                        _logger.LogInfo(
                            $"Existing Default Delivery Location Id: {existingLocation.Id} " +
                            $"changed to IsDefault = false.");
                    }
                      _repository.BuyerDeliveryLocation.UpdateRange(existingDefaultLocations);
                }

                
                location.IsDefault = data.IsDefault.Value;
            }

           _repository.BuyerDeliveryLocation.Update(location);
            _logger.LogInfo(
                $"Saving changes for Delivery Location Id: {request.Id}");
            await _repository.SaveAsync();

            _logger.LogInfo(
                $"Changes saved successfully for Delivery Location Id: {request.Id}");
            return location.Id;
        }
    }
}

