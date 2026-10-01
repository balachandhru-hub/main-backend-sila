using Buyer.Application.Features.Commands.CreateOutlet;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.UpdateOutlet
{
    public class UpdateOutletCommandHandler : IRequestHandler<UpdateOutletCommand, Unit>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public UpdateOutletCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<Unit> Handle(UpdateOutletCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Updating outlet. OutletId: {request.OutletId}, OrganizationId: {request.OrganizationId}");

            if (string.IsNullOrWhiteSpace(request.Request.OutletName))
            {
                throw new BadRequestCustomException("Outlet name is required.", "Enter an outlet name.");
            }

            BuyerBusinessProfile? buyer = _repository.BuyerBusinessProfile.FindFirstByCondition(
                x => x.OrganizationId == request.OrganizationId && x.IsActive);
            if (buyer == null)
            {
                _logger.LogError($"Buyer not found. OrganizationId: {request.OrganizationId}");
                throw new NotFoundCustomException("Buyer not found.", "The signed-in organization does not have a buyer profile.");
            }

            BuyerOutlet? outlet = await _repository.Wishlist.GetOutletAsync(request.OutletId, buyer.Id, cancellationToken);
            if (outlet == null)
            {
                _logger.LogError($"Outlet not found. OutletId: {request.OutletId}, BuyerId: {buyer.Id}");
                throw new NotFoundCustomException("Outlet not found.", "No outlet exists for this buyer organization.");
            }

            await CreateOutletCommandHandler.ValidateApprovalFlowAsync(
                _repository, _logger, request.Request.MasterApprovalFlowId, buyer.Id);

            outlet.OutletName = request.Request.OutletName.Trim();
            outlet.OutletCode = request.Request.OutletCode;
            outlet.Description = request.Request.Description;
            outlet.ExternalShipTo = request.Request.ExternalShipTo;
            outlet.AddressLine1 = request.Request.AddressLine1;
            outlet.City = request.Request.City;
            outlet.Country = request.Request.Country;
            outlet.MasterApprovalFlowId = request.Request.MasterApprovalFlowId;
            _repository.BuyerOutlet.Update(outlet);
            await _repository.SaveAsync();

            _logger.LogInfo($"Outlet updated. OutletId: {outlet.Id}, BuyerId: {buyer.Id}");
            return Unit.Value;
        }
    }
}
