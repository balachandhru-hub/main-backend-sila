using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.CreateOutlet
{
    public class CreateOutletCommandHandler : IRequestHandler<CreateOutletCommand, Guid>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public CreateOutletCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<Guid> Handle(CreateOutletCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Creating outlet. OrganizationId: {request.OrganizationId}");

            if (string.IsNullOrWhiteSpace(request.Request.OutletName))
            {
                _logger.LogError("Outlet name is missing.");
                throw new BadRequestCustomException("Outlet name is required.", "Enter an outlet name.");
            }

            BuyerBusinessProfile? buyer = _repository.BuyerBusinessProfile.FindFirstByCondition(
                x => x.OrganizationId == request.OrganizationId && x.IsActive);
            if (buyer == null)
            {
                _logger.LogError($"Buyer not found. OrganizationId: {request.OrganizationId}");
                throw new NotFoundCustomException("Buyer not found.", "The signed-in organization does not have a buyer profile.");
            }

            await ValidatePropertyAsync(_repository, _logger, request.Request.PropertyId, buyer.Id, cancellationToken);

            BuyerOutlet outlet = new BuyerOutlet
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                OutletName = request.Request.OutletName.Trim(),
                OutletCode = request.Request.OutletCode,
                Description = request.Request.Description,
                ExternalShipTo = request.Request.ExternalShipTo,
                AddressLine1 = request.Request.AddressLine1,
                City = request.Request.City,
                Country = request.Request.Country,
                PropertyId = request.Request.PropertyId,
                StorageLocation = request.Request.StorageLocation?.Trim()
            };
            _repository.BuyerOutlet.Create(outlet);
            await _repository.SaveAsync();

            _logger.LogInfo($"Outlet created. OutletId: {outlet.Id}, BuyerId: {buyer.Id}");
            return outlet.Id;
        }

        // An outlet's property must be a property of the same buyer.
        internal static async Task ValidatePropertyAsync(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            Guid? propertyId,
            Guid buyerId,
            CancellationToken cancellationToken)
        {
            if (propertyId == null || propertyId == Guid.Empty)
            {
                return;
            }

            BuyerProperty? property = await repository.WeeklyBucket.GetPropertyAsync(propertyId.Value, buyerId, cancellationToken);
            if (property == null)
            {
                logger.LogError($"Property not found. PropertyId: {propertyId}, BuyerId: {buyerId}");
                throw new NotFoundCustomException("Property not found.", "Select a property that belongs to this buyer organization.");
            }
        }
    }
}
