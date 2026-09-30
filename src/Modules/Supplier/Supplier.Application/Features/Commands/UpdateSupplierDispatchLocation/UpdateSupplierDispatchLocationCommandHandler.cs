using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;
using Supplier.Infrastructure.Contracts.IRepository;

namespace Supplier.Application.Features.Commands.UpdateSupplierDispatchLocation
{
    public class UpdateSupplierDispatchLocationCommandHandler
        : IRequestHandler<UpdateSupplierDispatchLocationCommand, Guid>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public UpdateSupplierDispatchLocationCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<Guid> Handle(
            UpdateSupplierDispatchLocationCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo(
                $"Updating Supplier Dispatch Location: {request.Id}");

            var data = request.Data;

            // --------------------------------------------------------
            // Validate Supplier
            // --------------------------------------------------------

            if (data.SupplierId == Guid.Empty)
            {
                _logger.LogError(
                    "Invalid supplier information. SupplierId is required.");

                throw new PreConditionFailedCustomException(
                    "Invalid supplier information.",
                    "SupplierId is required.");
            }

            // --------------------------------------------------------
            // Get Dispatch Location
            // --------------------------------------------------------

            _logger.LogInfo(
                $"Fetching Dispatch Location for SupplierId: {data.SupplierId} " +
                $"and LocationId: {request.Id}");

            var location = await _repository.SupplierDispatchLocation
                .FindByCondition(x =>
                    x.Id == request.Id &&
                    x.SupplierId == data.SupplierId &&
                    x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            if (location == null)
            {
                _logger.LogError(
                    $"Dispatch Location NOT found for SupplierId: {data.SupplierId} " +
                    $"and LocationId: {request.Id}");

                throw new NotFoundCustomException(
                    "Dispatch location not found.",
                    "The dispatch location does not exist or does not belong to the specified supplier.");
            }

            _logger.LogInfo(
                $"Found Dispatch Location for SupplierId: {data.SupplierId} " +
                $"and LocationId: {request.Id}");

            // --------------------------------------------------------
            // Update fields
            // --------------------------------------------------------

            if (data.LocationName != null)
            {
                _logger.LogInfo(
                    $"Updating LocationName for Dispatch Location Id: {request.Id}");

                location.LocationName = data.LocationName;
            }

            if (data.AddressLine1 != null)
            {
                _logger.LogInfo(
                    $"Updating AddressLine1 for Dispatch Location Id: {request.Id}");

                location.AddressLine1 = data.AddressLine1;
            }

            if (data.AddressLine2 != null)
            {
                _logger.LogInfo(
                    $"Updating AddressLine2 for Dispatch Location Id: {request.Id}");

                location.AddressLine2 = data.AddressLine2;
            }

            if (data.City != null)
            {
                _logger.LogInfo(
                    $"Updating City for Dispatch Location Id: {request.Id}");

                location.City = data.City;
            }

            if (data.State != null)
            {
                _logger.LogInfo(
                    $"Updating State for Dispatch Location Id: {request.Id}");

                location.State = data.State;
            }

            if (data.Country != null)
            {
                _logger.LogInfo(
                    $"Updating Country for Dispatch Location Id: {request.Id}");

                location.Country = data.Country;
            }

            if (data.PinCode != null)
            {
                _logger.LogInfo(
                    $"Updating PinCode for Dispatch Location Id: {request.Id}");

                location.PinCode = data.PinCode;
            }

            if (data.ContactPerson != null)
            {
                _logger.LogInfo(
                    $"Updating ContactPerson for Dispatch Location Id: {request.Id}");

                location.ContactPerson = data.ContactPerson;
            }

            if (data.ContactEmail != null)
            {
                _logger.LogInfo(
                    $"Updating ContactEmail for Dispatch Location Id: {request.Id}");

                location.ContactEmail = data.ContactEmail;
            }

            if (data.ContactPhone != null)
            {
                _logger.LogInfo(
                    $"Updating ContactPhone for Dispatch Location Id: {request.Id}");

                location.ContactPhone = data.ContactPhone;
            }

         

            if (data.IsDefault.HasValue)
            {
                _logger.LogInfo(
                    $"Updating IsDefault for Dispatch Location Id: {request.Id}");

                
                if (data.IsDefault.Value)
                {
                    var existingDefaultLocations =
                        _repository.SupplierDispatchLocation
                            .FindByCondition(x =>
                                x.SupplierId == data.SupplierId &&
                                x.Id != request.Id &&
                                x.IsDefault &&
                                x.IsActive)
                            .ToList();

                    foreach (var existingLocation in existingDefaultLocations)
                    {
                        existingLocation.IsDefault = false;

                      

                        _logger.LogInfo(
                            $"Existing Default Dispatch Location Id: " +
                            $"{existingLocation.Id} changed to IsDefault = false.");
                    }
                      _repository.SupplierDispatchLocation.UpdateRange(existingDefaultLocations);
                }

                
                location.IsDefault = data.IsDefault.Value;
            }

          

            _repository.SupplierDispatchLocation.Update(location);

            _logger.LogInfo(
                $"Saving changes for Dispatch Location Id: {request.Id}");

            await _repository.SaveAsync();

            _logger.LogInfo(
                $"Supplier Dispatch Location updated successfully. " +
                $"Id: {location.Id}");

            return location.Id;
        }
    }
}