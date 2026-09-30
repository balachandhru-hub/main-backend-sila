using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;
using Supplier.Domain.Entities;
using Supplier.Infrastructure.Contracts.IRepository;

namespace Supplier.Application.Features.Commands.CreateSupplierDispatchLocation
{
    public class CreateSupplierDispatchLocationCommandHandler : IRequestHandler<CreateSupplierDispatchLocationCommand, Guid>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public CreateSupplierDispatchLocationCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<Guid> Handle(CreateSupplierDispatchLocationCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Creating Dispatch Location for SupplierId: {request.SupplierId}");

            if (request.SupplierId == Guid.Empty)
            {
                _logger.LogError("SupplierId is empty. Cannot create dispatch location.");
                throw new PreConditionFailedCustomException("Invalid supplier information.", "SupplierId is required.");
            }

            _logger.LogInfo($"SupplierId validated. Proceeding to create dispatch location: {request.Data.LocationName}");
    

            if (request.Data.IsDefault)
            {
                _logger.LogInfo(
                    $"New delivery location is marked as Default. " +
                    $"Checking existing default location for SupplierId: {request.SupplierId}");

                var existingDefaultLocations = _repository.SupplierDispatchLocation
                    .FindByCondition(x =>
                        x.SupplierId == request.SupplierId &&
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
                  _repository.SupplierDispatchLocation.UpdateRange(existingDefaultLocations);
            }

            var location = new SupplierDispatchLocation
            {
                Id = Guid.NewGuid(),
                SupplierId = request.SupplierId,
                LocationName = request.Data.LocationName,
                AddressLine1 = request.Data.AddressLine1,
                AddressLine2 = request.Data.AddressLine2,
                City = request.Data.City,
                State = request.Data.State,
                Country = request.Data.Country,
                PinCode = request.Data.PinCode,
                ContactPerson = request.Data.ContactPerson,
                ContactEmail = request.Data.ContactEmail,
                ContactPhone = request.Data.ContactPhone,
                IsDefault = request.Data.IsDefault,
                IsActive = true
            };

            _repository.SupplierDispatchLocation.Create(location);
            _logger.LogInfo($"Saving dispatch location to database for SupplierId: {request.SupplierId}");
            await _repository.SaveAsync();

            _logger.LogInfo($"Dispatch Location created successfully. Id: {location.Id}");
            return location.Id;
        }
    }
}
